namespace Jeomseon.UnityReactive
{
    public interface IValueProcessor
    {
        public T Process<T>(T value);
    }
}
