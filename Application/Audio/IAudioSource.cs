namespace AudioVisualizer.Application.Audio;

public interface IAudioSource : IDisposable
{
    float CurrentRms { get; }
    float CurrentLowFreqRms { get; }
    void Start();
    void Stop();
}