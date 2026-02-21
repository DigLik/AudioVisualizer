using System.Runtime.Versioning;

using Microsoft.Win32;

using static AudioVisualizer.Infrastructure.Windows.Interop.Consts;
using static AudioVisualizer.Infrastructure.Windows.Interop.DwmApi;

namespace AudioVisualizer.Infrastructure.Windows;

[SupportedOSPlatform("windows")]
public static partial class ThemeHelper
{
    public static bool IsSystemInDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");

            if (value != null)
                return Convert.ToInt32(value) == 0;
        }
        catch { }
        return true;
    }

    public static void ApplyThemeToWindow(IntPtr hwnd, bool isDarkMode)
    {
        if (hwnd == IntPtr.Zero) return;
        int isDark = isDarkMode ? 1 : 0;
        DwmSetWindowAttribute(hwnd, DwmWaUseImmersiveDarkMode, ref isDark, sizeof(int));
    }
}