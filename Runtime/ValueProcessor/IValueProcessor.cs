namespace Jeomseon.Unity.Reactive.ValueProcessor
{
    public interface IValueProcessor
    {
        public T Process<T>(T value);
    }
}
