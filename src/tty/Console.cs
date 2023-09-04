//
// Console IO abstraction
//
using Linux = Tmds.Linux;
using Libc = Tmds.Linux.LibC;
using System.Buffers.Text; // For Utf8Formatter

namespace vilark;

/*
 * See https://chromium.googlesource.com/apps/libapps/+/a5fb83c190aa9d74f4a9bca233dac6be2664e9e9/hterm/doc/ControlSequences.md
 * See https://viewsourcecode.org/snaptoken/kilo/02.enteringRawMode.html
 * See https://man7.org/linux/man-pages/man3/termios.3.html
 */

readonly record struct ConsoleDimensions(int rows, int cols);


class Console
{
    private Linux.termios original_termios;
    private ConsoleDimensions current_dimensions;

    // ANSI Erase in Display
    public void ClearScreen() {
        Write("\x1b[2J"u8);
    }

    /*
    // ANSI Erase In Line
    // Don't use, it can flicker
    public void ClearAfterCursor() {
        Write("\x1b[K"u8);
    }
    */

    public void SetUnderline(bool enabled) {
        if (enabled) {
            Write("\x1b[4m"u8);
        } else {
            Write("\x1b[24m"u8);
        }
    }

    public void SetBackgroundColor(ColorRGB? color) {
        if (color != null) {
            Write("\x1b[48;2;"u8);
            WriteIntAsText(color.r);
            Write(";"u8);
            WriteIntAsText(color.g);
            Write(";"u8);
            WriteIntAsText(color.b);
            Write("m"u8);
        } else {
            Write("\x1b[49m"u8);
        }
    }

    public void SetForegroundColor(ColorRGB? color) {
        if (color != null) {
            Write("\x1b[38;2;"u8);
            WriteIntAsText(color.r);
            Write(";"u8);
            WriteIntAsText(color.g);
            Write(";"u8);
            WriteIntAsText(color.b);
            Write("m"u8);
        } else {
            Write("\x1b[39m"u8);
        }
    }

    public void SetCursorVisible(bool enabled) {
        Write(enabled ? "\x1b[?25h"u8 : "\x1b[?25l"u8);
    }

    public void SetAlternateScreen(bool enabled) {
        Write(enabled ? "\x1b[?1049h"u8 : "\x1b[?1049l"u8);
    }

    // Same as what `tput cols`, `tput lines` returns
    public ConsoleDimensions GetDimensions() {
        return current_dimensions;
    }

    unsafe public void UpdateDimensions() {
        Linux.winsize argWinsize;
        Libc.ioctl(2, Libc.TIOCGWINSZ, &argWinsize);
        current_dimensions = new ConsoleDimensions { rows=argWinsize.ws_row, cols=argWinsize.ws_col };
    }

    unsafe public void SetRawMode(bool enabled) {
        if (enabled) {
            Linux.termios t;
            Libc.tcgetattr(0, &t);
            original_termios = t;

            t.c_oflag &= ~(Libc.OPOST); // Don't add cr on output
            t.c_iflag &= ~(Libc.IXON);  // Disable Ctrl-S & Ctrl-Q
            t.c_iflag &= ~(Libc.IEXTEN);  // Disable Ctrl-V
            t.c_iflag &= ~(Libc.ICRNL);  // Fix Ctrl-M (Don't convert cr to nl on input)
            t.c_lflag &= ~(Libc.ECHO | Libc.ICANON); // No echo, No line mode
            t.c_cc[Libc.VINTR] = 0;  // Don't generate SIGINT, read() it as ctrl-c instead
            t.c_cc[Libc.VQUIT] = 0;  // Don't generate SIGQUIT, read() it as ctrl-\ instead
            t.c_cc[Libc.VSUSP] = 0;  // Don't generate SIGQUIT, read() it as ctrl-\ instead
            t.c_cc[Libc.VMIN] = 0;
            t.c_cc[Libc.VTIME] = 1; // Wait 1/10 of a second before returning empty read()
            Libc.tcsetattr(0, (int)Libc.TCSAFLUSH, &t);
        } else {
            Linux.termios t = original_termios;
            Libc.tcsetattr(0, (int)Libc.TCSAFLUSH, &t);
        }
    }

    public void SetCursorXY(int x, int y, DrawRect rect) {
        SetCursorRawXY(x + rect.x, y + rect.y);
    }

    // ANSI Move cursor absolute
    // Note: x and y are 0-based
    // This function converts them to 1-based for console
    // If the cursor is out of bounds, the terminal will silently clip it.
    private void SetCursorRawXY(int x, int y) {
        int console_x = x + 1;
        int console_y = y + 1;
        Write("\x1b["u8);
        WriteIntAsText(console_y);
        Write(";"u8);
        WriteIntAsText(console_x);
        Write("H"u8);
    }

    public void Flush() {
        _wb.Flush();
    }

    public void WriteIntAsText(int n) {
        Span<byte> n_bytes = stackalloc byte[20];
        int len;
        Utf8Formatter.TryFormat(n, n_bytes, out len);
        Write(n_bytes.Slice(0, len));
    }

    public void Write(DisplayText text) {
        if (text.bgColor != null) { SetBackgroundColor(text.bgColor); }
        if (text.fgColor != null) { SetForegroundColor(text.fgColor); }
        Write(System.Text.Encoding.UTF8.GetBytes(text.NormalizedText));
        if (text.bgColor != null) { SetBackgroundColor(null); }
        if (text.fgColor != null) { SetForegroundColor(null); }
    }

    public void Write(ReadOnlySpan<byte> bytes) {
        _wb.Write(bytes);
    }

    public void WriteRepeated(ReadOnlySpan<byte> bytes, int count) {
        for (int i = 0; i < count; i++) {
            _wb.Write(bytes);
        }
    }

    private ConsoleBufferedWriter _wb = new ConsoleBufferedWriter();
}


class ConsoleBufferedWriter
{
    public void Write(ReadOnlySpan<byte> bytes) {
        if (GetCurrentCapacity() >= bytes.Length) {
            // Fast path, append to current buffer
            _append(bytes);
        } else {
            // Need to flush
            Flush();
            if (GetCurrentCapacity() >= bytes.Length) {
                _append(bytes);
            } else {
                // Too large to buffer, don't bother
                _write(bytes);

            }
        }
    }

    public void Flush() {
        var src = new ReadOnlySpan<byte>(_buffer).Slice(0, _length);
        _write(src);
        _length = 0;
    }

    private void _append(ReadOnlySpan<byte> bytes) {
        var target = new Span<byte>(_buffer, _length, bytes.Length);
        bytes.CopyTo(target);
        _length += bytes.Length;
    }

    unsafe private void _write(ReadOnlySpan<byte> bytes) {
        fixed (byte* bp = bytes) {
            Libc.write(2, bp, bytes.Length);
        }
    }

    public int GetLength() { return _length; }
    public int GetMaxCapacity() { return _buffer.Length; }
    public int GetCurrentCapacity() { return GetMaxCapacity() - GetLength(); }

    private byte[] _buffer = new byte[4000];
    private int _length = 0;
}
