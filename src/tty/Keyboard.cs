using System.Buffers;
using System.Text;
using System.Diagnostics;
using Linux = Tmds.Linux;
using Libc = Tmds.Linux.LibC;
using System.Buffers.Text;
using static vilark.KeyCode;

// Use `showkey -a` to test
// ANSI: https://gist.github.com/fnky/458719343aabd01cfb17a3a4f7296797
// VT100: https://vt100.net/docs/vt100-ug/chapter3.html
// XTERM: https://www.xfree86.org/current/ctlseqs.html
// http://www.braun-home.net/michael/info/misc/VT100_commands.htm
// https://www.cs.colostate.edu/~mcrob/toolbox/unix/keyboard.html
// todo: ESC [ 200 ~, followed by the pasted text, followed by ESC [ 201 ~.
//
namespace vilark;

enum KeyCode {
    PAGE_UP = 1000,
    PAGE_DOWN,
    HOME,
    END,

    UP_ARROW,
    DOWN_ARROW,
    LEFT_ARROW,
    RIGHT_ARROW,

    BACKTAB,
    BACKSPACE,
    INSERT,
    DELETE,

    ESCAPE,
    UNKNOWN,
}

/*
[Flags]
enum KeyboardModifier {
    CTRL = 1,
    ALT = 2,
    META = 4,
}
*/

struct KeyPress
{
    // Init-only properties (can't be changed)
    public KeyCode? keyCode     {get; init; }
    public Rune? rune           {get; init; }
    public int modifiers        {get; init; }

    public const char ASCII_CTRL_W = (char)23;

    // Guess how much debugging time it took to realize I needed Value.Value here
    public bool IsRune(char c) => (rune != null && rune.Value.Value.Equals(c));
    public bool IsAsciiControlCode() => (rune != null && rune.Value.Value < 32);

    // Call for all ASCII 7-bit (including 0-32 control codes), or a utf-8 decoded rune
    public static KeyPress FromUnicodeScalar(int value) {
        return new KeyPress { rune = new Rune(value) };
    }

    public static KeyPress FromKeyCode(KeyCode code, int modifiers=0) {
        return new KeyPress { keyCode = code, modifiers = modifiers };
    }

    public override string ToString() {
        if (keyCode != null) {
            return $"{keyCode} mod={modifiers}";
        } else if (rune != null) {
            return $"{TextHelper.HumanEscapeRune(rune.Value)} mod={modifiers}";
        } else {
            throw new Exception("invalid keypress");
        }
    }
}


class Keyboard
{
    private const byte ESC = 0x1b;
    private KeySequenceReader reader = new();
    private byte[] m_utf8_bytes = new byte[4];

    private readonly record struct KeyByte(KeyCode? keyCode, byte? rawByte = null, int modifiers = 0);

    public IEnumerable<KeyPress> GetKeyPress() {
        while (true) {
            // Get first byte.  99% of the time, it won't need additional bytes/decoding.
            var kb = _GetKeyPressByteAndTrace();
            if (kb.keyCode != null) {
                yield return KeyPress.FromKeyCode(kb.keyCode.Value, kb.modifiers);
                continue;
            }
            if (kb.rawByte == null) {
                Trace.Assert(false);
                continue;
            }
            if (kb.rawByte.Value < 128) {
                // 7bit/ascii
                yield return KeyPress.FromUnicodeScalar(kb.rawByte.Value);
                continue;
            }

            /* Read UTF8 continuation bytes */
            /* If you send garbage UTF8 (partial sequence), too bad, a few bytes will be eaten... */
            int total_bytes = GetUTFTotalByteCount(kb.rawByte.Value);
            Trace.Assert(total_bytes > 0);

            int nr_bytes = 0;
            m_utf8_bytes[nr_bytes++] = kb.rawByte.Value;
            while (nr_bytes < total_bytes) {
                reader.ResetTracing();
                byte b = reader.GetNext();
                m_utf8_bytes[nr_bytes++] = b;
                Log.Info($"UTF8 continuation byte = {reader.ToString()}");
            }

            Log.Info($"UTF8 sequence received {nr_bytes} = {m_utf8_bytes}");
            ReadOnlySpan<byte> utf8_span = m_utf8_bytes.AsSpan().Slice(0, nr_bytes);
            Rune parsedRune;
            int bytesConsumed;
            OperationStatus decode_status = Rune.DecodeFromUtf8(utf8_span, out parsedRune, out bytesConsumed);
            if (decode_status != OperationStatus.Done) {
                Log.Info($"DecodeFromUtf8 did not return success: {decode_status}");
                Trace.Assert(parsedRune == Rune.ReplacementChar);
            }
            yield return KeyPress.FromUnicodeScalar(parsedRune.Value);
        }
    }

    private int GetUTFTotalByteCount(byte b) {
        return b switch {
            _ when ((b & 0xf0) == 0xf0) => 4,  // 11110000
            _ when ((b & 0xe0) == 0xe0) => 3,  // 11100000
            _ when ((b & 0xc0) == 0xc0) => 2,  // 11000000
            _ => -1   // Invalid UTF8, it is ASCII (<128)
        };
    }

