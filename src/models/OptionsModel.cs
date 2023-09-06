namespace vilark;

class OptionsModel
{
    public string? SelectedDirectory = null;

    public OptionsModel() {
        bool explicit_options_end = false;
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 1; i < args.Length; i++) {
            string arg = args[i];
            if (!explicit_options_end) {
                if (arg == "-h" || arg == "--help") {
                    ShowVersion();
                    ShowHelp();
                    Environment.Exit(0);
                } else if (arg == "--version") {
                    ShowVersion();
                    Environment.Exit(0);
                } else if (arg == "--") {
                    explicit_options_end = true;
                } else if (arg.StartsWith("-")) {
                    ShowUnknownOption(arg);
                    Environment.Exit(2);
                } else {
                    ParseNonOption(arg);
                }
            } else {
                ParseNonOption(arg);
            }
        }
    }

    private void ShowHelp() {
        System.Console.Write(
$@"

Usage:

    vilark
        Browse files, recursively, in current directory

    vilark <DIRECTORY>
        Browse files, recursively, in specified directory

    If a file is chosen, $EDITOR is executed to open it.

    vilark ---version
        Show version information

    vilark -h
        Show help message

");
    }

    private void ShowVersion() {
        System.Console.Write(
$@"
ViLark version {ViLarkMain.VERSION}
Copyright (2023) Karl Pickett / ViLark Project
https://github.com/kjpgit/vilark/
");
    }

    private void ShowUnknownOption(string opt) {
        System.Console.WriteLine($"Error: Unknown recognized option '{opt}'");
        System.Console.WriteLine($"Use -h for help");
    }

    private void ParseNonOption(string arg) {
        if (SelectedDirectory != null) {
            System.Console.WriteLine($"Error: only one directory is allowed");
            System.Console.WriteLine($"Use -h for help");
            Environment.Exit(2);
        }
        SelectedDirectory = arg;
    }

}
