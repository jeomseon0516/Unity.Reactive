namespace Jeomseon.Unity.Reactive.ReactiveList
{
    public delegate void ElementChangedHandler<in T>(int index, T previous, T current);
}
