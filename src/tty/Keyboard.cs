using System.Globalization;
using System.Text;
using System.Diagnostics;
using static vilark.KeyCode;

namespace vilark;

/* Simplified key codes used by vilark */
enum KeyCode {
    PAGE_UP = 1000,
    PAGE_DOWN,
    HOME,
    END,

    UP_ARROW,
    DOWN_ARROW,
    LEFT_ARROW,
    RIGHT_ARROW,

    TAB,
    BACKTAB,
    BACKSPACE,

    ESCAPE,
    UNKNOWN,
}

struct KeyPress
{
    // Init-only properties (can't be changed)
    public KeyCode? keyCode     {get; init; }
    public Rune? rune           {get; init; }

    public const char ASCII_CTRL_W = (char)23;

    // Guess how much debugging time it took to realize I needed Value.Value here
    public bool IsRune(char c) => (rune != null && rune.Value.Value.Equals(c));
    public bool IsAsciiControlCode() => (rune != null && rune.Value.Value < 32);

    // Call for all ASCII 7-bit (including 0-32 control codes), or a utf-8 decoded rune
    public static KeyPress FromUnicodeScalar(int value) {
        return new KeyPress { rune = new Rune(value) };
    }

    public static KeyPress FromKeyCode(KeyCode code) {
        return new KeyPress { keyCode = code};
    }

    public override string ToString() {
        if (keyCode != null) {
            return $"{keyCode}";
        } else if (rune != null) {
            return $"{TextHelper.HumanEscapeRune(rune.Value)}";
        } else {
            throw new Exception("invalid keypress");
        }
    }
}


class Keyboard
{
    public IEnumerable<KeyPress> GetKeyPress() {
        while (true) {
            var cki = System.Console.ReadKey(true);
            Log.Info($"readkey1 = {TextHelper.HumanEscapeChar(cki.KeyChar)}");
            if (IsSurrogate(cki.KeyChar)) {
                var cki2 = System.Console.ReadKey(true);
                Log.Info($"readkey2 = {TextHelper.HumanEscapeChar(cki2.KeyChar)}");
                // Probably an emoji.  Return it as a rune.
                var rune = new Rune(cki.KeyChar, cki2.KeyChar);
                yield return KeyPress.FromUnicodeScalar(rune.Value);
                continue;
            }

            var s = "";
            if((cki.Modifiers & ConsoleModifiers.Alt) != 0) s +=("ALT+");
            if((cki.Modifiers & ConsoleModifiers.Shift) != 0) s +=("SHIFT+");
            if((cki.Modifiers & ConsoleModifiers.Control) != 0) s +=("CTRL+");
            Log.Info(s + " " + cki.Key.ToString());
            if (cki.KeyChar != (char)0) {
                yield return cki.KeyChar switch {
                    (char)0x1b => KeyPress.FromKeyCode(ESCAPE),
                    (char)0x7f => KeyPress.FromKeyCode(BACKSPACE),
                    _ => KeyPress.FromUnicodeScalar(cki.KeyChar),
                };
            } else {
                KeyCode kc = cki.Key switch {
                    ConsoleKey.Home => HOME,
                    ConsoleKey.End => END,
                    ConsoleKey.PageUp => PAGE_UP,
                    ConsoleKey.PageDown => PAGE_DOWN,

                    ConsoleKey.RightArrow => RIGHT_ARROW,
                    ConsoleKey.LeftArrow => LEFT_ARROW,
                    ConsoleKey.UpArrow => UP_ARROW,
                    ConsoleKey.DownArrow => DOWN_ARROW,

                    ConsoleKey.Escape => ESCAPE,
                    ConsoleKey.Backspace => BACKSPACE,
                    ConsoleKey.Tab when (cki.Modifiers == 0) => TAB,
                    ConsoleKey.Tab when ((cki.Modifiers & ConsoleModifiers.Shift) != 0) => BACKTAB,
                    _ => UNKNOWN,
                };
                yield return KeyPress.FromKeyCode(kc);
            }
        }
    }

    private bool IsSurrogate(char c) {
        return CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.Surrogate;
    }

}

