namespace AudioVisualizer.Domain;

public class BeatDetector
{
    private float _flashBrightness = 0f;

    private const float AttackSpeed = 50f;
    private const float ReleaseSpeed = 3f;
    private const float BeatThreshold = 1.5f;

    public float Process(UnsafeCircularBuffer<float> history, float deltaTime)
    {
        if (history.Count == 0) return 0f;

        float averageRms = history.Rms;
        float currentRms = history[^1];

        bool isBeat = currentRms > 0.005f && currentRms > averageRms * BeatThreshold;

        if (isBeat)
        {
            float attackFactor = Math.Min(1f, deltaTime * AttackSpeed);
            _flashBrightness = MathUtils.ExponentialMovingAverage(_flashBrightness, 1f, attackFactor);
        }
        else
        {
            float releaseFactor = Math.Min(1f, deltaTime * ReleaseSpeed);
            _flashBrightness = MathUtils.ExponentialMovingAverage(_flashBrightness, 0f, releaseFactor);
        }

        return Math.Clamp(_flashBrightness, 0f, 1f);
    }
}