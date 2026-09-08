using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace CodexCreditMonitor;

// Use the interactive shell as the real parent, including its token and job
// membership. Shell.Application created in the caller does not guarantee this.
internal static class IndependentLaunch
{
    internal const string Marker = "--ccm-independent";

    internal static bool IsIndependent()
    {
        var shell = GetShellWindow();
        if (shell == IntPtr.Zero) return false;
        GetWindowThreadProcessId(shell, out var shellPid);
        using var process = Process.GetCurrentProcess();
        if (!IsProcessInJob(process.Handle, IntPtr.Zero, out var inJob) || inJob) return false;
        var snapshot = CreateToolhelp32Snapshot(2, 0);
        if (snapshot == new IntPtr(-1)) return false;
        try
        {
            var entry = new ProcessEntry { Size = (uint)Marshal.SizeOf<ProcessEntry>() };
            if (!Process32First(snapshot, ref entry)) return false;
            do
            {
                if (entry.ProcessId == process.Id) return entry.ParentProcessId == shellPid;
            } while (Process32Next(snapshot, ref entry));
            return false;
        }
        finally { CloseHandle(snapshot); }
    }

    internal static int Start(string executable, IEnumerable<string> arguments, string directory)
    {
        var shell = GetShellWindow();
        if (shell == IntPtr.Zero) throw new InvalidOperationException("Windows Explorer is not available.");
        GetWindowThreadProcessId(shell, out var shellPid);
        var parent = OpenProcess(0x0080, false, shellPid); // PROCESS_CREATE_PROCESS
        if (parent == IntPtr.Zero) throw new Win32Exception();
        IntPtr list = IntPtr.Zero, parentValue = IntPtr.Zero;
        var initialized = false;
        var child = new ProcessInformation();
        try
        {
            nuint size = 0;
            InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref size);
            list = Marshal.AllocHGlobal(checked((int)size));
            if (!InitializeProcThreadAttributeList(list, 1, 0, ref size)) throw new Win32Exception();
            initialized = true;
            parentValue = Marshal.AllocHGlobal(IntPtr.Size);
            Marshal.WriteIntPtr(parentValue, parent);
            if (!UpdateProcThreadAttribute(list, 0, (IntPtr)0x00020000, parentValue,
                    (nuint)IntPtr.Size, IntPtr.Zero, IntPtr.Zero)) throw new Win32Exception();
            var startup = new StartupInfoEx
            {
                StartupInfo = new StartupInfo { Size = Marshal.SizeOf<StartupInfoEx>(), Desktop = "winsta0\\default" },
                AttributeList = list
            };
            var command = new StringBuilder(string.Join(" ", new[] { executable }.Concat(arguments).Select(Quote)));
            // Do not inherit handles from either the agent or Explorer. Suspend
            // until job membership is checked so a failed hand-off never runs.
            if (!CreateProcess(executable, command, IntPtr.Zero, IntPtr.Zero, false,
                    0x00080000 | 0x00000004, IntPtr.Zero, directory, ref startup, out child))
                throw new Win32Exception();
            if (!IsProcessInJob(child.Process, IntPtr.Zero, out var inJob)) throw new Win32Exception();
            if (inJob) throw new InvalidOperationException("The monitor is still attached to a Windows job.");
            if (ResumeThread(child.Thread) == uint.MaxValue) throw new Win32Exception();
            return (int)child.ProcessId;
        }
        catch
        {
            if (child.Process != IntPtr.Zero) TerminateProcess(child.Process, 1);
            throw;
        }
        finally
        {
            if (child.Thread != IntPtr.Zero) CloseHandle(child.Thread);
            if (child.Process != IntPtr.Zero) CloseHandle(child.Process);
            if (initialized) DeleteProcThreadAttributeList(list);
            if (list != IntPtr.Zero) Marshal.FreeHGlobal(list);
            if (parentValue != IntPtr.Zero) Marshal.FreeHGlobal(parentValue);
            CloseHandle(parent);
        }
    }

    // Windows argv escaping: only double backslashes before a quote or the
    // closing delimiter. Doubling every backslash corrupts paths with spaces.
    internal static string Quote(string value)
    {
        var result = new StringBuilder("\"");
        var slashes = 0;
        foreach (var character in value)
        {
            if (character == '\\') { slashes++; continue; }
            result.Append('\\', character == '"' ? slashes * 2 + 1 : slashes);
            result.Append(character);
            slashes = 0;
        }
        return result.Append('\\', slashes * 2).Append('"').ToString();
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct StartupInfo
    {
        public int Size;
        public string? Reserved, Desktop, Title;
        public uint X, Y, XSize, YSize, XCountChars, YCountChars, FillAttribute, Flags;
        public ushort ShowWindow, ReservedSize;
        public IntPtr ReservedPointer, StandardInput, StandardOutput, StandardError;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct StartupInfoEx { public StartupInfo StartupInfo; public IntPtr AttributeList; }
    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation { public IntPtr Process, Thread; public uint ProcessId, ThreadId; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ProcessEntry
    {
        public uint Size, Usage, ProcessId;
        public UIntPtr DefaultHeapId;
        public uint ModuleId, Threads, ParentProcessId;
        public int BasePriority;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string ExeFile;
    }
    [DllImport("user32.dll")] private static extern IntPtr GetShellWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr OpenProcess(uint access, bool inherit, uint id);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool InitializeProcThreadAttributeList(IntPtr list, int count, uint flags, ref nuint size);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool UpdateProcThreadAttribute(IntPtr list, uint flags, IntPtr attribute, IntPtr value, nuint size, IntPtr previous, IntPtr returned);
    [DllImport("kernel32.dll")] private static extern void DeleteProcThreadAttributeList(IntPtr list);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CreateProcessW")]
    private static extern bool CreateProcess(string app, StringBuilder command, IntPtr processAttributes, IntPtr threadAttributes, bool inheritHandles, uint flags, IntPtr environment, string directory, ref StartupInfoEx startup, out ProcessInformation information);
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool IsProcessInJob(IntPtr process, IntPtr job, out bool result);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern uint ResumeThread(IntPtr thread);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool TerminateProcess(IntPtr process, uint exitCode);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool Process32First(IntPtr snapshot, ref ProcessEntry entry);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool Process32Next(IntPtr snapshot, ref ProcessEntry entry);
}
