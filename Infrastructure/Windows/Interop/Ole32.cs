using System.Runtime.InteropServices;

using static AudioVisualizer.Infrastructure.Windows.Interop.ComInterfaces;

namespace AudioVisualizer.Infrastructure.Windows.Interop;

internal static partial class Ole32
{
    [LibraryImport("ole32.dll")]
    public static partial int CoCreateInstance(in Guid rclsid, IntPtr pUnkOuter, uint dwClsContext, in Guid riid, out IMMDeviceEnumerator ppv);
}