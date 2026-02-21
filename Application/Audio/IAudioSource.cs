namespace AudioVisualizer.Application.Audio;

public interface IAudioSource : IDisposable
{
    float CurrentRms { get; }
    void Start();
    void Stop();
}