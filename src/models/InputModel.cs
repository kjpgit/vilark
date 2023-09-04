namespace vilark;

class InputModel
{
    private Config m_config;
    private IEnumerable<IScrollItem> _data = null!;
    private IEnumerable<IScrollItem> _filtered_data = null!;

    public IEnumerable<IScrollItem> FilteredData => _filtered_data;

    public InputModel(Config config) {
        m_config = config;
    }

    public void LoadInput(OptionsModel optionsModel) {
        var inputFileName = Environment.GetEnvironmentVariable("VILARK_INPUT_FILE");
        string? inputDisplayMode = Environment.GetEnvironmentVariable("VILARK_INPUT_DISPLAY");
        bool displayAsFile = (inputDisplayMode == "DIR_BAR_FILE");
        if (inputFileName != null) {
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
            _data = entries;
        } else {
            var exp = new DirectoryExplorer();
            string path = optionsModel.SelectedDirectory ?? ".";
            _data = exp.ScanDirectory(path).ToList();
        }
    }

    public void SetSearchFilter(string searchText) {
        if (searchText != String.Empty) {
            List<IScrollItem> filtered = new();
            var query = new FuzzyTextQuery(searchText, m_config.FuzzySearchMode);
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
