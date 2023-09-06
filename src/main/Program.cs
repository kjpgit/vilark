using System.Collections;
using System.Runtime.InteropServices;
using vilark;

class ViLarkMain
{
    public const string VERSION = "1.0";

    static void Main(string[] args)
    {
        Thread.CurrentThread.Name = "mainThread";
        string? debugLogName = Environment.GetEnvironmentVariable("VILARK_DEBUG_LOG");
        string logLevel = Environment.GetEnvironmentVariable("VILARK_DEBUG_LOG_LEVEL") ?? "INFO";

        if (debugLogName != null) {
            Log.Open(debugLogName, logLevel);
        }

        // Log execution env
        foreach (var arg in args) {
            Log.Info($"Main arg: {arg}");
        }
        var envVars = Environment.GetEnvironmentVariables().Cast<DictionaryEntry>();
        var sortedEnvVars = envVars.OrderBy(x => (string)x.Key);
        foreach (var e in sortedEnvVars) {
            Log.Info($"Main env: {e.Key}={e.Value}");
        }

        // Parse CLI options.  This will exit if anything is invalid.
        var optionsModel = new OptionsModel();

        // Options look ok, start up the UX
        var console = new vilark.Console();
        var keyboard = new Keyboard();
        var inputQueue = new InputQueue();
        var controller = new Controller(console, inputQueue, optionsModel);

        // Signals -> inputQueue
        PosixSignalRegistration.Create(PosixSignal.SIGWINCH, (context) => {
                Log.Info("SIGWINCH handler");
                var evt = new InputEvent { signal = PosixSignal.SIGWINCH };
                inputQueue.AddEvent(evt);
                context.Cancel = true;
            });

        PosixSignalRegistration.Create(PosixSignal.SIGTERM, (context) => {
                Log.Info("SIGTERM handler");
                var evt = new InputEvent { signal = PosixSignal.SIGTERM };
                inputQueue.AddEvent(evt);
                context.Cancel = true;
            });

        // Keyboard input -> inputQueue
        var consoleReadThread = new Thread(() => {
                foreach (KeyPress kp in keyboard.GetKeyPress()) {
                    var evt = new InputEvent { keyPress = kp };
                    inputQueue.AddEvent(evt);
                }
            });
        consoleReadThread.Name = "consoleReadThread";
        consoleReadThread.Start();

        controller.Run(args);
    }

}
