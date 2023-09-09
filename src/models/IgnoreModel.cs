using Ignore;

namespace vilark;

class IgnoreModel
{
    private static readonly IReadOnlyList<string> defaultConfigRules = new string[] {
        "# This file uses .gitignore format",
        ".git",
        "*.swp",
        "*.pyc",
        "Library/",
        "node_modules",
        "obj",
        "bin",
    };

    // Create some gitignore rules using
    // 1) Any ignores set in VILARK_IGNORE_FILE (which defaults to $HOME/.config/vilark/ignore_rules)
    // 2) All .gitignore files we find when scanning directories
    public IgnoreModel(string dirPath) {
        m_rules = new();

        var stdIgnoreFile = GetStandardIgnoreFile();
        if (stdIgnoreFile != null && File.Exists(stdIgnoreFile)) {
            AddIgnoresFromFile(stdIgnoreFile);
        }

        // Rules need to be checked in order of root -> level1 -> level2 ...
        var gitIgnoreFiles = WalkGitIgnoreFiles(dirPath);
        gitIgnoreFiles.Reverse();
        foreach (var f in gitIgnoreFiles) {
            AddIgnoresFromFile(f);
        }
    }

    // Called during directory exploration
    // Create a new state, which is all parent rules + new gitIgnoreFile rules
    public IgnoreModel(string gitIgnoreFile, IgnoreModel parent) {
        m_rules = new(parent.m_rules);
        AddIgnoresFromFile(gitIgnoreFile);
    }

    // Also called when we are scanning all directories
    private void AddIgnoresFromFile(string path) {
        Log.Info($"Using ignore rules from {path}");
        var lines = File.ReadAllLines(path);
        foreach (var line in lines) {
            Log.Info($"rule is {line}");
            m_rules.Add(new Ignore.IgnoreRule(line));
        }
    }

    private static List<string> WalkGitIgnoreFiles(string dirPath) {
        List<string> ret = new();
        string? absPath = Path.GetFullPath(dirPath);
        while (absPath != null) {
            Log.Info($"Checking for .gitignore in {absPath}");
            string gitIgnorePath = absPath + "/.gitignore";
            if (File.Exists(gitIgnorePath)) {
                ret.Add(gitIgnorePath);
            }
            // Go up a level
            absPath = Path.GetDirectoryName(absPath);
        }
        return ret;
    }

    private static string? GetStandardIgnoreFile() {
        var f = Environment.GetEnvironmentVariable("VILARK_IGNORE_FILE");
        if (f != null) {
            return f;
        }
        var home = Environment.GetEnvironmentVariable("HOME");
        if (home != null) {
            string stdPath = home + "/.config/vilark/ignore_rules.txt";
            if (!File.Exists(stdPath)) {
                CreateDefaultIgnoreConfig(stdPath);
            }
            return stdPath;
        }
        return null;
    }

    private static void CreateDefaultIgnoreConfig(string path) {
        string dirName = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dirName);

        using (var fs = new FileStream(path, FileMode.Create)) {
            foreach (var rule in defaultConfigRules) {
                fs.Write(System.Text.Encoding.UTF8.GetBytes(rule + "\n"));
            }
        }
    }

    // Check all rules, starting at global config, then root of tree and working down.
    public bool IsIgnored(string path) {
        var ignore = false;
        foreach (var rule in m_rules) {
            if (rule.Negate) {
                if (ignore && rule.IsMatch(path)) {
                    ignore = false;
                }
            } else if (!ignore && rule.IsMatch(path)) {
                ignore = true;
            }
        }
        return ignore;
    }

    private List<Ignore.IgnoreRule> m_rules;
}
