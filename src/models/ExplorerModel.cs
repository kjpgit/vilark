namespace vilark;


class ExternalInputEntry : IScrollItem
{
    required public string itemName;
    public bool displayAsFile = false;

    public string GetSelectionString() => itemName;
    public string GetDisplayString() => displayAsFile ? GetFileDisplayString() : itemName;

    private string GetFileDisplayString() {
        int splitPos = itemName.LastIndexOf('/');
        if (splitPos > 0 && !itemName.EndsWith("/")) {
            string dirPart = itemName.Substring(0, splitPos);
            string filePart = itemName.Substring(splitPos+1);
            return dirPart + " | " + filePart;
        } else {
            return itemName;
        }
    }
}

// NB: structs can't be covariant in List<>
class DirectoryEntry : IScrollItem
{
    required public string DirPath;
    required public string Name;

    public string GetSelectionString() {
        if (DirPath != "") {
            return DirPath + "/" + Name;
        } else {
            return Name;
        }
    }

    public string GetDisplayString() {
        string nicePath = NiceDirPath(DirPath);
        if (nicePath != "") {
            return nicePath + " | " + Name;
        } else {
            return Name;
        }
    }

    private string NiceDirPath(string dirPath) {
        // Don't show useless "./" for a relative path
        if (dirPath.StartsWith("./")) {
            return dirPath.Substring(2);
        } else if (dirPath == ".") {
            return "";
        } else {
            return dirPath;
        }
    }

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
    private string rootPath;
    private InputEvent<LoadProgressInfo> loadEvent;

    public DirectoryExplorer(string rootPath, InputEvent<LoadProgressInfo> loadEvent) {
        this.rootPath = rootPath;
        this.loadEvent = loadEvent;
    }

    public void Scan()
    {
        Log.Info($"ScanDirectory: {rootPath}");
        var initialIgnorer = new IgnoreModel(rootPath);
        List<DirectoryEntry> entries = new();

	// Make the initial root path less ugly
        // The rest we have to clean up during display
        while (rootPath.EndsWith("//")) {
            rootPath = rootPath.Substring(0, rootPath.Length - 1);
        }

        Log.Info($"ScanDirectory: normalized={rootPath}");
        dir_queue.Enqueue(new QueuedDirectory(rootPath, initialIgnorer));

        while (dir_queue.Count > 0) {
            QueuedDirectory context = dir_queue.Dequeue();
            string dirPath = context.path;
            IgnoreModel ignores = context.ignores;

            // See if there is a new .gitignore file to load, before listing files/dirs
            string newGitIgnoreFile = dirPath + "/.gitignore";
            if (File.Exists(newGitIgnoreFile) && dirPath != rootPath) {
                ignores = new IgnoreModel(newGitIgnoreFile, context.ignores);
            }

            string[]? allFiles = null;
            try {
                //Log.Debug($"reading files in {dirPath}");
                allFiles = Directory.GetFiles(dirPath);
            } catch (System.UnauthorizedAccessException e) {
                Log.Info($"Caught: {e}");
                // Fatal error if we can't access the root path
                if (dirPath == rootPath) {
                    loadEvent.AddEvent(new LoadProgressInfo(ErrorMessage: e.ToString()));
                    return;
                }
            }
            if (allFiles != null) {
                foreach (string f in allFiles) {
                    if (ignores.IsIgnored(f)) {
                        nr_ignored++;
                        continue;
                    }
                    //Log.Info($"read file {f}");
                    var dentry = new DirectoryEntry {
                            DirPath= dirPath,
                            Name= Path.GetFileName(f)
                    };
                    entries.Add(dentry);

                    nr_files++;
                    if ((nr_files % FILES_PROGRESS) == 0) {
                        ShowProgress();
                    }
                }
            }

            string[]? allDirs = null;
            try {
                //Log.Debug($"reading dirs in {dirPath}");
                allDirs = Directory.GetDirectories(dirPath);
            } catch (System.UnauthorizedAccessException e) {
                Log.Info($"Caught: {e}");
            }
            if (allDirs != null) {
                // Dirs, need to be queued.
                // It'also nice seeing a BFS tree growing down and to the right in the UX.
                foreach (string d in allDirs) {
                    // Ensure we match a rule like 'node_modules/'
                    if (ignores.IsIgnored(d + "/")) {
                        nr_ignored++;
                        continue;
                    }
                    //Log.Info($"read dir {d}");
                    dir_queue.Enqueue(new QueuedDirectory(d, ignores));

                    nr_dirs++;
                    if ((nr_dirs % DIRS_PROGRESS) == 0) {
                        ShowProgress();
                    }

                    // Don't think we need to care about showing empty dirs.
                    // We can still create a folder structure easily from the file data.
                }
            }
        }

        Log.Info($"nr_files={nr_files}, nr_dirs={nr_dirs}");
        loadEvent.AddEvent(new LoadProgressInfo(CompletedData: entries));
    }

    private void ShowProgress() {
        var progress = new LoadProgressInfo(Processed: nr_files + nr_dirs, Ignored: nr_ignored);
        loadEvent.AddEvent(progress);
    }

}
