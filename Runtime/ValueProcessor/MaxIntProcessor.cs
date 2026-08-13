using UnityEngine;

namespace Jeomseon.Unity.Reactive.ValueProcessor
{
    [System.Serializable]
    public class MaxIntProcessor : IValueProcessor
    {
        [field: SerializeField] public int Max { get; private set; }

        public MaxIntProcessor() { }

        public MaxIntProcessor(int max) => Max = max;

        public T Process<T>(T value)
        {
            return value is int intValue && Mathf.Min(intValue, Max) is T newValue ? newValue : value;
        }
    }
}
