using System.Runtime.InteropServices;

namespace AudioVisualizer.Infrastructure.Windows.Interop;

internal static partial class Kernel32
{
    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW")]
    public static partial IntPtr GetModuleHandle(IntPtr lpModuleName);

    [LibraryImport("kernel32.dll", EntryPoint = "CreateEventW")]
    public static partial IntPtr CreateEventW(IntPtr lpEventAttributes, [MarshalAs(UnmanagedType.Bool)] bool bManualReset, [MarshalAs(UnmanagedType.Bool)] bool bInitialState, IntPtr lpName);

    [LibraryImport("kernel32.dll")]
    public static partial uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(IntPtr hObject);
}
