// Copyright (C) 2023 Karl Pickett / Vilark Project
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace vilark;

/*
 * When starting a replacement (execve) or child (Process.Start) process, these
 * things need to be considered:
 *
 * 1. How the terminal will be set to the alternate screen, and more importantly, if it
 *    will be returned to the main screen when the process exits.
 *
 *    Vim can stop the usual "put screen back to main on exit" convention if
 *    we set t_ti and t_te to empty at startup, see ":help restorescreen"
 *
 * 2. Resetting all terminal colors/underlining back to the default, and enabling the cursor.
 *    This can be done easily just by write()ing escape codes to /dev/tty.
 *
 * 3. Setting the mode of /dev/tty back to cooked/echo, or at least how it was when you started.
 *    This needs an ioctl(), sadly.  Or, you hack it and run 'stty sane' in a subprocess.
 *
 *    From the vim docs:  "When Vim exits, the terminal will be put back into
 *    the mode it was before Vim started."
 *
 * 4. Restoring signal masks to the default.  execve() does this for you, otherwise
 *    if you are using .NET Process.Start(), perhaps unregister your signal handlers first?
 *
 * 5. Making sure nothing else is still reading from /dev/tty.  execve() does this for you,
 *    because all threads are terminated.  If starting a child with
 *    Process.Start(), ensure no other thread is calling ReadKey().
 *
 * After a lot of failed experiments, the most robust solution for 3-5 is to
 * execve() a wrapper shell command that calls "stty sane" and "exec $target_command"
 * in one shot.  Note that calling them separately was not always working, perhaps because
 * some .NET Console code was being triggered by child processes starting/exiting.
 *
 * The above wrapper command has been tested on Linux and Mac OS 13 (ARM).
 * It's annoying to have to depend on sh and stty for this, however.
 *
 * Note that `stty -g` might work, if you capture it before .Net Console changes anything.
 * I think 'stty sane' will be more robust and predictable.
 *
 */


class UnixProcess
{
    static private List<PosixSignalRegistration> _signal_registrations = new();

    static public void RegisterSignalHandler(PosixSignal signal, Action<PosixSignalContext> handler) {
        var registration  = PosixSignalRegistration.Create(signal, handler);
        _signal_registrations.Add(registration);
    }

    // Replace this process with a different process.
    // Gracefully handle cleanup tasks 3-5 listed at the top of this file.
    // The caller must have already done 1) and 2).
    static public void Exec(string program, string[] args, string[] envs) {
        string? fullPath = null;
        if (program.IndexOf('/') == -1) {
            fullPath = GetExecFullPath(program) ?? throw new Exception($"Can't find {program} in $PATH");
        } else {
            fullPath = program;
        }
        Log.Info($"Full path: {program} -> {fullPath}");

        var wrapperEnvs = new List<string>(envs);
        wrapperEnvs.Add($"VILARK_EXEC_PROG={fullPath}");
        wrapperEnvs.Add($"VILARK_EXEC_ARG1={args[1]}");

        string wrapperCommand = GetExecFullPath("sh") ?? throw new Exception("can't find sh in path");
        string ttyResetCmd = Environment.GetEnvironmentVariable("VILARK_TTY_RESET") ?? "stty sane";
        string[] wrapperArgs = new string[] {
            "sh",
            "-c",
            $"{ttyResetCmd} ; exec \"$VILARK_EXEC_PROG\" \"$VILARK_EXEC_ARG1\"",
        };

        // This should not return...
        Log.Info($"Calling NativeExecve, ttyResetCmd={ttyResetCmd}");
        Posix.NativeExecve(wrapperCommand, wrapperArgs, wrapperEnvs.ToArray());
        //Posix.NativeExecve(fullPath, args, envs);
        throw new Exception("Error: native execve failed for {fullPath}");
    }

    // Use the standard .Net runtime to launch a child process.
    // When it finishes, send an event to the notifications queue.
    static public void StartChild(string program, string[] args,
                    EventQueue<Notification> notifications) {
        //UnregisterSignals();
        //RunTerminalReset();
        var pi = new ProcessStartInfo(program);
        pi.ArgumentList.Add(args[1]);
        var p = Process.Start(pi);
        if (p != null) {
            Task.Run(() => {
                    p.WaitForExit();
                    Log.Info($"exec process finished: {p.ExitCode}");
                    notifications.AddEvent(new Notification(ChildExited: true));
                });
        } else {
            Log.Info("exec process didn't start");
            throw new Exception($"Couldn't start {program}");
        }
    }

    static private void UnregisterSignals() {
        foreach (var registration in _signal_registrations) {
            registration.Dispose();
        }
        _signal_registrations.Clear();
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
        string PATH = Environment.GetEnvironmentVariable("PATH") ?? throw new Exception("PATH not set");
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
        Log.Info($"SelfSigStop: my pid is {Environment.ProcessId}");
        var pi = new ProcessStartInfo("kill");
        pi.ArgumentList.Add("-STOP");
        pi.ArgumentList.Add($"{Environment.ProcessId}");
        var p = Process.Start(pi);
        if (p != null) {
            p.WaitForExit();
            Log.Info($"stop helper finished: pid={p.Id} rc={p.ExitCode}");
        } else {
            Log.Info("stop helper didn't start");
        }
    }

    /*
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
    */
}
