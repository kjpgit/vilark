using System.Text;

namespace vilark;

//
// The Windows console (conhost.exe) doesn't support combining
// codes. You'll have to first normalize to an equivalent string
// that uses precomposed characters.
//
// Also, support double width and zero width characters.
//

readonly struct DisplayText
{
    public DisplayText(string someArbitraryUnicodeJunk, int? maxColumns = null, bool autoFit=false,
            ColorRGB? fgColor = null, ColorRGB? bgColor = null) {
        NormalizedText = someArbitraryUnicodeJunk.Normalize(NormalizationForm.FormC);

        // This works for emojis, because they use two UTF-16 chars, and are also two columns wide.
        // I'm sure there are some codepoints it doesn't work for.
        Columns = NormalizedText.Length;
        this.fgColor = fgColor;
        this.bgColor = bgColor;

        // Fixme: this is slicing characters, which could break emojis/surrogate pairs
        // Fixme: add some unit tests
        if (maxColumns != null && Columns > maxColumns.Value) {
            if (autoFit) {
                // Show "..<endofstring>"
                NormalizedText = ".." + NormalizedText[^(maxColumns.Value-2)..];
                Columns = NormalizedText.Length;
            } else {
                // Show "<frontofstring>"
                NormalizedText = NormalizedText.Substring(0, maxColumns.Value);
                Columns = NormalizedText.Length;
            }
        }
    }

    public readonly string NormalizedText;
    public readonly int Columns;
    public readonly ColorRGB? fgColor;
    public readonly ColorRGB? bgColor;
}
