using System;

namespace CpuPowerGadget.Utilities
{
    public class SimpleAverage
    {
        private readonly int _size;
        private readonly float[] _values;
        private int _valuesIndex;
        private int _valueCount;
        private float _sum;

        public SimpleAverage(int size)
        {
            _size = Math.Max(size, 1);
            _values = new float[_size];
        }

        public float Add(float newValue)
        {
            var temp = newValue - _values[_valuesIndex];
            _values[_valuesIndex] = newValue;
            _sum += temp;

            _valuesIndex++;
            _valuesIndex %= _size;
        
            if (_valueCount < _size) 
                _valueCount++;

            return _sum / _valueCount;
        }

        public void Reset()
        {
            Array.Clear(_values, 0, _values.Length);
            _valuesIndex = 0;
            _valueCount = 0;
            _sum = 0;
        }
    } 
}
