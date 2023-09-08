namespace vilark;

class OutputModel
{
    public OutputModel() { }

    public void WriteOutput(IScrollItem? chosenItem) {

        // Truncate the file to empty if nothing was chosen.
        var outFileName = Environment.GetEnvironmentVariable("VILARK_OUTPUT_FILE");
        if (!String.IsNullOrEmpty(outFileName)) {
            // Use FileMode.Create so the file is truncated if it exists.
            using (var fs = new FileStream(outFileName, FileMode.Create)) {
                if (chosenItem != null) {
                    var s = chosenItem.GetSelectionString();
                    Log.Info($"Chosen item: [{s}]");
                    fs.Write(System.Text.Encoding.UTF8.GetBytes(s));
                } else {
                    Log.Info($"No item chosen");
                }
            }
        }

        // Launch a process, but only if something was chosen.
        var launchCmd = Environment.GetEnvironmentVariable("EDITOR");
        if (!String.IsNullOrEmpty(launchCmd) && chosenItem != null) {
            var s = chosenItem.GetSelectionString();
            Log.Info($"Launching EDITOR {launchCmd} {s}");
            string shortName = Path.GetFileName(launchCmd);
            string[] args = { shortName, s };
            UnixProcess.Exec(launchCmd, args, UnixProcess.GetCurrentEnvs());
        }
    }
}
