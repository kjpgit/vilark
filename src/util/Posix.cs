using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

/*
 * This provides access to execve() on linux and mac, because:
 *
 * - Mono.Posix doesn't seem to work with AOT.
 * - Tmds.Linux is Linux only.
 * - .NET runtime doesn't expose it for us
 */

namespace vilark;

class Posix
{
    static public unsafe int NativeExecve(string path, string[] args, string[] envs)
    {
        // Put args and envs on the heap
        byte[] args_data = new byte[_get_cstr_array_length_utf8(args)];
        byte[] envs_data = new byte[_get_cstr_array_length_utf8(envs)];

        fixed (byte *args_data_p = &args_data[0], envs_data_p = &envs_data[0]) {
            byte* path_ptr = stackalloc byte[Encoding.UTF8.GetByteCount(path) + 1];
            _write_c_string_utf8(path, path_ptr);

            byte** args_ptrs = stackalloc byte*[args.Length + 1];
            byte** envs_ptrs = stackalloc byte*[envs.Length + 1];
            _build_cstr_array_utf8(args, args_data_p, args_ptrs);
            _build_cstr_array_utf8(envs, envs_data_p, envs_ptrs);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                return LinuxSyscalls.execve(path_ptr, args_ptrs, envs_ptrs);
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                return MacSyscalls.execve(path_ptr, args_ptrs, envs_ptrs);
            } else {
                throw new Exception("Unsupported OS Platform");
            }
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

    private static unsafe void _build_cstr_array_utf8(string[] string_array, byte* dest, byte**ptrs) {
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

    private static unsafe void _write_c_string_utf8(string s, byte* dest) {
        foreach (byte b in Encoding.UTF8.GetBytes(s)) {
            *dest++ = b;
        }
        *dest++ = 0;
    }

}

[SupportedOSPlatform("linux")]
public static unsafe class LinuxSyscalls
{
    public const string linux_libc = "libc.so.6";

    [DllImport(linux_libc, SetLastError = true)]
    public static extern int execve(byte* filename, byte** args, byte** envs);
}

[SupportedOSPlatform("macos")]
public static unsafe class MacSyscalls
{
    public const string mac_libc = "libSystem";

    [DllImport(mac_libc, SetLastError = true)]
    public static extern int execve(byte* filename, byte** args, byte** envs);
}
