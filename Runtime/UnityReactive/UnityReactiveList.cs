using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;
using Jeomseon.Reactive; // 공통 인터페이스/델리게이트 참조
using UnityEngine.Serialization;

namespace Jeomseon.UnityReactive
{

    [Serializable]
    public class UnityReactiveList<T> : IList<T>, IReadOnlyReactiveList<T>
    {
        [SerializeField, FormerlySerializedAs("_list")] private List<T> list = new();

        [SerializeField, FormerlySerializedAs("_addedEvent")] private UnityEvent<int[], T[]> addedEvent = new();
        [SerializeField, FormerlySerializedAs("_removedEvent")] private UnityEvent<int[], T[]> removedEvent = new();
        [SerializeField, FormerlySerializedAs("_changedEvent")] private UnityEvent<int, T, T> changedEvent = new();
        [SerializeField, FormerlySerializedAs("_reorderedEvent")] private UnityEvent<IReadOnlyList<T>> reorderedEvent = new();

        public event AddOrRemoveHandler<T> AddedEvent
        {
            add
            {
                if (value == null) return;

                addedEvent.AddListener((UnityAction<int[], T[]>)Delegate.CreateDelegate(typeof(UnityAction<int[], T[]>), value.Target, value.Method));

                int[] indices = new int[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    indices[i] = i;
                }

                value.Invoke(indices, list.ToArray());
            }
            remove => removeListenerSafe(addedEvent, value);
        }

        public event AddOrRemoveHandler<T> RemovedEvent
        {
            add => addListenerSafe(removedEvent, value);
            remove => removeListenerSafe(removedEvent, value);
        }

        public event ElementChangedHandler<T> ChangedEvent
        {
            add => addListenerSafe(changedEvent, value);
            remove => removeListenerSafe(changedEvent, value);
        }

        public event Action<IReadOnlyList<T>> ReorderedEvent
        {
            add { if (value == null) return; reorderedEvent.AddListener((UnityAction<IReadOnlyList<T>>)Delegate.CreateDelegate(typeof(UnityAction<IReadOnlyList<T>>), value.Target, value.Method)); }
            remove { if (value == null) return; reorderedEvent.RemoveListener((UnityAction<IReadOnlyList<T>>)Delegate.CreateDelegate(typeof(UnityAction<IReadOnlyList<T>>), value.Target, value.Method)); }
        }

        public int Count => list.Count;
        public int Capacity { get => list.Capacity; set => list.Capacity = value; }
        public bool IsReadOnly => false;

        public T this[int index]
        {
            get => list[index];
            set
            {
                if (index < 0 || index >= list.Count) return;

                T prev = list[index];
                list[index] = value;
                changedEvent.Invoke(index, prev, value);
            }
        }

        public void AddListenerToAddedEventWithoutNotify(AddOrRemoveHandler<T> onAddAction) => addListenerSafe(addedEvent, onAddAction);

        // -------------------- Add / Insert --------------------
        public void Add(T item) => insertInternal(list.Count, item);
        public void Insert(int index, T item) => insertInternal(index, item);

        private void insertInternal(int index, T item)
        {
            if (index < 0 || index > list.Count) return;

            list.Insert(index, item);
            addedEvent.Invoke(new int[] { index }, new T[] { item });
        }

        public void AddRange(IEnumerable<T> collection) => InsertRange(list.Count, collection);
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (collection == null || index < 0 || index > list.Count) return;

            int diff = getCollectionCount(collection);
            if (diff <= 0) return;

            list.InsertRange(index, collection);
            getArrayFromCollection(index, diff, collection, out int[] indices, out T[] arr);
            addedEvent.Invoke(indices, arr);
        }

