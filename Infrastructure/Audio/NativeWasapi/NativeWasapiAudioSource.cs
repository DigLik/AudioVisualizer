using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using AudioVisualizer.Application.Audio;
using AudioVisualizer.Domain;
using AudioVisualizer.Infrastructure.Windows.Interop;

namespace AudioVisualizer.Infrastructure.Audio.NativeWasapi;

[SupportedOSPlatform("windows")]
public partial class NativeWasapiAudioSource : IAudioSource
{
    private volatile float _currentRms;
    public float CurrentRms => _currentRms;

    private ComInterfaces.IMMDeviceEnumerator? _deviceEnumerator;
    private ComInterfaces.IMMDevice? _device;
    private ComInterfaces.IAudioClient? _audioClient;
    private ComInterfaces.IAudioCaptureClient? _captureClient;

    private IntPtr _hEvent = IntPtr.Zero;
    private Thread? _captureThread;
    private CancellationTokenSource? _cts;
    private bool _isDisposed;

    private int _bytesPerFrame;
    private int _bitsPerSample;

    public NativeWasapiAudioSource() => InitializeWasapi();

    private void InitializeWasapi()
    {
        Guid clsidDeviceEnum = new Guid(Consts.DeviceEnumeratorClsid);
        Guid iidDeviceEnum = typeof(ComInterfaces.IMMDeviceEnumerator).GUID;

        Ole32.CoCreateInstance(in clsidDeviceEnum, IntPtr.Zero, 1, in iidDeviceEnum, out _deviceEnumerator);
        _deviceEnumerator.GetDefaultAudioEndpoint(0, 0, out _device);

        Guid iidAudioClient = typeof(ComInterfaces.IAudioClient).GUID;
        _device.Activate(in iidAudioClient, 1, IntPtr.Zero, out _audioClient);

        _audioClient.GetMixFormat(out IntPtr waveFormatPtr);

        var format = Marshal.PtrToStructure<WAVEFORMATEX>(waveFormatPtr);
        _bytesPerFrame = format.nBlockAlign;
        _bitsPerSample = format.wBitsPerSample;

        const uint streamFlags = Consts.AudclntStreamflagsLoopback | Consts.AudclntStreamflagsEventcallback;
        _audioClient.Initialize(0, streamFlags, 0, 0, waveFormatPtr, Guid.Empty);

        _hEvent = Kernel32.CreateEventW(IntPtr.Zero, false, false, IntPtr.Zero);
        _audioClient.SetEventHandle(_hEvent);
        Guid iidCaptureClient = typeof(ComInterfaces.IAudioCaptureClient).GUID;
        _audioClient.GetService(in iidCaptureClient, out _captureClient);

        Marshal.FreeCoTaskMem(waveFormatPtr);
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        _cts = new CancellationTokenSource();
        _captureThread = new Thread(CaptureLoop)
        {
            IsBackground = true,
            Priority = ThreadPriority.Highest
        };

        _audioClient!.Start();
        _captureThread.Start();
    }

    public void Stop()
    {
        if (_isDisposed) return;

        _cts?.Cancel();
        _captureThread?.Join();
        _audioClient?.Stop();
    }

    private void CaptureLoop()
    {
        var token = _cts!.Token;

        while (!token.IsCancellationRequested)
        {
            uint waitResult = Kernel32.WaitForSingleObject(_hEvent, 100);
            if (waitResult != 0) continue;

            _captureClient!.GetNextPacketSize(out uint packetLength);

            while (packetLength != 0)
            {
                _captureClient.GetBuffer(out IntPtr dataPtr, out uint numFramesToRead, out uint flags, out _, out _);

                if ((flags & 0x2) != 0)
                {
                    _currentRms = 0f;
                }
                else
                {
                    unsafe
                    {
                        int byteCount = (int)numFramesToRead * _bytesPerFrame;
                        var byteSamples = new ReadOnlySpan<byte>(dataPtr.ToPointer(), byteCount);
                        _currentRms = SignalProcessing.CalculateRms(byteSamples, _bitsPerSample);
                    }
                }

                _captureClient.ReleaseBuffer(numFramesToRead);
                _captureClient.GetNextPacketSize(out packetLength);
            }
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        Stop();
        _isDisposed = true;

        if (_hEvent != IntPtr.Zero) Kernel32.CloseHandle(_hEvent);

        _captureClient = null;
        _audioClient = null;
        _device = null;
        _deviceEnumerator = null;

        GC.SuppressFinalize(this);
    }
}