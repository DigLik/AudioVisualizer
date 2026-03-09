namespace AudioVisualizer.Domain;

public class AutoGainControl(float baseRadius = 200f, float attackSpeed = 15f, float releaseSpeed = 0.5f)
{
    private float _peakEnvelope = 0.001f;

    public float Process(float currentRms, float averageRms, float deltaTime)
    {
        _peakEnvelope = currentRms > _peakEnvelope
            ? MathUtils.ExponentialMovingAverage(_peakEnvelope, currentRms, Math.Min(1f, deltaTime * attackSpeed))
            : _peakEnvelope = MathUtils.ExponentialMovingAverage(_peakEnvelope, 0.001f, Math.Min(1f, deltaTime * releaseSpeed));

        float effectivePeak = Math.Max(_peakEnvelope, 0.005f);
        float normalizedVolume = averageRms / effectivePeak;

        return Math.Min(normalizedVolume * baseRadius, baseRadius * 3f);
    }
}