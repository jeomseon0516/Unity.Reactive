using System;
using Jeomseon.Unity.Reactive.ReactiveField;
using R3;

namespace Jeomseon.Unity.Reactive.R3
{
    public static class ReactiveFieldR3Extensions
    {
        public static Observable<T> ObserveValue<T>(
            this IReadOnlyReactiveField<T> source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return Observable.Create<T>(observer =>
            {
                source.ChangedEvent += OnChanged;
                return Disposable.Create(() => source.ChangedEvent -= OnChanged);

                void OnChanged(T value) => observer.OnNext(value);
            });
        }
    }
}
