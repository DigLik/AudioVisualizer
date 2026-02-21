using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using AudioVisualizer.Application.Configuration;
using AudioVisualizer.Application.Rendering;
using AudioVisualizer.Infrastructure.Windows;
using AudioVisualizer.Infrastructure.Windows.Interop;

namespace AudioVisualizer.Infrastructure.Rendering.Gdi;

[SupportedOSPlatform("windows")]
public unsafe partial class GdiVisualizerRenderer : IVisualizerRenderer
{
    private IntPtr _hwnd;
    private IntPtr _memDc;
    private IntPtr _hBitmap;
    private IntPtr _oldBitmap;
    private uint* _pixelPtr;

    private int _width;
    private int _height;
    private volatile bool _shouldClose;
    private bool _isDarkMode;

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
            Name = "GdiWindowThread",
            IsBackground = true
        };
        _windowThread.SetApartmentState(ApartmentState.STA);
        _windowThread.Start();

        _windowReady.Wait();

        _stopwatch.Start();
        _lastFrameTime = _stopwatch.ElapsedTicks;
    }

    private void WindowThreadProc(string title)
    {
        var hInstance = Kernel32.GetModuleHandle(IntPtr.Zero);
        fixed (char* pClassName = "GdiVisualizerClass")
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
                0, "GdiVisualizerClass", title, Consts.WsOverlappedWindow,
                Consts.CwUseDefault, Consts.CwUseDefault,
                rect.right - rect.left, rect.bottom - rect.top,
                IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);
        }

        ApplyDwmEffects();
        ApplyTheme();

        IntPtr hdc = User32.GetDC(_hwnd);
        _memDc = Gdi32.CreateCompatibleDC(hdc);
        _ = User32.ReleaseDC(_hwnd, hdc);

        _needsResize = true;
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

        if (_config.EnableAcrylic)
        {
            if (Environment.OSVersion.Version.Build >= 22000)
            {
                int backdropType = Consts.DwmSbtTransientWindow;
                DwmApi.DwmSetWindowAttribute(_hwnd, Consts.DwmWaSystemBackdropType, ref backdropType, sizeof(int));
            }
        }
    }

    private void ApplyTheme()
    {
        _isDarkMode = ThemeHelper.IsSystemInDarkMode();
        ThemeHelper.ApplyThemeToWindow(_hwnd, _isDarkMode);
    }

    public void BeginFrame()
    {
        if (_needsResize)
        {
            _width = _targetWidth;
            _height = _targetHeight;
            CreateDIB();
            _needsResize = false;
        }

        long currentFrameTime = _stopwatch.ElapsedTicks;
        DeltaTime = (float)(currentFrameTime - _lastFrameTime) / Stopwatch.Frequency;
        _lastFrameTime = currentFrameTime;

        if (_pixelPtr != null && _width > 0 && _height > 0)
        {
            uint clearColor = _config.IsWindowTransparent ?
                0x00000000 : (_isDarkMode ? 0xFF000000 : 0xFFFFFFFF);
            new Span<uint>(_pixelPtr, _width * _height).Fill(clearColor);
        }
    }

    public void EndFrame()
    {
        if (_hBitmap != IntPtr.Zero && _width > 0 && _height > 0)
        {
            IntPtr hdc = User32.GetDC(_hwnd);
            Gdi32.BitBlt(hdc, 0, 0, _width, _height, _memDc, 0, 0, Consts.SrcCopy);
            _ = User32.ReleaseDC(_hwnd, hdc);
        }
    }

    public void DrawCenteredCircle(float radius)
    {
        float cx = _width / 2f;
        float cy = _height / 2f;

        int xMin = Math.Max(0, (int)(cx - radius - 1));
        int xMax = Math.Min(_width - 1, (int)(cx + radius + 1));
        int yMin = Math.Max(0, (int)(cy - radius - 1));
        int yMax = Math.Min(_height - 1, (int)(cy + radius + 1));

        byte color = _isDarkMode ? (byte)255 : (byte)0;

        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = MathF.Sqrt(dx * dx + dy * dy);

                if (dist <= radius)
                {
                    BlendPixel(x, y, color, color, color, 255);
                }
                else if (dist < radius + 1f)
                {
                    float alpha = 1f - (dist - radius);
                    BlendPixel(x, y, color, color, color, (byte)(alpha * 255));
                }
            }
        }
    }

    public void DrawCenteredSquare(float size, byte r, byte g, byte b, byte a)
    {
        float cx = _width / 2f;
        float cy = _height / 2f;
        float half = size / 2f;

        int xMin = Math.Max(0, (int)(cx - half));
        int xMax = Math.Min(_width - 1, (int)(cx + half));
        int yMin = Math.Max(0, (int)(cy - half));
        int yMax = Math.Min(_height - 1, (int)(cy + half));

        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                BlendPixel(x, y, r, g, b, a);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BlendPixel(int x, int y, byte r, byte g, byte b, byte a)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height) return;

        uint* pixel = _pixelPtr + (y * _width + x);
        uint bg = *pixel;

        byte bgA = (byte)((bg >> 24) & 0xFF);
        byte bgR = (byte)((bg >> 16) & 0xFF);
        byte bgG = (byte)((bg >> 8) & 0xFF);
        byte bgB = (byte)(bg & 0xFF);

        float alpha = a / 255f;
        float invAlpha = 1f - alpha;

        byte finalR = (byte)(r * alpha + bgR * invAlpha);
        byte finalG = (byte)(g * alpha + bgG * invAlpha);
        byte finalB = (byte)(b * alpha + bgB * invAlpha);
        byte finalA = (byte)(a + bgA * invAlpha);

        *pixel = (uint)((finalA << 24) | (finalR << 16) | (finalG << 8) | finalB);
    }

    private void CreateDIB()
    {
        if (_width <= 0 || _height <= 0) return;

        if (_hBitmap != IntPtr.Zero)
        {
            Gdi32.SelectObject(_memDc, _oldBitmap);
            Gdi32.DeleteObject(_hBitmap);
        }

        var bmi = new BITMAPINFO
        {
            bmiHeader = new BITMAPINFOHEADER
            {
                biSize = (uint)sizeof(BITMAPINFOHEADER),
                biWidth = _width,
                biHeight = -_height,
                biPlanes = 1,
                biBitCount = 32,
                biCompression = Consts.BiRgb
            }
        };

        _hBitmap = Gdi32.CreateDIBSection(_memDc, in bmi, Consts.DibRgbColors, out IntPtr ppvBits, IntPtr.Zero, 0);
        _pixelPtr = (uint*)ppvBits;
        _oldBitmap = Gdi32.SelectObject(_memDc, _hBitmap);

        _needsResize = false;
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case Consts.WmEraseBkgnd:
                return 1;

            case Consts.WmSettingChange:
                if (lParam != IntPtr.Zero)
                {
                    string? param = Marshal.PtrToStringUni(lParam);
                    if (param == "ImmersiveColorSet")
                        ApplyTheme();
                }
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
        {
            User32.SendMessage(_hwnd, Consts.WmClose, IntPtr.Zero, IntPtr.Zero);
            _hwnd = IntPtr.Zero;
        }

        if (_hBitmap != IntPtr.Zero)
        {
            Gdi32.SelectObject(_memDc, _oldBitmap);
            Gdi32.DeleteObject(_hBitmap);
            _hBitmap = IntPtr.Zero;
        }

        if (_memDc != IntPtr.Zero)
        {
            Gdi32.DeleteDC(_memDc);
            _memDc = IntPtr.Zero;
        }

        GC.SuppressFinalize(this);
    }
}