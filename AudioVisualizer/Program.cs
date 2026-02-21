using System.Runtime.InteropServices;

using AudioVisualizer.Application.Audio;
using AudioVisualizer.Application.Core;
using AudioVisualizer.Application.Rendering;
using AudioVisualizer.Infrastructure.Audio.NativeWasapi;
using AudioVisualizer.Infrastructure.Rendering.Gdi;

if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    throw new NotSupportedException();

using IAudioSource audioSource = new NativeWasapiAudioSource();
using IVisualizerRenderer renderer = new GdiVisualizerRenderer();

var app = new VisualizerApp(audioSource, renderer);
app.Run();