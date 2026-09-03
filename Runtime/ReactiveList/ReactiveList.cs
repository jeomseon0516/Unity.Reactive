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

namespace Jeomseon.Unity.Reactive.ReactiveList
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

        // UnityEvent는 리스너 하나가 던진 예외를 격리하지 않습니다(UnityEventBase.Invoke는 개별 호출에
        // try/catch가 없어, 하나가 던지면 나머지 리스너 호출이 그대로 중단되고 예외가 호출자까지
        // 전파됩니다). addedEvent 등 필드 자체(및 Inspector persistent listener 직렬화)는 그대로 두고,
        // AddListener로 등록하는 런타임 리스너만 격리 wrapper로 감싸 등록합니다. 제거 시 원본으로
        // 다시 찾을 수 있도록 매핑을 보관합니다. Delegate.CreateDelegate(Target, Method)로 UnityAction을
        // 재구성하던 이전 방식은 이미 combine된 멀티캐스트 델리게이트가 들어오면 Target/Method가
        // 마지막 호출 대상만 가리켜 나머지 구독자가 조용히 누락되는 결함도 있었는데, 원본을 그대로
        // 호출하는 wrapper로 대체해 함께 해결됩니다. 동일 델리게이트(같은 Target+Method)가 여러 번
        // 구독될 수 있어(표준 멀티캐스트 이벤트 관례상 유효한 사용) 키당 wrapper 하나만 저장하면
        // 두 번째 구독이 첫 번째 매핑을 덮어써 첫 wrapper가 영원히 제거 불가능해지는 누수가 생깁니다.
        // Stack으로 보관해 add/remove를 LIFO로 짝지어 이 문제를 없앱니다.
        [NonSerialized] private readonly Dictionary<AddOrRemoveHandler<T>, Stack<UnityAction<int[], T[]>>> _addedListeners = new();
        [NonSerialized] private readonly Dictionary<AddOrRemoveHandler<T>, Stack<UnityAction<int[], T[]>>> _removedListeners = new();
        [NonSerialized] private readonly Dictionary<ElementChangedHandler<T>, Stack<UnityAction<int, T, T>>> _changedListeners = new();
        [NonSerialized] private readonly Dictionary<Action<IReadOnlyList<T>>, Stack<UnityAction<IReadOnlyList<T>>>> _reorderedListeners = new();

        public event AddOrRemoveHandler<T> AddedEvent
        {
            add
            {
                if (value == null) return;

                addedEvent.AddListener(RegisterIsolated(_addedListeners, value));

                int[] indices = new int[_runtime.Count];
                for (int i = 0; i < _runtime.Count; i++)
                {
                    indices[i] = i;
                }

                value.Invoke(indices, _runtime.ToArray());
            }
            remove => RemoveListenerSafe(addedEvent, _addedListeners, value);
        }

        public event AddOrRemoveHandler<T> RemovedEvent
        {
            add => AddListenerSafe(removedEvent, _removedListeners, value);
            remove => RemoveListenerSafe(removedEvent, _removedListeners, value);
        }

        public event ElementChangedHandler<T> ChangedEvent
        {
            add => AddListenerSafe(changedEvent, _changedListeners, value);
            remove => RemoveListenerSafe(changedEvent, _changedListeners, value);
        }

        public event Action<IReadOnlyList<T>> ReorderedEvent
        {
            add => AddListenerSafe(reorderedEvent, _reorderedListeners, value);
            remove => RemoveListenerSafe(reorderedEvent, _reorderedListeners, value);
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

        public void AddListenerToAddedEventWithoutNotify(AddOrRemoveHandler<T> onAddAction) => AddListenerSafe(addedEvent, _addedListeners, onAddAction);

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

            reorderedEvent.Invoke(_runtime.ToArray());
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
                    reorderedEvent.Invoke(_runtime.ToArray());
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
        // 아래 4쌍은 동일한 패턴을 UnityEvent<int[],T[]> / UnityEvent<int,T,T> / UnityEvent<IReadOnlyList<T>>
        // 각각에 대해 반복합니다(제네릭 하나로 묶으면 DynamicInvoke가 필요해져 매 호출마다 리플렉션
        // 비용이 드는 역행이라 타입별로 분리 유지).
        private static void AddListenerSafe(UnityEvent<int[], T[]> unityEvent, Dictionary<AddOrRemoveHandler<T>, Stack<UnityAction<int[], T[]>>> listeners, AddOrRemoveHandler<T> callback)
        {
            if (callback == null) return;
            unityEvent.AddListener(RegisterIsolated(listeners, callback));
        }

        private static void RemoveListenerSafe(UnityEvent<int[], T[]> unityEvent, Dictionary<AddOrRemoveHandler<T>, Stack<UnityAction<int[], T[]>>> listeners, AddOrRemoveHandler<T> callback)
        {
            if (callback == null) return;
            if (listeners.TryGetValue(callback, out Stack<UnityAction<int[], T[]>> stack) && stack.Count > 0)
            {
                unityEvent.RemoveListener(stack.Pop());
                if (stack.Count == 0) listeners.Remove(callback);
            }
        }

        private static UnityAction<int[], T[]> RegisterIsolated(Dictionary<AddOrRemoveHandler<T>, Stack<UnityAction<int[], T[]>>> listeners, AddOrRemoveHandler<T> original)
        {
            UnityAction<int[], T[]> isolated = (indices, items) =>
            {
                try { original(indices, items); }
                catch (Exception e) { Debug.LogException(e); }
            };

            if (!listeners.TryGetValue(original, out Stack<UnityAction<int[], T[]>> stack))
            {
                stack = new Stack<UnityAction<int[], T[]>>();
                listeners[original] = stack;
            }
            stack.Push(isolated);

            return isolated;
        }

        private static void AddListenerSafe(UnityEvent<int, T, T> unityEvent, Dictionary<ElementChangedHandler<T>, Stack<UnityAction<int, T, T>>> listeners, ElementChangedHandler<T> callback)
        {
            if (callback == null) return;

            UnityAction<int, T, T> isolated = (index, previous, current) =>
            {
                try { callback(index, previous, current); }
                catch (Exception e) { Debug.LogException(e); }
            };

            if (!listeners.TryGetValue(callback, out Stack<UnityAction<int, T, T>> stack))
            {
                stack = new Stack<UnityAction<int, T, T>>();
                listeners[callback] = stack;
            }
            stack.Push(isolated);

            unityEvent.AddListener(isolated);
        }

        private static void RemoveListenerSafe(UnityEvent<int, T, T> unityEvent, Dictionary<ElementChangedHandler<T>, Stack<UnityAction<int, T, T>>> listeners, ElementChangedHandler<T> callback)
        {
            if (callback == null) return;
            if (listeners.TryGetValue(callback, out Stack<UnityAction<int, T, T>> stack) && stack.Count > 0)
            {
                unityEvent.RemoveListener(stack.Pop());
                if (stack.Count == 0) listeners.Remove(callback);
            }
        }

        private static void AddListenerSafe(UnityEvent<IReadOnlyList<T>> unityEvent, Dictionary<Action<IReadOnlyList<T>>, Stack<UnityAction<IReadOnlyList<T>>>> listeners, Action<IReadOnlyList<T>> callback)
        {
            if (callback == null) return;

            UnityAction<IReadOnlyList<T>> isolated = snapshot =>
            {
                try { callback(snapshot); }
                catch (Exception e) { Debug.LogException(e); }
            };

            if (!listeners.TryGetValue(callback, out var stack))
            {
                stack = new Stack<UnityAction<IReadOnlyList<T>>>();
                listeners[callback] = stack;
            }
            stack.Push(isolated);

            unityEvent.AddListener(isolated);
        }

        private static void RemoveListenerSafe(UnityEvent<IReadOnlyList<T>> unityEvent, Dictionary<Action<IReadOnlyList<T>>, Stack<UnityAction<IReadOnlyList<T>>>> listeners, Action<IReadOnlyList<T>> callback)
        {
            if (callback == null) return;
            if (listeners.TryGetValue(callback, out var stack) && stack.Count > 0)
            {
                unityEvent.RemoveListener(stack.Pop());
                if (stack.Count == 0) listeners.Remove(callback);
            }
        }
    }
}
