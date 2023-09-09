//
// Console IO abstraction
//
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
    // ANSI Erase in Display
    public void ClearScreen() {
        Write("\x1b[2J"u8);
    }

    public void ResetTextAttrs() {
        Write("\x1b[0m"u8);
    }

    public void SetUnderline(bool enabled) {
        if (enabled) {
            Write("\x1b[4m"u8);
        } else {
            Write("\x1b[24m"u8);
        }
    }

    public void SetBackgroundColor(ColorRGB color) {
        Write("\x1b[48;2;"u8);
        WriteIntAsText(color.r);
        Write(";"u8);
        WriteIntAsText(color.g);
        Write(";"u8);
        WriteIntAsText(color.b);
        Write("m"u8);
    }

    public void SetForegroundColor(ColorRGB color) {
        Write("\x1b[38;2;"u8);
        WriteIntAsText(color.r);
        Write(";"u8);
        WriteIntAsText(color.g);
        Write(";"u8);
        WriteIntAsText(color.b);
        Write("m"u8);
    }

    public void SetCursorVisible(bool enabled) {
        Write(enabled ? "\x1b[?25h"u8 : "\x1b[?25l"u8);
    }

    public void SetAlternateScreen(bool enabled) {
        Write(enabled ? "\x1b[?1049h"u8 : "\x1b[?1049l"u8);
    }

    // C# appears to call TIOCGWINSZ() automatically, good.
    // https://github.com/dotnet/runtime/blob/main/src/native/libs/System.Native/pal_console.c#L27
    public ConsoleDimensions GetDimensions() {
        return new ConsoleDimensions(System.Console.WindowHeight, System.Console.WindowWidth);
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

    private void WriteIntAsText(int n) {
        Span<byte> n_bytes = stackalloc byte[20];
        int len;
        Utf8Formatter.TryFormat(n, n_bytes, out len);
        Write(n_bytes.Slice(0, len));
    }

    public void Write(DisplayText text) {
        if (text.bgColor != null) { SetBackgroundColor(text.bgColor); }
        if (text.fgColor != null) { SetForegroundColor(text.fgColor); }
        Write(System.Text.Encoding.UTF8.GetBytes(text.NormalizedText));
        if (text.bgColor != null || text.fgColor != null) { ResetTextAttrs(); }
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


/*
 * This class was mainly written to experiment with the
 * low level Span/bytes handling, which avoids memory allocations,
 * and also ensure callers aren't doing any expensive conversions.
 *
 * There's no real reason to use it over a the standard buffered byte stream.
 *
 */
class ConsoleBufferedWriter
{
    private Stream _os = System.Console.OpenStandardOutput();

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

    private void _write(ReadOnlySpan<byte> bytes) {
        _os.Write(bytes);
    }

    public int GetLength() { return _length; }
    public int GetMaxCapacity() { return _buffer.Length; }
    public int GetCurrentCapacity() { return GetMaxCapacity() - GetLength(); }

    private byte[] _buffer = new byte[4000];
    private int _length = 0;
}
