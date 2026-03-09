using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using AudioVisualizer.Application.Configuration;
using AudioVisualizer.Application.Rendering;
using AudioVisualizer.Infrastructure.Windows;
using AudioVisualizer.Infrastructure.Windows.Interop;

namespace AudioVisualizer.Infrastructure.Rendering.Direct2D;

[SupportedOSPlatform("windows")]
public unsafe partial class Direct2DVisualizerRenderer : IVisualizerRenderer
{
    private IntPtr _hwnd;
    private int _width;
    private int _height;
    private volatile bool _shouldClose;
    private volatile bool _isDarkMode;

    private readonly Stopwatch _stopwatch = new();
    private long _lastFrameTime;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private WndProcDelegate _wndProcDelegate = null!;

    private VisualizerConfig _config;

    private Thread _windowThread = null!;
    private readonly ManualResetEventSlim _windowReady = new(false);
    private volatile bool _needsResize;
    private int _targetWidth;
    private int _targetHeight;

    private ID2D1Factory* _factory;
    private ID2D1HwndRenderTarget* _renderTarget;
    private ID2D1SolidColorBrush* _brush;

    public bool ShouldClose => _shouldClose;
    public float DeltaTime { get; private set; }

    public void Initialize(int width, int height, string title, VisualizerConfig config = default)
    {
        _config = config;
        _targetWidth = width;
        _targetHeight = height;
        _wndProcDelegate = WndProc;

        _windowThread = new Thread(() => WindowThreadProc(title))
        {
            Name = "D2DWindowThread",
            IsBackground = true
        };
        _windowThread.SetApartmentState(ApartmentState.STA);
        _windowThread.Start();
        _windowReady.Wait();

        Guid iidFactory = new Guid("06152247-6f50-465a-9245-118bfd3b6007");
        ID2D1Factory* factory = null;
        int hr = D2D1.D2D1CreateFactory(0, in iidFactory, IntPtr.Zero, &factory);
        if (hr != 0) throw new Exception("Failed to initialize Direct2D.");
        _factory = factory;

        _stopwatch.Start();
        _lastFrameTime = _stopwatch.ElapsedTicks;
    }

    private void WindowThreadProc(string title)
    {
        var hInstance = Kernel32.GetModuleHandle(IntPtr.Zero);
        fixed (char* pClassName = "D2DVisualizerClass")
        {
            var wc = new WNDCLASSEX
            {
                cbSize = (uint)sizeof(WNDCLASSEX),
                style = Consts.CsHRedraw | Consts.CsVRedraw,
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
                hInstance = hInstance,
                hCursor = User32.LoadCursor(IntPtr.Zero, Consts.IdcArrow),
                lpszClassName = pClassName
            };
            User32.RegisterClassEx(in wc);

            var rect = new RECT { left = 0, top = 0, right = _targetWidth, bottom = _targetHeight };
            User32.AdjustWindowRect(ref rect, Consts.WsOverlappedWindow, false);

            _hwnd = User32.CreateWindowEx(
                0, "D2DVisualizerClass", title, Consts.WsOverlappedWindow,
                Consts.CwUseDefault, Consts.CwUseDefault,
                rect.right - rect.left, rect.bottom - rect.top,
                IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);
        }

        User32.SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, 0, 0,
            Consts.SwpFrameChanged | Consts.SwpNoMove | Consts.SwpNoSize | Consts.SwpNoZOrder);

        User32.GetClientRect(_hwnd, out RECT clientRect);
        _targetWidth = clientRect.right - clientRect.left;
        _targetHeight = clientRect.bottom - clientRect.top;

        ApplyDwmEffects();
        ApplyTheme();

        User32.ShowWindow(_hwnd, Consts.SwShow);
        _windowReady.Set();

        while (User32.GetMessage(out MSG msg, IntPtr.Zero, 0, 0))
        {
            User32.TranslateMessage(ref msg);
            User32.DispatchMessage(ref msg);
        }

