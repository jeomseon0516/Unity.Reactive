using UnityEngine;

namespace Jeomseon.UnityReactive
{
    [System.Serializable]
    public class MinIntProcessor : IValueProcessor
    {
        [field: SerializeField] public int Min { get; private set; }

        public MinIntProcessor() { }

        public MinIntProcessor(int min) => Min = min;

        public T Process<T>(T value)
        {
            return value is int intValue && Mathf.Max(intValue, Min) is T newValue ? newValue : value;
        }
    }
}
