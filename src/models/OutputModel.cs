// Copyright (C) 2023 Karl Pickett / Vilark Project
namespace vilark;

class OutputModel
{
    private Config m_config;

    public OutputModel(Config config) {
        m_config = config;
    }

    // IF the user does not want to launch a process, returns null
    public string? GetEditorCommand() {
        var ret = Environment.GetEnvironmentVariable("EDITOR");
        return String.IsNullOrEmpty(ret) ? null : ret;
    }

    // IF the user does not want to write the chosen selection, returns null
    public string? GetOutputFile() {
        var ret = Environment.GetEnvironmentVariable("VILARK_OUTPUT_FILE");
        return String.IsNullOrEmpty(ret) ? null : ret;
    }

    // Write the chosen selection (even if it is empty), to the output file.
    // Truncate the file to empty if nothing was chosen.
    // Do nothing if VILARK_OUTPUT_FILE is not set.
    public void WriteOutput(ISelectableItem? chosenItem) {
        var outFileName = GetOutputFile();
        if (outFileName != null) {
            // Use FileMode.Create so the file is truncated if it exists.
            using (var fs = new FileStream(outFileName, FileMode.Create)) {
                if (chosenItem != null) {
                    var s = chosenItem.GetChoiceString();
                    Log.Info($"Chosen item: [{s}]");
                    fs.Write(System.Text.Encoding.UTF8.GetBytes(s));
                } else {
                    Log.Info($"No item chosen");
                }
            }
        }
    }

    // Launch a process, but only if something was chosen, and an editor is set.
    public void LaunchEditor(ISelectableItem? chosenItem, EventQueue<Notification> notifications) {
        var editorCommand = GetEditorCommand();
        if (editorCommand != null && chosenItem != null) {
            var s = chosenItem.GetChoiceString();
            Log.Info($"Launching EDITOR {m_config.EditorLaunchMode} {editorCommand} {s}");
            string shortName = Path.GetFileName(editorCommand);
            string[] args = { shortName, s };

            if (m_config.EditorLaunchMode == EditorLaunchMode.EDITOR_LAUNCH_REPLACE) {
                var envs = UnixProcess.GetCurrentEnvs();
                envs = envs.Where(val => !val.StartsWith("VILARK_IPC_URL")).ToArray();
                UnixProcess.Exec(editorCommand, args, envs);
            } else {
                UnixProcess.StartChild(editorCommand, args, notifications);
            }
        }
    }
}

