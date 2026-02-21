using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AudioVisualizer.Infrastructure.Windows.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_COLOR_F { public float r, g, b, a; }

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_POINT_2F { public float x, y; }

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_ELLIPSE { public D2D1_POINT_2F point; public float radiusX; public float radiusY; }

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_SIZE_U { public uint width, height; }

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_PIXEL_FORMAT { public uint format, alphaMode; }

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_RENDER_TARGET_PROPERTIES { public uint type; public D2D1_PIXEL_FORMAT pixelFormat; public float dpiX, dpiY; public uint usage, minLevel; }

[StructLayout(LayoutKind.Sequential)]
internal struct D2D1_HWND_RENDER_TARGET_PROPERTIES { public IntPtr hwnd; public D2D1_SIZE_U pixelSize; public uint presentOptions; }

internal unsafe struct ID2D1Factory
{
    public void** lpVtbl;
    public uint Release() => ((delegate* unmanaged[Stdcall]<ID2D1Factory*, uint>)lpVtbl[2])((ID2D1Factory*)Unsafe.AsPointer(ref this));
    public int CreateHwndRenderTarget(D2D1_RENDER_TARGET_PROPERTIES* renderTargetProperties, D2D1_HWND_RENDER_TARGET_PROPERTIES* hwndRenderTargetProperties, ID2D1HwndRenderTarget** hwndRenderTarget)
        => ((delegate* unmanaged[Stdcall]<ID2D1Factory*, D2D1_RENDER_TARGET_PROPERTIES*, D2D1_HWND_RENDER_TARGET_PROPERTIES*, ID2D1HwndRenderTarget**, int>)lpVtbl[14])((ID2D1Factory*)Unsafe.AsPointer(ref this), renderTargetProperties, hwndRenderTargetProperties, hwndRenderTarget);
}

internal unsafe struct ID2D1HwndRenderTarget
{
    public void** lpVtbl;
    public uint Release() => ((delegate* unmanaged[Stdcall]<ID2D1HwndRenderTarget*, uint>)lpVtbl[2])((ID2D1HwndRenderTarget*)Unsafe.AsPointer(ref this));
    public int CreateSolidColorBrush(D2D1_COLOR_F* color, IntPtr brushProperties, ID2D1SolidColorBrush** solidColorBrush)
        => ((delegate* unmanaged[Stdcall]<ID2D1HwndRenderTarget*, D2D1_COLOR_F*, IntPtr, ID2D1SolidColorBrush**, int>)lpVtbl[8])((ID2D1HwndRenderTarget*)Unsafe.AsPointer(ref this), color, brushProperties, solidColorBrush);
    public void FillEllipse(D2D1_ELLIPSE* ellipse, ID2D1SolidColorBrush* brush)
        => ((delegate* unmanaged[Stdcall]<ID2D1HwndRenderTarget*, D2D1_ELLIPSE*, ID2D1SolidColorBrush*, void>)lpVtbl[21])((ID2D1HwndRenderTarget*)Unsafe.AsPointer(ref this), ellipse, brush);
    public void Clear(D2D1_COLOR_F* clearColor)
        => ((delegate* unmanaged[Stdcall]<ID2D1HwndRenderTarget*, D2D1_COLOR_F*, void>)lpVtbl[47])((ID2D1HwndRenderTarget*)Unsafe.AsPointer(ref this), clearColor);
    public void BeginDraw()
        => ((delegate* unmanaged[Stdcall]<ID2D1HwndRenderTarget*, void>)lpVtbl[48])((ID2D1HwndRenderTarget*)Unsafe.AsPointer(ref this));
    public int EndDraw(ulong* tag1, ulong* tag2)
        => ((delegate* unmanaged[Stdcall]<ID2D1HwndRenderTarget*, ulong*, ulong*, int>)lpVtbl[49])((ID2D1HwndRenderTarget*)Unsafe.AsPointer(ref this), tag1, tag2);
    public int Resize(D2D1_SIZE_U* pixelSize)
        => ((delegate* unmanaged[Stdcall]<ID2D1HwndRenderTarget*, D2D1_SIZE_U*, int>)lpVtbl[58])((ID2D1HwndRenderTarget*)Unsafe.AsPointer(ref this), pixelSize);
}

internal unsafe struct ID2D1SolidColorBrush
{
    public void** lpVtbl;
    public uint Release() => ((delegate* unmanaged[Stdcall]<ID2D1SolidColorBrush*, uint>)lpVtbl[2])((ID2D1SolidColorBrush*)Unsafe.AsPointer(ref this));
    public void SetColor(D2D1_COLOR_F* color)
        => ((delegate* unmanaged[Stdcall]<ID2D1SolidColorBrush*, D2D1_COLOR_F*, void>)lpVtbl[8])((ID2D1SolidColorBrush*)Unsafe.AsPointer(ref this), color);
}

internal static partial class D2D1
{
    [LibraryImport("d2d1.dll")]
    public static unsafe partial int D2D1CreateFactory(uint factoryType, in Guid riid, IntPtr pFactoryOptions, ID2D1Factory** ppIFactory);
}