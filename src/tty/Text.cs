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
        Columns = NormalizedText.Length;  // fixme for wide chars/emojis
        this.fgColor = fgColor;
        this.bgColor = bgColor;

        if (maxColumns != null && Columns > maxColumns.Value) {
            if (autoFit) {
                // Show "..<endofstring>"
                NormalizedText = ".." + NormalizedText[^(maxColumns.Value-2)..];
                Columns = NormalizedText.Length;  // fixme for wide chars/emojis
            } else {
                // Show "<frontofstring>"
                NormalizedText = NormalizedText.Substring(0, maxColumns.Value);
                Columns = NormalizedText.Length;  // fixme for wide chars/emojis
            }
        }
    }

    public readonly string NormalizedText;
    public readonly int Columns;
    public readonly ColorRGB? fgColor;
    public readonly ColorRGB? bgColor;
}
