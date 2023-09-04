// C# Doesn't seem to have an exec() API
// (Replacing current process)
//
using Linux = Tmds.Linux;
using Libc = Tmds.Linux.LibC;
using System.Text;
using System.Collections;

class LinuxProcess
{
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

        Log.Info($"Exec: program {program} -> fullPath {fullPath}");

        unsafe {
            byte* path_ptr = stackalloc byte[Encoding.UTF8.GetByteCount(fullPath) + 1];
            _write_c_string_utf8(fullPath, path_ptr);

            byte* args_data = stackalloc byte[_get_cstr_array_length_utf8(args)];
            byte* envs_data = stackalloc byte[_get_cstr_array_length_utf8(envs)];
            byte** args_ptrs = stackalloc byte*[args.Length + 1];
            byte** envs_ptrs = stackalloc byte*[envs.Length + 1];
            _build_cstr_array_utf8(args, args_data, args_ptrs);
            _build_cstr_array_utf8(envs, envs_data, envs_ptrs);

            int ret = Libc.execve(path_ptr, args_ptrs, envs_ptrs);
        }
    }

    /*
     * Return how many bytes are needed, including null terminators, to
     * build a c-array containing the data of all strings in `a`
     */
    private static int _get_cstr_array_length_utf8(string[] string_array) {
        int nr_bytes = 0;
        foreach (string s in string_array) {
            nr_bytes += Encoding.UTF8.GetByteCount(s) + 1;
        }
        return nr_bytes;
    }

    unsafe private static void _build_cstr_array_utf8(string[] string_array, byte* dest, byte**ptrs) {
        int string_num = 0;
        for ( ; string_num < string_array.Length; string_num++) {
            string s = string_array[string_num];
            ptrs[string_num] = dest;
            foreach (byte b in Encoding.UTF8.GetBytes(s)) {
                *dest++ = b;
            }
            *dest++ = 0;
        }
        ptrs[string_num] = null;
    }

    unsafe private static void _write_c_string_utf8(string s, byte* dest) {
        foreach (byte b in Encoding.UTF8.GetBytes(s)) {
            *dest++ = b;
        }
        *dest++ = 0;
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

}
