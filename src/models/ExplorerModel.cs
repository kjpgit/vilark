// Copyright (C) 2023 Karl Pickett / Vilark Project
using System.Diagnostics;
namespace vilark;


// NB: structs can't be covariant in List<>, so this is a class
class DirectoryEntry : ISelectableItem
{
    // If this is not null, implicitly add it to the directory path in each instance.
    // It is not used for display or searching, only final selection output.
    // It's static because we know we are only viewing a single dir hierarchy at a time.
    private static string _abs_dir_prefix = "";

    // The portion of the path below _abs_dir_prefix.
    // We combine the dir and the file to optimize search speed.
    // Example: "file.txt", "github/myproject/foo.txt"
    private string ending_path;

    public static void SetRootPrefix(string prefix) {
        Log.Info($"DirectoryEntry._abs_dir_prefix = {prefix}");
        Debug.Assert(prefix.EndsWith("/"));
        _abs_dir_prefix = prefix;
    }

    public DirectoryEntry(string ending_path) {
        this.ending_path = ending_path;
    }

    public string GetDisplayString() => TextHelper.GetNiceFileDisplayString(ending_path);

    public string GetChoiceString() {
        return _abs_dir_prefix + ending_path;
    }

    public string GetSearchString() => ending_path;
}

readonly record struct QueuedDirectory(string path, IgnoreModel ignores);


class DirectoryExplorer
{
    private const int FILES_PROGRESS = 1000;
    private const int DIRS_PROGRESS = 100;
    private Queue<QueuedDirectory> dir_queue = new();
    private int nr_files = 0;
    private int nr_dirs = 0;
    private int nr_ignored = 0;
    private string rootAbsPath;
    private EventQueue<Notification> m_notifications;
    private Config m_config;

    public DirectoryExplorer(Config config, string rootPath, EventQueue<Notification> notifications) {
        rootAbsPath = Path.GetFullPath(rootPath);
        if (!rootAbsPath.EndsWith("/")) {
            rootAbsPath += "/";
        }
        m_notifications = notifications;
        m_config = config;
    }

    public IEnumerable<ISelectableItem> Scan()
    {
        Log.Info($"ScanDirectory: rootAbsPath={rootAbsPath}");
        DirectoryEntry.SetRootPrefix(rootAbsPath);
        var initialIgnorer = new IgnoreModel(rootAbsPath, m_config.GitIgnoresEnabled);

        List<DirectoryEntry> entries = new();
        dir_queue.Enqueue(new QueuedDirectory(rootAbsPath, initialIgnorer));

        while (dir_queue.Count > 0) {
            QueuedDirectory context = dir_queue.Dequeue();
            string dirAbsPath = context.path;
            IgnoreModel ignores = context.ignores;
            Debug.Assert(dirAbsPath.EndsWith("/"));
            Debug.Assert(dirAbsPath.StartsWith(rootAbsPath));

            // See if there is a new .gitignore file to load, before listing files/dirs
            string newGitIgnoreFile = dirAbsPath + "/.gitignore";
            if (File.Exists(newGitIgnoreFile) && dirAbsPath != rootAbsPath && m_config.GitIgnoresEnabled) {
                ignores = new IgnoreModel(newGitIgnoreFile, context.ignores);
            }

            string[]? allFiles = null;
            try {
                //Log.Info($"reading files in {dirAbsPath}");
                allFiles = Directory.GetFiles(dirAbsPath);
            } catch (System.UnauthorizedAccessException e) {
                Log.Info($"Caught: {e}");
                // Fatal error if we can't access the root path
                if (dirAbsPath == rootAbsPath) {
                    throw;
                }
            }
            if (allFiles != null) {
                SortStrings(allFiles);
                foreach (string f in allFiles) {
                    if (ignores.IsIgnored(f)) {
                        nr_ignored++;
                        continue;
                    }
                    //Log.Info($"read file {f}");
                    var dentry = new DirectoryEntry(f.Substring(rootAbsPath.Length));
                    entries.Add(dentry);

                    nr_files++;
                    if ((nr_files % FILES_PROGRESS) == 0) {
                        ShowProgress();
                    }
                }
            }

            string[]? allDirs = null;
            try {
                //Log.Debug($"reading dirs in {dirAbsPath}");
                allDirs = Directory.GetDirectories(dirAbsPath);
            } catch (System.UnauthorizedAccessException e) {
                Log.Info($"Caught: {e}");
            }
            if (allDirs != null) {
                SortStrings(allDirs);
                // Dirs, need to be queued.
                // It'also nice seeing a BFS tree growing down and to the right in the UX.
                foreach (string origDir in allDirs) {
                    // Ensure we match a gitignore rule like 'node_modules/'
                    // Also this loop require all dirs to end with /
                    var d = (origDir.EndsWith("/") ? origDir : origDir + "/");
                    if (ignores.IsIgnored(d)) {
                        nr_ignored++;
                        continue;
                    }
                    if (IsSymbolicLink(d)) {
                        Log.Info($"Not following symlink {d}");
                        continue;
                    }
                    //Log.Info($"read dir {d}");
                    dir_queue.Enqueue(new QueuedDirectory(d, ignores));

                    nr_dirs++;
                    if ((nr_dirs % DIRS_PROGRESS) == 0) {
                        ShowProgress();
                    }
                }
            }
        }

        Log.Info($"nr_files={nr_files}, nr_dirs={nr_dirs}");
        return entries;
    }

    private void ShowProgress() {
        var progress = new Notification(Processed: nr_files + nr_dirs, Ignored: nr_ignored);
        m_notifications.AddEvent(progress);
    }

    private void SortStrings(string[] arr) {
        Array.Sort(arr);
    }

    static private bool IsSymbolicLink(string path) {
        FileInfo pathInfo = new FileInfo(path);
        return pathInfo.Attributes.HasFlag(System.IO.FileAttributes.ReparsePoint);
    }

}
