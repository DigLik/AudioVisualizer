using System.Numerics;
using System.Runtime.InteropServices;

namespace AudioVisualizer.Domain;

public static class SignalProcessing
{
    public static float CalculateLowPassRms(ReadOnlySpan<byte> data, int bitsPerSample, int channels, LowPassFilter filter)
    {
        if (data.Length == 0 || channels == 0) return 0f;

        float sumSquares = 0f;
        int frames = 0;

        if (bitsPerSample == 32)
        {
            ReadOnlySpan<float> samples = MemoryMarshal.Cast<byte, float>(data);
            frames = samples.Length / channels;
            for (int i = 0; i < frames; i++)
            {
                float mono = 0f;
                for (int c = 0; c < channels; c++)
                    mono += samples[i * channels + c];
                mono /= channels;

                float filtered = filter.Process(mono);
                sumSquares += filtered * filtered;
            }
        }
        else if (bitsPerSample == 16)
        {
            ReadOnlySpan<short> samples = MemoryMarshal.Cast<byte, short>(data);
            frames = samples.Length / channels;
            for (int i = 0; i < frames; i++)
            {
                float mono = 0f;
                for (int c = 0; c < channels; c++)
                    mono += samples[i * channels + c];
                mono /= channels;

                float filtered = filter.Process(mono);
                sumSquares += filtered * filtered;
            }
        }
        else if (bitsPerSample == 24)
        {
            frames = data.Length / (3 * channels);
            for (int i = 0; i < frames; i++)
            {
                float mono = 0f;
                for (int c = 0; c < channels; c++)
                {
                    int offset = (i * channels + c) * 3;
                    mono += data[offset] | (data[offset + 1] << 8) | ((sbyte)data[offset + 2] << 16);
                }
                mono /= channels;
                float filtered = filter.Process(mono);
                sumSquares += filtered * filtered;
            }
        }
        else if (bitsPerSample == 8)
        {
            frames = data.Length / channels;
            for (int i = 0; i < frames; i++)
            {
                float mono = 0f;
                for (int c = 0; c < channels; c++)
                    mono += data[i * channels + c] - 128f;
                mono /= channels;
                float filtered = filter.Process(mono);
                sumSquares += filtered * filtered;
            }
        }

        return frames > 0 ? MathF.Sqrt(sumSquares / frames) : 0f;
    }

    public static float CalculateRms(ReadOnlySpan<byte> data, int bitsPerSample)
    {
        if (data.Length == 0) return 0f;

        float sumSquares = 0f;
        int sampleCount = 0;

        if (bitsPerSample == 32)
        {
            sumSquares = CalculateSumSquares32BitFloatSimd(data);
            sampleCount = data.Length / 4;
        }
        else if (bitsPerSample == 24)
        {
            sumSquares = CalculateSumSquares24BitScalar(data);
            sampleCount = data.Length / 3;
        }
        else if (bitsPerSample == 16)
        {
            sumSquares = CalculateSumSquares16BitSimd(data);
            sampleCount = data.Length / 2;
        }
        else if (bitsPerSample == 8)
        {
            sumSquares = CalculateSumSquares8BitScalar(data);
            sampleCount = data.Length;
        }

        return sampleCount > 0 ? MathF.Sqrt(sumSquares / sampleCount) : 0f;
    }

    private static float CalculateSumSquares32BitFloatSimd(ReadOnlySpan<byte> data)
    {
        ReadOnlySpan<float> samples = MemoryMarshal.Cast<byte, float>(data);
        int vectorSize = Vector<float>.Count;
        Vector<float> sumSqVec = Vector<float>.Zero;
        int i = 0;

        for (; i <= samples.Length - vectorSize; i += vectorSize)
        {
            var v = new Vector<float>(samples.Slice(i, vectorSize));
            sumSqVec += v * v;
        }

        float sumSquares = Vector.Dot(sumSqVec, Vector<float>.One);

        for (; i < samples.Length; i++)
        {
            float sample = samples[i];
            sumSquares += sample * sample;
        }
        return sumSquares;
    }

    private static float CalculateSumSquares24BitScalar(ReadOnlySpan<byte> data)
    {
        float sumSquares = 0f;
        for (int i = 0; i <= data.Length - 3; i += 3)
        {
            int sampleInt = (data[i] | (data[i + 1] << 8) | ((sbyte)data[i + 2] << 16));
            float sample = sampleInt;
            sumSquares += sample * sample;
        }
        return sumSquares;
    }

    private static float CalculateSumSquares16BitSimd(ReadOnlySpan<byte> data)
    {
        ReadOnlySpan<short> samples = MemoryMarshal.Cast<byte, short>(data);
        int vectorSize = Vector<short>.Count;
        int i = 0;

        Vector<float> sumSq1 = Vector<float>.Zero;
        Vector<float> sumSq2 = Vector<float>.Zero;

        for (; i <= samples.Length - vectorSize; i += vectorSize)
        {
            var vShort = new Vector<short>(samples.Slice(i, vectorSize));
            Vector.Widen(vShort, out Vector<int> vInt1, out Vector<int> vInt2);

            var vFloat1 = Vector.ConvertToSingle(vInt1);
            var vFloat2 = Vector.ConvertToSingle(vInt2);

            sumSq1 += vFloat1 * vFloat1;
            sumSq2 += vFloat2 * vFloat2;
        }

        float sumSquares = Vector.Dot(sumSq1, Vector<float>.One) + Vector.Dot(sumSq2, Vector<float>.One);

        for (; i < samples.Length; i++)
        {
            float sample = samples[i];
            sumSquares += sample * sample;
        }
        return sumSquares;
    }

    private static float CalculateSumSquares8BitScalar(ReadOnlySpan<byte> data)
    {
        float sumSquares = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float sample = data[i] - 128f;
            sumSquares += sample * sample;
        }
        return sumSquares;
    }
}