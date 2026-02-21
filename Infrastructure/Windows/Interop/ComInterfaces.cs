using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace AudioVisualizer.Infrastructure.Windows.Interop;

internal static partial class ComInterfaces
{
    [GeneratedComInterface]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IMMDeviceEnumerator
    {
        public void EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr ppDevices);
        public void GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppEndpoint);
    }

    [GeneratedComInterface]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IMMDevice
    {
        public void Activate(in Guid iid, uint dwClsCtx, IntPtr pActivationParams, out IAudioClient ppInterface);
    }

    [GeneratedComInterface]
    [Guid("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IAudioClient
    {
        public void Initialize(int shareMode, uint streamFlags, long hnsBufferDuration, long hnsPeriodicity, IntPtr pFormat, in Guid audioSessionGuid);
        public void GetBufferSize(out uint pNumBufferFrames);
        public void GetStreamLatency(out long phnsLatency);
        public void GetCurrentPadding(out uint pNumPaddingFrames);
        public void IsFormatSupported(int shareMode, IntPtr pFormat, out IntPtr ppClosestMatch);
        public void GetMixFormat(out IntPtr ppDeviceFormat);
        public void GetDevicePeriod(out long phnsDefaultDevicePeriod, out long phnsMinimumDevicePeriod);
        public void Start();
        public void Stop();
        public void Reset();
        public void SetEventHandle(IntPtr eventHandle);
        public void GetService(in Guid riid, out IAudioCaptureClient ppv);
    }

    [GeneratedComInterface]
    [Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IAudioCaptureClient
    {
        public void GetBuffer(out IntPtr ppData, out uint pNumFramesToRead, out uint pdwFlags, out ulong pu64DevicePosition, out ulong pu64QPCPosition);
        public void ReleaseBuffer(uint numFramesRead);
        public void GetNextPacketSize(out uint pNumFramesInNextPacket);
    }
}
