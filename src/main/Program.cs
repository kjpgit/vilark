using System.Collections;
using System.Runtime.InteropServices;
using vilark;

class ViLarkMain
{
    public const string VERSION = "1.3";

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
        var keyboardEvent = new InputEvent<KeyPress>();
        var signalEvent = new InputEvent<PosixSignal>();
        var controller = new Controller(console, keyboardEvent, signalEvent, optionsModel);

        // Synchronization is important here.. one signal at a time is processed by main thread,
        // and we wait until it is done before returning to the runtime.
        var captureSignal = (PosixSignalContext context, bool cancel) => {
            Log.Info($"Signal start: {context.Signal}");
            signalEvent.ProducerWaitHandle.WaitOne();
            signalEvent.AddEvent(context.Signal);
            controller.SignalProcessingDone.WaitOne();
            context.Cancel = cancel;
            Log.Info($"Signal end: {context.Signal}");
        };

        UnixProcess.RegisterSignalHandler(PosixSignal.SIGWINCH, (context) => captureSignal(context, false));
        UnixProcess.RegisterSignalHandler(PosixSignal.SIGTERM, (context) => captureSignal(context, false));

        // We get this if the user resumes after CTRL-Z SUSPend
        // We redraw and re-enable alternate screen when this happens
        // Also, the .NET runtime restores termios as long as we don't cancel it.
        UnixProcess.RegisterSignalHandler(PosixSignal.SIGCONT, (context) => captureSignal(context, false));

        // CTRL-\ sends this, we don't want it to kill us, so we cancel it
        UnixProcess.RegisterSignalHandler(PosixSignal.SIGQUIT, (context) => captureSignal(context, true));

        // CTRL-Z handler, if it comes as a signal.
        // Unfortunately, .NET doesn't support reading it as a keypress.
        //
        // The choices we have:
        // 1) Capture and ignore signal = user can't suspend with ctrl-Z
        // 2) Default signal = suspend app but keeps us on application screen (doesn't switch to shell)
        // 3) Capture and switch back to main screen, then send ourselves a SIGSTOP.
        // We go for option 3)
        //
        // Feature request filed: https://github.com/dotnet/runtime/issues/91709
        //
        UnixProcess.RegisterSignalHandler(PosixSignal.SIGTSTP, (context) => captureSignal(context, true));

        // CTRL-C
        // This is terrible if left at the default, it kills us and leaves us on the app screen.
        // So we catch it, do basic terminal cleanup, and exit.
        //
        // The default action seems to race with our handler (50/50), so we cancel it.
        //
        // I don't know of many full screen apps that do exit on ctrl-c (tig does), but
        // we're just a simple dialog so it's probably fine.
        UnixProcess.RegisterSignalHandler(PosixSignal.SIGINT, (context) => captureSignal(context, false));


        // Read from keyboard/tty in a separate thread.
        var consoleReadThread = new Thread(() => {
                keyboardEvent.ProducerWaitHandle.WaitOne();
                foreach (KeyPress kp in keyboard.GetKeyPress()) {
                    keyboardEvent.AddEventAndWait(kp);
                }
            });
        consoleReadThread.Name = "consoleReadThread";
        consoleReadThread.Start();

        controller.Run(args);
    }

}
