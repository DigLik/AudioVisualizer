using System.Numerics;
using System.Runtime.InteropServices;

namespace AudioVisualizer.Domain;

public unsafe class UnsafeCircularBuffer<T> : IDisposable
    where T : unmanaged, INumber<T>
{
    private T* _buffer;
    private int _head;
    private T _sum;
    private T _sumSquares;

    public UnsafeCircularBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        Capacity = capacity;
        _buffer = (T*)NativeMemory.Alloc((nuint)capacity, (nuint)sizeof(T));

        _sum = T.Zero;
        _sumSquares = T.Zero;
    }

    public void Push(T item)
    {
        T oldItem = T.Zero;

        if (Count == Capacity)
            oldItem = _buffer[_head];
        else
            Count++;

        _sum = _sum - oldItem + item;
        _sumSquares = _sumSquares - (oldItem * oldItem) + (item * item);

        _buffer[_head] = item;
        _head = (_head + 1) % Capacity;
    }

    public int Count { get; private set; }
    public int Capacity { get; }

    public T ArithmeticMean
    {
        get
        {
            if (Count == 0) return T.Zero;
            return _sum / T.CreateChecked(Count);
        }
    }

    public T Rms
    {
        get
        {
            if (Count == 0) return T.Zero;
            T meanSquare = _sumSquares / T.CreateChecked(Count);
            double msDouble = double.CreateChecked(meanSquare);
            if (msDouble < 0) msDouble = 0;
            return T.CreateChecked(Math.Sqrt(msDouble));
        }
    }

    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= Count) throw new IndexOutOfRangeException();
            int actualIndex = Count < Capacity
                ? index
                : (_head + index) % Capacity;
            return _buffer[actualIndex];
        }
    }

    public void Dispose()
    {
        if (_buffer != null)
        {
            NativeMemory.Free(_buffer);
            _buffer = null;
        }
        GC.SuppressFinalize(this);
    }

    ~UnsafeCircularBuffer() => Dispose();
}