    // Trace a single utf8 byte, or a sequence of 7-bit escapes
    private KeyByte _GetKeyPressByteAndTrace() {
        reader.ResetTracing();
        var kb = _GetKeyPressByte();
        Log.Info($"GetKeyPressByte() = {reader.ToString()}");
        return kb;
    }

    private KeyByte _GetKeyPressByte() {
        byte b = reader.GetNext();
        return b switch {
              0 => kb(UNKNOWN),  // I feel safer by not returning this as char data.
              ESC => _ParseEscapeSequence(),
              127 => kb(BACKSPACE),
              _ => kb(rawByte: (byte)b, modifiers: 0)
        };
    }

    private KeyByte _ParseEscapeSequence() {
        byte? b = reader.TryNext();
        // <esc> <nochar/timeout>          -> esc
        // <esc> <esc>                     -> esc
        return b switch {
            null => kb(ESCAPE),
            ESC => kb(ESCAPE),
            (byte)'[' => _ParseEscapeSequenceCSI(),
            (byte)'O' => _ParseEscapeSequenceSS3(),
            _ => kb(UNKNOWN),
        };
    }

    private KeyByte _ParseEscapeSequenceCSI() {
        byte? d1 = reader.TryNext();
        return d1 switch {
            null => kb(UNKNOWN),
            (byte)'A' => kb(UP_ARROW),
            (byte)'B' => kb(DOWN_ARROW),
            (byte)'C' => kb(RIGHT_ARROW),
            (byte)'D' => kb(LEFT_ARROW),
            (byte)'H' => kb(HOME),
            (byte)'F' => kb(END),
            (byte)'Z' => kb(BACKTAB),
             _ => _ParseEscapeSequenceCSIExtended(d1.Value),
        };
    }

    private KeyByte _ParseEscapeSequenceCSIExtended(byte d1) {
        // This is a hack... but it works with the keys we need, which isn't that many
        if (d1 >= '0' && d1 <= '9') {
            byte? d2 = reader.TryNext();
            if (d2 == '~') {
                return d1 switch {
                    (byte)'1' => kb(HOME),
                    (byte)'2' => kb(INSERT),
                    (byte)'3' => kb(DELETE),
                    (byte)'4' => kb(END),
                    (byte)'5' => kb(PAGE_UP),
                    (byte)'6' => kb(PAGE_DOWN),
                    _ => kb(UNKNOWN),
                };
            }
        }
        return kb(UNKNOWN);
    }

    private KeyByte _ParseEscapeSequenceSS3() {
        // https://vi.stackexchange.com/questions/15324/up-arrow-key-code-why-a-becomes-oa
        // Sent if "Cursor Key Mode" / application modei is enabled.
        // (Typically for full screen program)
        // Application mode = smkx , Normal = rmkx
        byte? d1 = reader.TryNext();
        return d1 switch {
            (byte)'A' => kb(UP_ARROW),
            (byte)'B' => kb(DOWN_ARROW),
            (byte)'C' => kb(RIGHT_ARROW),
            (byte)'D' => kb(LEFT_ARROW),
            (byte)'H' => kb(HOME),
            (byte)'F' => kb(END),
             _ => kb(UNKNOWN),
        };
    }

    private KeyByte kb(KeyCode? keyCode = null, byte? rawByte = null, int modifiers = 0) {
        return new KeyByte(keyCode, rawByte, modifiers);
    }

}

class KeySequenceReader
{
    // To reduce syscalls
    private ConsoleBufferedReader _reader = new();

    // For tracing
    private byte[] _buffer = new byte[10];
    private int _length = 0;

    public void ResetTracing() {
        _length = 0;
    }

    public byte GetNext() {
        while (true) {
            byte? b = TryNext();
            if (b != null) {
                return b.Value;
            }
        }
    }

    public byte? TryNext() {
        byte? nextByte = _reader.TryReadByte();
        if (nextByte != null) {
            // Save to trace stack
            _buffer[_length] = nextByte.Value;
            _length++;
        }
        return nextByte;
    }

    override public string ToString() {
        return TextHelper.HumanEscapeBytes(new Span<byte>(_buffer, 0, _length));
    }
}


class ConsoleBufferedReader
{
    public byte? TryReadByte() {
        if (GetSize() == 0) {
            _readChunk();
        }
        if (GetSize() == 0) {
            return null;
        } else {
            return _buffer[_offset++];
        }
    }

    private void _readChunk() {
        _length = 0;
        _offset = 0;
        var target = new Span<byte>(_buffer, 0, _buffer.Length);
        int ret;
        unsafe {
            fixed (byte* bp = target) {
                ret = (int)Libc.read(1, bp, _buffer.Length);
            }
        }
        if (ret > 0) {
            _length = ret;
        }
    }

    private int GetSize() {
        return _length - _offset;
    }

    private byte[] _buffer = new byte[4000];
    private int _length = 0;
    private int _offset = 0;
}
