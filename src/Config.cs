using System.Text.Json;
using System.Text.Json.Serialization;
namespace vilark;

record ConfigJson {
    public string? FuzzySearchMode;
    public string? SelectionFGColor;
    public string? SelectionBGColor;
}

// For AOT support
[JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = true)]
[JsonSerializable(typeof(ConfigJson))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}

class Config
{
    // Defaults, override in config
    public FuzzySearchMode FuzzySearchMode = FuzzySearchMode.FUZZY_WORD_ORDERED;
    public ColorRGB SelectionFGColor = ColorRGB.FromString("rgb(10,30,50)");
    public ColorRGB SelectionBGColor = ColorRGB.FromString("rgb(222,236,249)");

    public Config() {
        LoadSettings();
    }

    public static string? GetSettingsFile() {
        var f = Environment.GetEnvironmentVariable("VILARK_SETTINGS_FILE");
        if (f != null) {
            return f;
        }
        var home = Environment.GetEnvironmentVariable("HOME");
        if (home != null) {
            string stdPath = home + "/.config/vilark/settings.json";
            return stdPath;
        }
        return null;
    }

    public void LoadSettings() {
        string? fileName = GetSettingsFile();
        if (fileName == null || !File.Exists(fileName)) {
            return;
        }

        var jsonString = File.ReadAllText(fileName);
        ConfigJson? config = JsonSerializer.Deserialize<ConfigJson>(jsonString,
                SourceGenerationContext.Default.ConfigJson);
        if (config == null) {
            return;
        }

        if (config.FuzzySearchMode != null) {
            FuzzySearchMode = (FuzzySearchMode)Enum.Parse(FuzzySearchMode.GetType(), config.FuzzySearchMode);
        }
        if (config.SelectionFGColor != null) {
            SelectionFGColor = ColorRGB.FromString(config.SelectionFGColor);
        }
        if (config.SelectionBGColor != null) {
            SelectionBGColor = ColorRGB.FromString(config.SelectionBGColor);
        }
    }

    public void SaveSettings() {
        string? fileName = GetSettingsFile();
        if (fileName == null) {
            return;
        }

        var settings = new ConfigJson() {
            FuzzySearchMode = this.FuzzySearchMode.ToString(),
            SelectionFGColor = this.SelectionFGColor.ToString(),
            SelectionBGColor = this.SelectionBGColor.ToString()
        };

        string jsonString = JsonSerializer.Serialize(settings, SourceGenerationContext.Default.ConfigJson);

        // Ensure all directories are created
        string dirName = Path.GetDirectoryName(fileName)!;
        Directory.CreateDirectory(dirName);

        string tmpName = fileName + ".tmp";
        File.WriteAllText(tmpName, jsonString);
        File.Move(tmpName, fileName, overwrite:true);
    }
}
