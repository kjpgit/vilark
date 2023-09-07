using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace vilark;


/*
 * When launching a child process, these things need to be considered:
 *
 * 1. Putting the terminal back to the 'main' screen.  Even if this is only for a moment,
 *    the convention is that each process leaves the terminal as it was when they started.
 *    Whatever we leave it at, is what the next process will leave it at, by default.
 *
 * 2. Resetting all terminal colors/underlining back to the default, and enabling the cursor.
 *    Note that 1) and 2) can be done easily just by write()ing escape codes to /dev/tty.
 *
 * 3. Setting the mode of /dev/tty back to cooked/echo, or at least how it was when you started.
 *    This needs an ioctl(), sadly.  Or, you hack it and run 'tput init' in a subprocess.
 *
 * 4. Restoring signal masks to the default.  execve() does this for you, otherwise
 *    if you are using .NET Process.Start(), you should unregister your signal handlers first.
 *
 * 5. Making sure nothing else is still reading from /dev/tty.  Again, execve() does this for you,
 *    because all threads are terminated.  The .NET ReadKey() API is awkward to cancel otherwise.
 *
 */


class UnixProcess
{
    static private List<PosixSignalRegistration> _signal_registrations = new();

    static public void RegisterSignalHandler(PosixSignal signal, Action<PosixSignalContext> handler) {
        var registration  = PosixSignalRegistration.Create(signal, handler);
        _signal_registrations.Add(registration);
    }

    static private void UnregisterSignals() {
        foreach (var registration in _signal_registrations) {
            registration.Dispose();
        }
        _signal_registrations.Clear();
    }

    // Replace this process with a different process.
    // Gracefully handle cleanup tasks 3) and 4) listed at the top of this file.
    // The caller must have already done 1) and 2).
    static public void Exec(string program, string[] args, string[] envs) {
        string? fullPath = null;
        if (program.IndexOf('/') == -1) {
            fullPath = GetExecFullPath(program);
            if (fullPath == null) {
                throw new Exception($"Can't find {program} in $PATH");
            }
        } else {
            fullPath = program;
        }

        Log.Info($"Full path: {program} -> {fullPath}");

        // "When Vim exits the terminal will be put back into the mode it was
        // before Vim started."
        RunTerminalReset();

        // This should not return...
        Posix.NativeExecve(fullPath, args, envs);

        throw new Exception("Error: native execve failed for {fullPath}");
    }

    static public void ExecManaged(string program, string[] args, string[] envs) {
        UnregisterSignals();
        RunTerminalReset();
        var pi = new ProcessStartInfo(program);
        foreach (var arg in args.Skip(1)) {
            pi.ArgumentList.Add(arg);
        }
        var p = Process.Start(pi);
        if (p != null) {
            //Environment.Exit(1);
            p.WaitForExit();
            Log.Info($"exec process finished: {p.ExitCode}");
        } else {
            Log.Info("exec process didn't start");
        }
    }

    public static string[] GetCurrentEnvs() {
        List<string> ret = new();
        var envVars = Environment.GetEnvironmentVariables().Cast<DictionaryEntry>();
        var sortedEnvVars = envVars.OrderBy(x => (string)x.Key);
        foreach (var e in sortedEnvVars) {
            ret.Add($"{e.Key}={e.Value}");
        }
        return ret.ToArray();
    }

    public static string? GetExecFullPath(string program) {
        string PATH = Environment.GetEnvironmentVariable("PATH")!;
        var paths = PATH.Split(":");
        foreach (var path in paths) {
            string fullPath = path + "/" + program;
            if (Path.Exists(fullPath)) {
                return fullPath;
            }
        }
        return null;
    }

    public static void SelfSigStop() {
        var pi = new ProcessStartInfo("sh");
        pi.ArgumentList.Add("-c");
        pi.ArgumentList.Add("kill -STOP $PPID");
        var p = Process.Start(pi);
        if (p != null) {
            p.WaitForExit();
            Log.Info($"stop helper finished: {p.ExitCode}");
        } else {
            Log.Info("stop helper didn't start");
        }
    }

    private static void RunTerminalReset() {
        // Hack: Set our tty back to normal/cooked mode, right before execve()
        // 'reset' also clears the screen, so it isn't great for ctrl-z/ctrl-c handling.
        // 'init' doesn't clear the screen, but seems to put back echo/cooked mode ok.
        //
        // Unfortunately, even Mono.Posix doesn't have an ioctl for this :(
        // Request filed: https://github.com/dotnet/runtime/issues/91710
        //
        var pi = new ProcessStartInfo("tput");
        pi.ArgumentList.Add("init");
        var p = Process.Start(pi);
        if (p != null) {
            Log.Info("tput started");
            p.WaitForExit();
            Log.Info($"tput exit code: {p.ExitCode}");
            //Thread.Sleep(1000);
        } else {
            Log.Info("warning: tput didn't start");
        }
    }
}
