using UnityEngine;

namespace Jeomseon.UnityReactive
{
    [System.Serializable]
    public class MaxIntProcessor : IValueProcessor
    {
        [field: SerializeField] public int Max { get; private set; }
        public T Process<T>(T value)
        {
            return value is int intValue && Mathf.Min(intValue, Max) is T newValue ? newValue : value;
        }

        public MaxIntProcessor(int max) => Max = max;
    }
}
