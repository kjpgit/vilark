// Copyright (C) 2023 Karl Pickett / Vilark Project
using System.Diagnostics;
namespace vilark;

// This can be a file, or a buffer, or a simple string choice
interface ISelectableItem
{
    // This is what the user sees, e.g. "github/myproject | foo.txt"
    public string GetDisplayString();

    // This is what the external program receives, e.g. /home/bob/github/myproject/foo.txt
    // Note that for files, we always return absolute paths
    public string GetChoiceString();

    // This is what the user searches on, which is a subset of the path.
    // example: github/myproject/foo.txt
    public string GetSearchString();
}




class InputModel
{
    private IEnumerable<ISelectableItem>? _data = null;
    private IEnumerable<ISelectableItem>? _filtered_data = null;
    private Thread? _load_thread = null;
    private OptionsModel m_options;
    private Config m_config;
    private EventQueue<Notification> m_notifications;

    // This is null when we are still loading
    public IEnumerable<ISelectableItem>? FilteredData => _filtered_data;

    public InputModel(OptionsModel options, Config config, EventQueue<Notification> notifications) {
        m_config = config;
        m_options = options;
        m_notifications = notifications;
    }

    public void StartLoadingAsync() {
        _load_thread = new Thread(() => {
                LoadInput(m_options.SelectedDirectory ?? ".");
                });
        _load_thread.Name = "fileFinder";
        _load_thread.Start();
    }

    private void LoadInput(string selectedDirectory) {
        try {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var entries = LoadInputImpl(selectedDirectory);
            stopwatch.Stop();
            Log.Info($"LoadInputImpl completed in {stopwatch.Elapsed}");
            m_notifications.AddEvent(new Notification(CompletedData: entries));
        } catch (Exception e) {
            m_notifications.AddEvent(new Notification(ErrorMessage: e.ToString()));
        }
    }

    private IEnumerable<ISelectableItem> LoadInputImpl(string selectedDirectory) {
        var inputFileName = Environment.GetEnvironmentVariable("VILARK_INPUT_FILE");
        if (inputFileName != null) {
            // An explicit input file was given.
            // Don't apply any ignore rules on it, use it exactly.
            string? inputDisplayMode = Environment.GetEnvironmentVariable("VILARK_INPUT_DISPLAY");
            bool displayAsFile = (inputDisplayMode == "DIR_BAR_FILE");
            List<ExternalInputEntry> entries = new();
            using (var inFile = File.OpenText(inputFileName)) {
                while (true) {
                    var line = inFile.ReadLine();
                    if (line == null) {
                        break;
                    }
                    entries.Add(new ExternalInputEntry(line, displayAsFile:displayAsFile));
                }
            }
            return entries;
        } else {
            // Load files, recursively, and honor all ignore rules
            var explorer = new DirectoryExplorer(m_config, selectedDirectory, m_notifications);
            var entries = explorer.Scan();
            return entries;
        }
    }

    public void SetCompletedData(IEnumerable<ISelectableItem> data) {
        _data = data;
    }

    public void SetSearchFilter(string searchText, FuzzySearchMode mode) {
        if (_data == null) {
            return;
        }
        if (searchText != String.Empty) {
            List<ISelectableItem> filtered = new();
            var query = new FuzzyTextQuery(searchText, mode);
            foreach (var dentry in _data) {
                var result = query.RunQuery(dentry.GetSearchString());
                if (result.is_complete_match) {
                    filtered.Add(dentry);
                }
            }
            _filtered_data = filtered;
        } else {
            _filtered_data = _data;
        }
    }
}
