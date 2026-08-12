using UnityEngine;

namespace Jeomseon.UnityReactive
{
    [System.Serializable]
    public class ClampIntProcessor : IValueProcessor
    {
        [field: SerializeField] public int Min { get; private set; }
        [field: SerializeField] public int Max { get; private set; }

        public ClampIntProcessor() { }

        public ClampIntProcessor(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public T Process<T>(T value)
        {
            return value is int intValue && Mathf.Clamp(intValue, Min, Max) is T newValue ? newValue : value;
        }
    }
}
