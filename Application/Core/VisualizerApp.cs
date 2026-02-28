using AudioVisualizer.Application.Audio;
using AudioVisualizer.Application.Configuration;
using AudioVisualizer.Application.Rendering;
using AudioVisualizer.Domain;

namespace AudioVisualizer.Application.Core;

public class VisualizerApp(IAudioSource audioSource, IVisualizerRenderer renderer)
{
    private const float Sensitivity = 5000f;
    private const float SmoothingMultiplier = 20f;
    private float _smoothedRms = 0f;

    public void Run()
    {
        audioSource.Start();

        var config = new VisualizerConfig
        {
            IsWindowTransparent = true,
            EnableAcrylic = true,
            AcrylicBlurAmount = 0.2f
        };

        renderer.Initialize(800, 600, "Clean Architecture Visualizer", config);

        while (!renderer.ShouldClose)
            ProcessFrame();

        audioSource.Stop();
    }

    private void ProcessFrame()
    {
        float deltaTime = renderer.DeltaTime;
        float targetRms = audioSource.CurrentRms;

        float smoothFactor = deltaTime * SmoothingMultiplier;
        if (smoothFactor > 1f) smoothFactor = 1f;

        _smoothedRms = MathUtils.ExponentialMovingAverage(_smoothedRms, targetRms, smoothFactor);
        float radius = _smoothedRms * Sensitivity;

        renderer.BeginFrame();
        renderer.DrawCenteredCircle(radius);
        renderer.EndFrame();
    }
}