        _shouldClose = true;
    }

    private void ApplyDwmEffects()
    {
        if (!_config.IsWindowTransparent) return;
        var margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
        DwmApi.DwmExtendFrameIntoClientArea(_hwnd, ref margins);

        if (_config.EnableAcrylic && Environment.OSVersion.Version.Build >= 22000)
        {
            int backdropType = Consts.DwmSbtTransientWindow;
            DwmApi.DwmSetWindowAttribute(_hwnd, Consts.DwmWaSystemBackdropType, ref backdropType, sizeof(int));
        }
    }

    private void ApplyTheme()
    {
        _isDarkMode = ThemeHelper.IsSystemInDarkMode();
        ThemeHelper.ApplyThemeToWindow(_hwnd, _isDarkMode);
    }

    private void CreateDeviceResources()
    {
        if (_renderTarget != null) return;

        var rtProps = new D2D1_RENDER_TARGET_PROPERTIES
        {
            type = 0,
            pixelFormat = new D2D1_PIXEL_FORMAT { format = 87, alphaMode = 1 },
            dpiX = 0, dpiY = 0, usage = 0, minLevel = 0
        };

        var hwndRtProps = new D2D1_HWND_RENDER_TARGET_PROPERTIES
        {
            hwnd = _hwnd,
            pixelSize = new D2D1_SIZE_U { width = (uint)_targetWidth, height = (uint)_targetHeight },
            presentOptions = 0
        };

        ID2D1HwndRenderTarget* rt = null;
        if (_factory->CreateHwndRenderTarget(&rtProps, &hwndRtProps, &rt) == 0)
        {
            _renderTarget = rt;

            ID2D1SolidColorBrush* brush = null;
            var color = new D2D1_COLOR_F { r = 1, g = 1, b = 1, a = 1 };
            _renderTarget->CreateSolidColorBrush(&color, IntPtr.Zero, &brush);
            _brush = brush;
        }
    }

    private void DiscardDeviceResources()
    {
        if (_brush != null) { _brush->Release(); _brush = null; }
        if (_renderTarget != null) { _renderTarget->Release(); _renderTarget = null; }
    }

    public void BeginFrame()
    {
        CreateDeviceResources();

        if (_needsResize && _renderTarget != null)
        {
            var size = new D2D1_SIZE_U { width = (uint)_targetWidth, height = (uint)_targetHeight };
            _renderTarget->Resize(&size);
            _width = _targetWidth;
            _height = _targetHeight;
            _needsResize = false;
        }

        long currentFrameTime = _stopwatch.ElapsedTicks;
        DeltaTime = (float)(currentFrameTime - _lastFrameTime) / Stopwatch.Frequency;
        _lastFrameTime = currentFrameTime;

        if (_renderTarget != null)
        {
            _renderTarget->BeginDraw();
            var clearColor = _config.IsWindowTransparent
                ? new D2D1_COLOR_F { r = 0, g = 0, b = 0, a = 0 }
                : (_isDarkMode ? new D2D1_COLOR_F { r = 0, g = 0, b = 0, a = 1 } : new D2D1_COLOR_F { r = 1, g = 1, b = 1, a = 1 });
        }
    }

    public void FillBackground(float r, float g, float b, float a)
    {
        if (_renderTarget == null) return;
        var color = new D2D1_COLOR_F { r = r * a, g = g * a, b = b * a, a = a };
        _renderTarget->Clear(&color);
    }

    public void EndFrame()
    {
        if (_renderTarget != null)
        {
            int hr = _renderTarget->EndDraw(null, null);
            if (hr == unchecked((int)0x8899000C))
                DiscardDeviceResources();
        }
    }

    public void DrawCenteredCircle(float radius)
    {
        if (_renderTarget == null || _brush == null) return;

        var ellipse = new D2D1_ELLIPSE
        {
            point = new D2D1_POINT_2F { x = _width / 2f, y = _height / 2f },
            radiusX = radius,
            radiusY = radius
        };

        float c = _isDarkMode ? 1f : 0f;
        var color = new D2D1_COLOR_F { r = c, g = c, b = c, a = 1f };
        _brush->SetColor(&color);

        _renderTarget->FillEllipse(&ellipse, _brush);
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case Consts.WmNcCalcSize:
            {
                if (wParam != IntPtr.Zero)
                {
                    var nccsp = Marshal.PtrToStructure<NCCALCSIZE_PARAMS>(lParam);
                    int originalTop = nccsp.rgrc0.top;

                    IntPtr result = User32.DefWindowProc(hWnd, msg, wParam, lParam);

                    nccsp = Marshal.PtrToStructure<NCCALCSIZE_PARAMS>(lParam);
                    nccsp.rgrc0.top = originalTop;
                    Marshal.StructureToPtr(nccsp, lParam, false);

                    return result;
                }
                break;
            }

            case Consts.WmNcHitTest:
            {
                if (DwmApi.DwmDefWindowProc(hWnd, msg, wParam, lParam, out IntPtr result))
                    return result;

                User32.GetWindowRect(hWnd, out RECT rect);
                int x = (short)(lParam.ToInt64() & 0xFFFF);
                int y = (short)((lParam.ToInt64() >> 16) & 0xFFFF);

                int borderWidth = 8;

                if (x - rect.left < borderWidth && y - rect.top < borderWidth) return Consts.HtTopLeft;
                if (rect.right - x < borderWidth && y - rect.top < borderWidth) return Consts.HtTopRight;
                if (x - rect.left < borderWidth && rect.bottom - y < borderWidth) return Consts.HtBottomLeft;
                if (rect.right - x < borderWidth && rect.bottom - y < borderWidth) return Consts.HtBottomRight;
                if (x - rect.left < borderWidth) return Consts.HtLeft;
                if (rect.right - x < borderWidth) return Consts.HtRight;
                if (y - rect.top < borderWidth) return Consts.HtTop;
                if (rect.bottom - y < borderWidth) return Consts.HtBottom;

                return Consts.HtCaption;
            }

            case Consts.WmEraseBkgnd:
                return 1;

            case Consts.WmSettingChange:
                if (lParam != IntPtr.Zero && Marshal.PtrToStringUni(lParam) == "ImmersiveColorSet")
                    ApplyTheme();
                return IntPtr.Zero;

            case Consts.WmDestroy:
                User32.PostQuitMessage(0);
                return IntPtr.Zero;

            case Consts.WmSize:
                _targetWidth = (int)((uint)lParam & 0xFFFF);
                _targetHeight = (int)(((uint)lParam >> 16) & 0xFFFF);
                _needsResize = true;
                return IntPtr.Zero;
        }
        return User32.DefWindowProc(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        _shouldClose = true;
        if (_hwnd != IntPtr.Zero)
            User32.SendMessage(_hwnd, Consts.WmClose, IntPtr.Zero, IntPtr.Zero);
        DiscardDeviceResources();
        if (_factory != null)
        {
            _factory->Release();
            _factory = null;
        }
        GC.SuppressFinalize(this);
    }
}