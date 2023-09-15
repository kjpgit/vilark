// Copyright (C) 2023 Karl Pickett / Vilark Project
using System.Diagnostics;
//using Ignore;

namespace vilark;

class IgnoreModel
{
    private static readonly string defaultGlobalIgnore = @"
# Vilark global ignores (they apply to files in any directory)
# This file uses .gitignore format
#
# Note: These all convert to regexes, which can be slow.
# It is better to use individual .gitignore files in the directories which need it.
# Star patterns (e.g. *.pyc) in particular seem unoptimized right now.

# This doesn't cause a noticable slowdown, and you probably always want this ignored
.git

# If you are on Mac, you probably want this
Library/

# These wildcard patterns cause a slowdown, so they're disabled by default
#*.swp
#*.pyc
";

    // Create some gitignore rules using
    // 1) Any ignores set in VILARK_IGNORE_FILE (which defaults to $HOME/.config/vilark/ignore_rules)
    // 2) All .gitignore files in dirPath and parent dirs
    public IgnoreModel(string absDirPath, bool gitIgnoresEnabled) {
        Trace.Assert(absDirPath.StartsWith("/"));
        Trace.Assert(absDirPath.EndsWith("/"));

        m_rules = new();

        var stdIgnoreFile = GetStandardIgnoreFile();
        if (stdIgnoreFile != null && File.Exists(stdIgnoreFile)) {
            AddIgnoresFromFile(stdIgnoreFile, absDirPath.Length);
        }

        if (gitIgnoresEnabled) {
            // Rules need to be checked in order of root -> level1 -> level2 ...
            var gitIgnoreFiles = WalkGitIgnoreFiles(absDirPath);
            gitIgnoreFiles.Reverse();
            foreach (var f in gitIgnoreFiles) {
                AddIgnoresFromFile(f);
            }
        }
    }

    // Called during directory exploration
    // Create a new state, which is all parent rules + new gitIgnoreFile rules
    public IgnoreModel(string gitIgnoreFile, IgnoreModel parent) {
        m_rules = new(parent.m_rules);
        AddIgnoresFromFile(gitIgnoreFile);
    }

    // Also called when we are scanning all directories
    private void AddIgnoresFromFile(string ignoreFilePath, int? pathStartIndex = null) {
        if (!ignoreFilePath.StartsWith("/")) { throw new Exception("must use absolute path"); }

        if (pathStartIndex == null) {
            var dirName = Path.GetDirectoryName(ignoreFilePath);
            if (dirName == null) {
                throw new Exception("no dirName");
            }
            pathStartIndex = dirName.Length;
        }

        Log.Info($"Using ignore rules from {ignoreFilePath}, pathStartIndex={pathStartIndex}");
        var lines = File.ReadAllLines(ignoreFilePath);
        foreach (var line in lines) {
            Log.Info($"rule is {line}");
            m_rules.Add(new IgnoreContext(new Ignore.IgnoreRule(line), pathStartIndex.Value));
        }
    }

    private static List<string> WalkGitIgnoreFiles(string absDirPath) {
        Trace.Assert(absDirPath.StartsWith("/"));
        Trace.Assert(absDirPath.EndsWith("/"));

        // The loop works better when the dirs dont end with /, except for the root
        if (absDirPath != "/") {
            absDirPath = absDirPath.Substring(0, absDirPath.Length-1);
        }

        List<string> ret = new();
        while (true) {
            Log.Info($"Checking for .gitignore in {absDirPath}");
            string gitIgnorePath = absDirPath + "/.gitignore";
            if (File.Exists(gitIgnorePath)) {
                ret.Add(gitIgnorePath);
            }
            // Go up a level
            var parentPath = Path.GetDirectoryName(absDirPath);
            if (parentPath == null) {
                break;
            }
            absDirPath = parentPath;
        }
        return ret;
    }

    private static string? GetStandardIgnoreFile() {
        var f = Environment.GetEnvironmentVariable("VILARK_IGNORE_FILE");
        if (f != null) {
            if (f == "") {
                Log.Info("Global ignores disabled");
                return null;  // User doesn't want to use any standard ignores
            }
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
            fs.Write(System.Text.Encoding.UTF8.GetBytes(defaultGlobalIgnore));
        }
    }

    // Check all rules, starting at global config, then root of tree and working down.
    public bool IsIgnored(string path) {
        var ignore = false;
        foreach (var ctx in m_rules) {
            var rule = ctx.rule;
            ReadOnlySpan<char> path_span = path;
            path_span = path_span.Slice(ctx.pathStartIndex);
            if (rule.Negate) {
                if (ignore && rule.IsMatch(path_span)) {
                    ignore = false;
                }
            } else if (!ignore && rule.IsMatch(path_span)) {
                ignore = true;
            }
        }
        //Log.Info($"ignore: {path} ({ignore})");
        return ignore;
    }

    private record struct IgnoreContext(Ignore.IgnoreRule rule, int pathStartIndex);

    private List<IgnoreContext> m_rules;
}
