using AudioVisualizer.Application.Configuration;

namespace AudioVisualizer.Application.Rendering;

public interface IVisualizerRenderer : IDisposable
{
    bool ShouldClose { get; }
    float DeltaTime { get; }

    void Initialize(int width, int height, string title, VisualizerConfig config = default);
    void BeginFrame();
    void EndFrame();
    void DrawCenteredCircle(float radius);
    void FillBackground(float r, float g, float b, float a);
}