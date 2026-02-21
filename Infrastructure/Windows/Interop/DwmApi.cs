using System.Runtime.InteropServices;

namespace AudioVisualizer.Infrastructure.Windows.Interop;

internal static partial class DwmApi
{
    [LibraryImport("dwmapi.dll")]
    public static partial int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    [LibraryImport("dwmapi.dll")]
    public static partial int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS pMarInset);
}
