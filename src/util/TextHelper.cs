using System.Text;

namespace vilark;

class TextHelper
{
    public static string HumanEscapeBytes(ReadOnlySpan<byte> bytes) {
        var sb = new StringBuilder();
        for (var i = 0; i < bytes.Length; i++) {
            sb.Append(HumanEscapeByte(bytes[i]));
        }
        return sb.ToString();
    }

    public static string HumanEscapeByte(byte b) {
        int val = (int)b;
        if (val == '\\') {
            return "\\\\";
        } else if (val >= 33 && val <= 126) {
            return ((char)val).ToString();
        } else {
            return String.Format("\\x{0:X2}", val);
        }
    }

    public static string HumanEscapeRune(Rune r) {
        int val = r.Value;
        if (val == '\\') {
            return "\\\\";
        } else if (val >= 33 && val <= 126) {
            return ((char)val).ToString();
        } else {
            return String.Format("U+{0:X}", val);
        }
    }

    // For safe backspace, that works with emoji and other non-BMP codepoints
    public static string RemoveLastRune(string s) {
        var iterator = s.EnumerateRunes();
        int count = iterator.Count();
        var sb = new StringBuilder();
        foreach (Rune rune in iterator.Take(count - 1)) {
            sb.Append(rune);
        }
        return sb.ToString();
    }

    // Runes work with combining characters (e.g. surrogate pair)
    public static bool HasUppercaseChar(string s) {
        foreach (Rune rune in s.EnumerateRunes()) {
            if (Rune.IsUpper(rune)) {
                return true;
            }
        }
        return false;
    }

}


