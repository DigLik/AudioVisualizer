using AudioVisualizer.Application.Audio;
using AudioVisualizer.Application.Configuration;
using AudioVisualizer.Application.Rendering;
using AudioVisualizer.Domain;

namespace AudioVisualizer.Application.Core;

public class VisualizerApp(IAudioSource audioSource, IVisualizerRenderer renderer) : IDisposable
{
    private readonly UnsafeCircularBuffer<float> _rmsHistory = new(16);
    private readonly UnsafeCircularBuffer<float> _lowFreqRmsHistory = new(16);

    private readonly BeatDetector _beatDetector = new();
    private readonly AutoGainControl _agc = new();

    public void Run()
    {
        audioSource.Start();

        var config = new VisualizerConfig
        {
            IsWindowTransparent = true,
            EnableAcrylic = true,
            AcrylicBlurAmount = 0.2f
        };

        renderer.Initialize(800, 800, "", config);

        while (!renderer.ShouldClose)
            ProcessFrame();

        audioSource.Stop();
    }

    private void ProcessFrame()
    {
        float deltaTime = renderer.DeltaTime;
        float targetRms = audioSource.CurrentRms;
        float lowFreqRms = audioSource.CurrentLowFreqRms;

        _rmsHistory.Push(targetRms);
        _lowFreqRmsHistory.Push(lowFreqRms);

        float flashBrightness = _beatDetector.Process(_lowFreqRmsHistory, deltaTime);
        float radius = _agc.Process(targetRms, _rmsHistory.Rms, deltaTime);

        renderer.BeginFrame();
        renderer.FillBackground(1f, 1f, 1f, flashBrightness);
        renderer.DrawCenteredCircle(radius);
        renderer.EndFrame();
    }

    public void Dispose()
    {
        _rmsHistory.Dispose();
        _lowFreqRmsHistory.Dispose();
        GC.SuppressFinalize(this);
    }
}