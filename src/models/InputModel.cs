// Copyright (C) 2023 Karl Pickett / ViLark Project
namespace vilark;


record struct LoadProgressInfo(int? Processed = null, int? Ignored = null,
        IEnumerable<IScrollItem>? CompletedData = null,
        string? ErrorMessage = null);

class InputModel
{
    private IEnumerable<IScrollItem>? _data = null;
    private IEnumerable<IScrollItem>? _filtered_data = null;
    private Thread? _load_thread = null;
    private OptionsModel m_options;
    private EventQueue<LoadProgressInfo> m_load_event;

    // This is null when we are still loading
    public IEnumerable<IScrollItem>? FilteredData => _filtered_data;

    public InputModel(OptionsModel options, EventQueue<LoadProgressInfo> loadEvent) {
        m_options = options;
        m_load_event = loadEvent;
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
            var entries = LoadInputImpl(selectedDirectory);
            m_load_event.AddEvent(new LoadProgressInfo(CompletedData: entries));
        } catch (Exception e) {
            m_load_event.AddEvent(new LoadProgressInfo(ErrorMessage: e.ToString()));
        }
    }

    private IEnumerable<IScrollItem> LoadInputImpl(string selectedDirectory) {
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
                    entries.Add(new ExternalInputEntry {itemName=line, displayAsFile=displayAsFile});
                }
            }
            return entries;
        } else {
            // Load files, recursively, and honor all ignore rules
            var explorer = new DirectoryExplorer(selectedDirectory, m_load_event);
            var entries = explorer.Scan();
            return entries;
        }
    }

    public void SetCompletedData(IEnumerable<IScrollItem> data) {
        _data = data;
    }

    public void SetSearchFilter(string searchText, FuzzySearchMode mode) {
        if (_data == null) {
            return;
        }
        if (searchText != String.Empty) {
            List<IScrollItem> filtered = new();
            var query = new FuzzyTextQuery(searchText, mode);
            foreach (var dentry in _data) {
                var result = query.RunQuery(dentry.GetSelectionString());
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
