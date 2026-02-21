namespace AudioVisualizer.Infrastructure.Windows.Interop;

internal static class Consts
{
    // Окно и GDI
    public const uint CsVRedraw = 0x0001;
    public const uint CsHRedraw = 0x0002;
    public const uint WsOverlappedWindow = 0x00CF0000;
    public const int CwUseDefault = unchecked((int)0x80000000);
    public const int SwShow = 5;

    // Сообщения Windows
    public const uint WmSize = 0x0005;
    public const uint WmDestroy = 0x0002;
    public const uint WmQuit = 0x0012;
    public const uint WmSettingChange = 0x001A;
    public const uint WmTimer = 0x0113;
    public const uint WmEnterSizeMove = 0x0231;
    public const uint WmExitSizeMove = 0x0232;
    public const uint WmSizing = 0x0214;
    public const uint WmMoving = 0x0216;
    public const uint WmClose = 0x0010;
    public const uint WmEraseBkgnd = 0x0014;

    // DWM (Отрисовка и темы)
    public const int DwmSbtMainWindow = 2;
    public const int DwmSbtTransientWindow = 3;
    public const int DwmWaUseImmersiveDarkMode = 20;
    public const int DwmWaSystemBackdropType = 38;

    // Параметры GDI и отрисовки
    public const uint BiRgb = 0;
    public const uint DibRgbColors = 0;
    public const uint PmRemove = 0x0001;
    public const uint SrcCopy = 0x00CC0020;
    public const int IdcArrow = 32512;

    // Прозрачность
    public const uint LwaColorKey = 0x00000001;
    public const uint LwaAlpha = 0x00000002;

    // Константы WASAPI
    public const uint AudclntStreamflagsLoopback = 0x00020000;
    public const uint AudclntStreamflagsEventcallback = 0x00040000;
    public const string DeviceEnumeratorClsid = "BCDE0395-E52F-467C-8E3D-C4579291692E";


}
