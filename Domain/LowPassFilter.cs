namespace AudioVisualizer.Domain;

public class LowPassFilter
{
    private float _a1, _a2, _b0, _b1, _b2;
    private float _x1, _x2, _y1, _y2;

    public void SetParameters(float sampleRate, float cutoffFreq)
    {
        float w0 = 2f * MathF.PI * cutoffFreq / sampleRate;
        float cosW0 = MathF.Cos(w0);
        float alpha = MathF.Sin(w0) / (2f * 0.7071f);

        float a0 = 1f + alpha;
        _b0 = (1f - cosW0) / 2f / a0;
        _b1 = (1f - cosW0) / a0;
        _b2 = (1f - cosW0) / 2f / a0;
        _a1 = -2f * cosW0 / a0;
        _a2 = (1f - alpha) / a0;
    }

    public float Process(float input)
    {
        float output = _b0 * input + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;

        _x2 = _x1;
        _x1 = input;
        _y2 = _y1;
        _y1 = output;

        return output;
    }
}