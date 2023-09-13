// Copyright (C) 2023 Karl Pickett / Vilark Project
namespace vilark;

class ColorRGB
{
    private ColorRGB() { }

    public static ColorRGB FromString(string s) {
        if (s.StartsWith("rgb(")) {
            s = s.Substring(4);
        }
        if (s.EndsWith(")")) {
            s = s.Substring(0, s.Length - 1);
        }
        string[] parts = s.Split(",");
        if ((parts.Length) != 3) {
            throw new Exception($"Invalid RGB color: {s}");
        }
        var ret = new ColorRGB();
        ret.r = parseComponent(parts[0].Trim());
        ret.g = parseComponent(parts[1].Trim());
        ret.b = parseComponent(parts[2].Trim());
        return ret;
    }

    override public string ToString() {
        return $"rgb({r},{g},{b})";
    }

    static private int parseComponent(string s) {
        int ret = Convert.ToInt32(s);
        if (ret < 0 || ret > 255) {
            throw new Exception($"Invalid RGB color component: {s}");
        }
        return ret;
    }


    public int r;
    public int g;
    public int b;

    public void AdjustR(int delta) { r = Math.Clamp(r+delta*10, 0, 255); }
    public void AdjustG(int delta) { g = Math.Clamp(g+delta*10, 0, 255); }
    public void AdjustB(int delta) { b = Math.Clamp(b+delta*10, 0, 255); }
}

