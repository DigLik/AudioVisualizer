using System.Numerics;
using System.Runtime.CompilerServices;

namespace AudioVisualizer.Domain;

public static class MathUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ExponentialMovingAverage<T>(T current, T target, T factor)
        where T : INumber<T> => factor * target + (T.One - factor) * current;
}