using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using ObservableCollections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Jeomseon.UnityReactive
{
    // Inspector 편집 대상은 항상 list(순수 List<T>, Unity 기본 직렬화)입니다. 실제 변경 알림·Move
    // 지원은 ObservableCollections(Cysharp)의 ObservableList<T>가 담당하고, list는 Inspector
    // 표시/저장 시점(OnBeforeSerialize)에만 _runtime과 동기화됩니다. 이 분리 덕분에 R3 등 외부
    // 반응형 라이브러리 없이도 독립적으로 동작하며, Inspector에 보이는 필드·UI는 기존과 동일합니다.
    [Serializable]
    public class ReactiveList<T> : IList<T>, IReadOnlyReactiveList<T>, ISerializationCallbackReceiver
    {
        [SerializeField, FormerlySerializedAs("_list")] private List<T> list = new();

        [SerializeField, FormerlySerializedAs("_addedEvent")] private UnityEvent<int[], T[]> addedEvent = new();
        [SerializeField, FormerlySerializedAs("_removedEvent")] private UnityEvent<int[], T[]> removedEvent = new();
        [SerializeField, FormerlySerializedAs("_changedEvent")] private UnityEvent<int, T, T> changedEvent = new();
        [SerializeField, FormerlySerializedAs("_reorderedEvent")] private UnityEvent<IReadOnlyList<T>> reorderedEvent = new();

        [NonSerialized] private ObservableList<T> _runtime;

        public event AddOrRemoveHandler<T> AddedEvent
        {
            add
            {
                if (value == null) return;

                addedEvent.AddListener((UnityAction<int[], T[]>)Delegate.CreateDelegate(typeof(UnityAction<int[], T[]>), value.Target, value.Method));

                int[] indices = new int[_runtime.Count];
                for (int i = 0; i < _runtime.Count; i++)
                {
                    indices[i] = i;
                }

                value.Invoke(indices, _runtime.ToArray());
            }
            remove => RemoveListenerSafe(addedEvent, value);
        }

        public event AddOrRemoveHandler<T> RemovedEvent
        {
            add => AddListenerSafe(removedEvent, value);
            remove => RemoveListenerSafe(removedEvent, value);
        }

        public event ElementChangedHandler<T> ChangedEvent
        {
            add => AddListenerSafe(changedEvent, value);
            remove => RemoveListenerSafe(changedEvent, value);
        }

        public event Action<IReadOnlyList<T>> ReorderedEvent
        {
            add { if (value == null) return; reorderedEvent.AddListener((UnityAction<IReadOnlyList<T>>)Delegate.CreateDelegate(typeof(UnityAction<IReadOnlyList<T>>), value.Target, value.Method)); }
            remove { if (value == null) return; reorderedEvent.RemoveListener((UnityAction<IReadOnlyList<T>>)Delegate.CreateDelegate(typeof(UnityAction<IReadOnlyList<T>>), value.Target, value.Method)); }
        }

        public int Count => _runtime.Count;
        public bool IsReadOnly => false;

        public T this[int index]
        {
            get => _runtime[index];
            set
            {
                if ((uint)index >= (uint)_runtime.Count) return;
                _runtime[index] = value;
            }
        }

        public void AddListenerToAddedEventWithoutNotify(AddOrRemoveHandler<T> onAddAction) => AddListenerSafe(addedEvent, onAddAction);

        // -------------------- Add / Insert --------------------
        public void Add(T item) => _runtime.Add(item);
        public void Insert(int index, T item)
        {
            if ((uint)index > (uint)_runtime.Count) return;
            _runtime.Insert(index, item);
        }

        public void AddRange(IEnumerable<T> collection) => InsertRange(_runtime.Count, collection);
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (collection == null || (uint)index > (uint)_runtime.Count) return;
            T[] items = collection.ToArray();
            if (items.Length == 0) return;
            _runtime.InsertRange(index, items);
        }

        // -------------------- Remove --------------------
        public bool Remove(T item) => _runtime.Remove(item);

        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)_runtime.Count) return;
            _runtime.RemoveAt(index);
        }

        public void RemoveRange(int index, int count)
        {
            if (index < 0 || count < 0 || index + count > _runtime.Count) return;
            _runtime.RemoveRange(index, count);
        }

        // ObservableList<T>에는 RemoveAll이 없고, 개별 RemoveAt마다 이벤트가 따로 발행되면 기존
        // "한 번에 묶어서 알림" 계약이 깨집니다. CollectionChanged를 잠깐 끊고 직접 제거한 뒤 기존과
        // 동일하게 한 번만 removedEvent를 발행합니다.
        public int RemoveAll(Predicate<T> match)
        {
            if (match == null) return 0;

            List<(int index, T item)> matches = new();
            for (int i = 0; i < _runtime.Count; i++)
            {
                if (match(_runtime[i])) matches.Add((i, _runtime[i]));
            }

            if (matches.Count == 0) return 0;

            _runtime.CollectionChanged -= OnRuntimeCollectionChanged;
            try
            {
                for (int i = matches.Count - 1; i >= 0; i--)
                {
                    _runtime.RemoveAt(matches[i].index);
                }
            }
            finally
            {
                _runtime.CollectionChanged += OnRuntimeCollectionChanged;
            }

            int[] indices = new int[matches.Count];
            T[] items = new T[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {
                indices[i] = matches[i].index;
                items[i] = matches[i].item;
            }

            removedEvent.Invoke(indices, items);
            return matches.Count;
        }

        public void Clear()
        {
            if (_runtime.Count == 0) return;

            T[] items = _runtime.ToArray();
            int[] indices = Enumerable.Range(0, items.Length).ToArray();

            _runtime.CollectionChanged -= OnRuntimeCollectionChanged;
            try
            {
                _runtime.Clear();
            }
            finally
            {
                _runtime.CollectionChanged += OnRuntimeCollectionChanged;
            }

            removedEvent.Invoke(indices, items);
        }

        // -------------------- Reorder --------------------
        public void Move(int oldIndex, int newIndex)
        {
            if ((uint)oldIndex >= (uint)_runtime.Count || (uint)newIndex >= (uint)_runtime.Count) return;
            _runtime.Move(oldIndex, newIndex);
        }

        public void Reverse(int index, int count) { ReorderWithoutNotify(() => _runtime.Reverse(index, count)); }
        public void Reverse() { ReorderWithoutNotify(() => _runtime.Reverse()); }
        public void Sort(Comparison<T> comparison) { ReorderWithoutNotify(() => _runtime.Sort(Comparer<T>.Create(comparison))); }
        public void Sort(int index, int count, IComparer<T> comparer) { ReorderWithoutNotify(() => _runtime.Sort(index, count, comparer)); }
        public void Sort() { ReorderWithoutNotify(() => _runtime.Sort()); }
        public void Sort(IComparer<T> comparer) { ReorderWithoutNotify(() => _runtime.Sort(comparer)); }

        private void ReorderWithoutNotify(Action reorder)
        {
            _runtime.CollectionChanged -= OnRuntimeCollectionChanged;
            try
            {
                reorder();
            }
            finally
            {
                _runtime.CollectionChanged += OnRuntimeCollectionChanged;
            }

            reorderedEvent.Invoke(_runtime);
        }

        // -------------------- 기타 List<T> Wrappers --------------------
        public List<T> ToList() => _runtime.ToList();
        public T[] ToArray() => _runtime.ToArray();
        public ReadOnlyCollection<T> AsReadOnly() => new(_runtime.ToList());
        public bool Contains(T item) => _runtime.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _runtime.CopyTo(array, arrayIndex);
        public bool Exist(Predicate<T> match) => _runtime.Any(item => match(item));
        public T Find(Predicate<T> match) => _runtime.FirstOrDefault(item => match(item));
        public List<T> FindAll(Predicate<T> match) => _runtime.Where(item => match(item)).ToList();
        public int FindIndex(int startIndex, int count, Predicate<T> match) => _runtime.ToList().FindIndex(startIndex, count, match);
        public int FindIndex(int startIndex, Predicate<T> match) => _runtime.ToList().FindIndex(startIndex, match);
        public int FindIndex(Predicate<T> match) => _runtime.ToList().FindIndex(match);
        public void ForEach(Action<T> action) { foreach (T item in _runtime) action(item); }
        public List<T> GetRange(int index, int count) => _runtime.ToList().GetRange(index, count);
        public int IndexOf(T item, int index, int count) => _runtime.ToList().IndexOf(item, index, count);
        public int IndexOf(T item, int index) => _runtime.ToList().IndexOf(item, index);
        public int IndexOf(T item) => _runtime.IndexOf(item);
        public int LastIndexOf(T item) => _runtime.ToList().LastIndexOf(item);
        public int LastIndexOf(T item, int index) => _runtime.ToList().LastIndexOf(item, index);
        public int LastIndexOf(T item, int index, int count) => _runtime.ToList().LastIndexOf(item, index, count);
        public bool TrueForAll(Predicate<T> match) => _runtime.All(item => match(item));
        public IEnumerator<T> GetEnumerator() => _runtime.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _runtime.GetEnumerator();

        // 생성자
        public ReactiveList() { InitializeRuntime(list); }
        public ReactiveList(int capacity) { list = new List<T>(capacity); InitializeRuntime(list); }
        public ReactiveList(IEnumerable<T> collection) { list = new List<T>(collection ?? Array.Empty<T>()); InitializeRuntime(list); }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // Inspector/저장 시점의 스냅샷만 갱신합니다. _runtime이 실제 데이터의 원천입니다.
            list.Clear();
            list.AddRange(_runtime);
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // list는 Inspector에서 직접 편집됐을 수 있으므로(요소 추가/삭제/값 변경), 역직렬화마다
            // _runtime을 list 기준으로 새로 구성합니다.
            InitializeRuntime(list);
        }

        private void InitializeRuntime(List<T> source)
        {
            _runtime = new ObservableList<T>(source);
            _runtime.CollectionChanged += OnRuntimeCollectionChanged;
        }

        private void OnRuntimeCollectionChanged(in NotifyCollectionChangedEventArgs<T> e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    InvokeAddOrRemove(addedEvent, e.IsSingleItem, e.NewItem, e.NewItems, e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    InvokeAddOrRemove(removedEvent, e.IsSingleItem, e.OldItem, e.OldItems, e.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (e.IsSingleItem)
                    {
                        changedEvent.Invoke(e.NewStartingIndex, e.OldItem, e.NewItem);
                    }
                    else
                    {
                        for (int i = 0; i < e.NewItems.Length; i++)
                        {
                            changedEvent.Invoke(e.NewStartingIndex + i, e.OldItems[i], e.NewItems[i]);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    reorderedEvent.Invoke(_runtime);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    // Clear/Sort/Reverse는 각 메서드가 CollectionChanged를 일시 해제하고 직접
                    // 이벤트를 발행하므로 여기서는 아무 것도 하지 않습니다.
                    break;
            }
        }

        private void InvokeAddOrRemove(UnityEvent<int[], T[]> unityEvent, bool isSingleItem, T singleItem, ReadOnlySpan<T> items, int startingIndex)
        {
            if (isSingleItem)
            {
                unityEvent.Invoke(new[] { startingIndex }, new[] { singleItem });
                return;
            }

            T[] itemArray = items.ToArray();
            int[] indices = new int[itemArray.Length];
            for (int i = 0; i < indices.Length; i++) indices[i] = startingIndex + i;
            unityEvent.Invoke(indices, itemArray);
        }

        // -------------------- Helpers --------------------
        private static void AddListenerSafe(UnityEvent<int[], T[]> unityEvent, AddOrRemoveHandler<T> callback)
        {
            if (callback == null) return;
            unityEvent.AddListener((UnityAction<int[], T[]>)Delegate.CreateDelegate(typeof(UnityAction<int[], T[]>), callback.Target, callback.Method));
        }

        private static void RemoveListenerSafe(UnityEvent<int[], T[]> unityEvent, AddOrRemoveHandler<T> callback)
        {
            if (callback == null) return;
            unityEvent.RemoveListener((UnityAction<int[], T[]>)Delegate.CreateDelegate(typeof(UnityAction<int[], T[]>), callback.Target, callback.Method));
        }

        private static void AddListenerSafe(UnityEvent<int, T, T> unityEvent, ElementChangedHandler<T> callback)
        {
            if (callback == null) return;
            unityEvent.AddListener((UnityAction<int, T, T>)Delegate.CreateDelegate(typeof(UnityAction<int, T, T>), callback.Target, callback.Method));
        }

        private static void RemoveListenerSafe(UnityEvent<int, T, T> unityEvent, ElementChangedHandler<T> callback)
        {
            if (callback == null) return;
            unityEvent.RemoveListener((UnityAction<int, T, T>)Delegate.CreateDelegate(typeof(UnityAction<int, T, T>), callback.Target, callback.Method));
        }
    }
}
