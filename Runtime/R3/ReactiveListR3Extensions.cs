using System;
using System.Collections.Generic;
using Jeomseon.Unity.Reactive.ReactiveList;
using R3;

namespace Jeomseon.Unity.Reactive.R3
{
    public static class ReactiveListR3Extensions
    {
        public static Observable<(int[] Indices, T[] Items)> ObserveAdded<T>(
            this IReadOnlyReactiveList<T> source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return Observable.Create<(int[] Indices, T[] Items)>(observer =>
            {
                source.AddedEvent += OnAdded;
                return Disposable.Create(() => source.AddedEvent -= OnAdded);

                void OnAdded(int[] indices, T[] items)
                    => observer.OnNext((indices, items));
            });
        }

        public static Observable<(int[] Indices, T[] Items)> ObserveRemoved<T>(
            this IReadOnlyReactiveList<T> source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return Observable.Create<(int[] Indices, T[] Items)>(observer =>
            {
                source.RemovedEvent += OnRemoved;
                return Disposable.Create(() => source.RemovedEvent -= OnRemoved);

                void OnRemoved(int[] indices, T[] items)
                    => observer.OnNext((indices, items));
            });
        }

        public static Observable<(int Index, T Previous, T Current)> ObserveChanged<T>(
            this IReadOnlyReactiveList<T> source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return Observable.Create<(int Index, T Previous, T Current)>(observer =>
            {
                source.ChangedEvent += OnChanged;
                return Disposable.Create(() => source.ChangedEvent -= OnChanged);

                void OnChanged(int index, T previous, T current)
                    => observer.OnNext((index, previous, current));
            });
        }

        public static Observable<IReadOnlyList<T>> ObserveReordered<T>(
            this IReadOnlyReactiveList<T> source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return Observable.Create<IReadOnlyList<T>>(observer =>
            {
                source.ReorderedEvent += OnReordered;
                return Disposable.Create(() => source.ReorderedEvent -= OnReordered);

                void OnReordered(IReadOnlyList<T> items)
                    => observer.OnNext(items);
            });
        }
    }
}