        // -------------------- Remove --------------------
        public bool Remove(T item)
        {
            int index = list.IndexOf(item);
            if (index < 0) return false;
            list.RemoveAt(index);
            removedEvent.Invoke(new[] { index }, new T[] { item });
            return true;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= list.Count) return;
            T item = list[index];
            list.RemoveAt(index);
            removedEvent.Invoke(new[] { index }, new T[] { item });
        }

        public void RemoveRange(int index, int count)
        {
            if (index < 0 || count < 0 || index + count > list.Count) return;

            T[] items = list.GetRange(index, count).ToArray();
            int[] indices = new int[items.Length];

            for (int i = 0; i < count; i++)
            {
                indices[i] = index + i;
            }

            list.RemoveRange(index, count);
            removedEvent.Invoke(indices, items);
        }

        public int RemoveAll(Predicate<T> match)
        {
            List<(int, T)> values = new();
            for (int i = 0; i < list.Count; i++)
            {
                if (!match.Invoke(list[i])) continue;

                values.Add((i, list[i]));
            }

            int count = 0;
            if (values.Count > 0)
            {
                int[] indices = new int[values.Count];
                T[] items = new T[values.Count];
                for (int i = 0; i < values.Count; i++)
                {
                    indices[i] = values[i].Item1;
                    items[i] = values[i].Item2;
                }

                count = list.RemoveAll(match);
                removedEvent.Invoke(indices, items);
            }

            return count;
        }

        public void Clear()
        {
            if (list.Count == 0) return;
            T[] items = list.ToArray();
            int[] indices = new int[list.Count];

            for (int i = 0; i < items.Length; i++)
            {
                indices[i] = i;
            }

            list.Clear();
            removedEvent.Invoke(indices, items);
        }

        // -------------------- Helpers --------------------
        private static void addListenerSafe(UnityEvent<int[], T[]> unityEvent, AddOrRemoveHandler<T> callback)
        {
            if (callback == null) return;
            unityEvent.AddListener((UnityAction<int[], T[]>)Delegate.CreateDelegate(typeof(UnityAction<int[], T[]>), callback.Target, callback.Method));
        }

        private static void removeListenerSafe(UnityEvent<int[], T[]> unityEvent, AddOrRemoveHandler<T> callback)
        {
            if (callback == null) return;
            unityEvent.RemoveListener((UnityAction<int[], T[]>)Delegate.CreateDelegate(typeof(UnityAction<int[], T[]>), callback.Target, callback.Method));
        }

        private static void addListenerSafe(UnityEvent<int, T, T> unityEvent, ElementChangedHandler<T> callback)
        {
            if (callback == null) return;
            unityEvent.AddListener((UnityAction<int, T, T>)Delegate.CreateDelegate(typeof(UnityAction<int, T, T>), callback.Target, callback.Method));
        }

        private static void removeListenerSafe(UnityEvent<int, T, T> unityEvent, ElementChangedHandler<T> callback)
        {
            if (callback == null) return;
            unityEvent.RemoveListener((UnityAction<int, T, T>)Delegate.CreateDelegate(typeof(UnityAction<int, T, T>), callback.Target, callback.Method));
        }

        private static int getCollectionCount(IEnumerable<T> collection) =>
            collection is ICollection<T> col ? col.Count : collection.Count();

        private static void getArrayFromCollection(int start, int count, IEnumerable<T> collection, out int[] indices, out T[] arr)
        {
            indices = new int[count];
            arr = new T[count];

            if (collection is IList<T> list)
            {
                for (int i = 0; i < count; i++)
                {
                    indices[i] = start + i;
                    arr[i] = list[i];
                }
            }
            else
            {
                using var enumerator = collection.GetEnumerator();
                for (int i = 0; i < count; i++)
                {
                    if (!enumerator.MoveNext()) break;
                    indices[i] = start + i;
                    arr[i] = enumerator.Current;
                }
            }
        }

        public void Reverse(int index, int count, IComparer<T> comparer)
        {
            list.Reverse(index, count);
            reorderedEvent.Invoke(list);
        }

        public void Reverse()
        {
            list.Reverse();
            reorderedEvent.Invoke(list);
        }

        public void Sort(Comparison<T> comparison)
        {
            list.Sort(comparison);
            reorderedEvent.Invoke(list);
        }

        public void Sort(int index, int count, IComparer<T> comparer)
        {
            list.Sort(index, count, comparer);
            reorderedEvent.Invoke(list);
        }

        public void Sort()
        {
            list.Sort();
            reorderedEvent.Invoke(list);
        }

        public void Sort(IComparer<T> comparer)
        {
            list.Sort(comparer);
            reorderedEvent.Invoke(list);
        }

        // -------------------- 기타 List<T> Wrappers --------------------
        public List<T> ToList() => list.ToList();
        public T[] ToArray() => list.ToArray();
        public ReadOnlyCollection<T> AsReadOnly() => list.AsReadOnly();
        public int BinarySearch(int index, int count, T item, IComparer<T> comparer) => list.BinarySearch(index, count, item, comparer);
        public int BinarySearch(T item) => list.BinarySearch(item);
        public int BinarySearch(T item, IComparer<T> comparer) => list.BinarySearch(item, comparer);
        public bool Contains(T item) => list.Contains(item);
        public List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter) => list.ConvertAll(converter);
        public void CopyTo(T[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);
        public void CopyTo(T[] array) => list.CopyTo(array);
        public void CopyTo(int index, T[] array, int arrayIndex, int count) => list.CopyTo(index, array, arrayIndex, count);
        public bool Exist(Predicate<T> match) => list.Exists(match);
        public T Find(Predicate<T> match) => list.Find(match);
        public List<T> FindAll(Predicate<T> match) => list.FindAll(match);
        public int FindIndex(int startIndex, int count, Predicate<T> match) => list.FindIndex(startIndex, count, match);
        public int FindIndex(int startIndex, Predicate<T> match) => list.FindIndex(startIndex, match);
        public int FindIndex(Predicate<T> match) => list.FindIndex(match);
        public void ForEach(Action<T> action) => list.ForEach(action);
        public List<T> GetRange(int index, int count) => list.GetRange(index, count);
        public int IndexOf(T item, int index, int count) => list.IndexOf(item, index, count);
        public int IndexOf(T item, int index) => list.IndexOf(item, index);
        public int IndexOf(T item) => list.IndexOf(item);
        public int LastIndexOf(T item) => list.LastIndexOf(item);
        public int LastIndexOf(T item, int index) => list.LastIndexOf(item, index);
        public int LastIndexOf(T item, int index, int count) => list.LastIndexOf(item, index, count);
        public void TrimExcess() => list.TrimExcess();
        public bool TrueForAll(Predicate<T> match) => list.TrueForAll(match);
        public List<T>.Enumerator GetEnumerator() => list.GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();

        // 생성자
        public UnityReactiveList() { }
        public UnityReactiveList(int capacity) => list.Capacity = capacity;
        public UnityReactiveList(IEnumerable<T> collection) => list = new List<T>(collection);
    }
}
