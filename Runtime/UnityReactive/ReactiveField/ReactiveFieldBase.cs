using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Jeomseon.Events;
using UnityEngine.Serialization;

namespace Jeomseon.UnityReactive
{
    [System.Serializable]
    public abstract class ReactiveFieldBase<T> : IReactiveField<T>
    {
        [SerializeField, FormerlySerializedAs("_changedEvent")] protected UnityEvent<T> changedEvent = new();

        // UnityEvent는 리스너 하나가 던진 예외를 격리하지 않습니다(UnityEventBase.Invoke는 개별 호출에
        // try/catch가 없어, 하나가 던지면 나머지 리스너 호출이 그대로 중단되고 예외가 호출자까지
        // 전파됩니다). 원본 델리게이트를 직접 등록하는 대신 격리 wrapper로 감싸 등록하고, 제거 시
        // 원본으로 다시 찾을 수 있도록 매핑을 보관합니다. 동일 델리게이트(같은 Target+Method)가
        // 여러 번 구독될 수 있어(표준 멀티캐스트 이벤트 관례상 유효한 사용) 키당 wrapper 하나만
        // 저장하면 두 번째 구독이 첫 번째 매핑을 덮어써 첫 wrapper가 영원히 제거 불가능해지는
        // 누수가 생깁니다. Stack으로 보관해 add/remove를 LIFO로 짝지어 이 문제를 없앱니다.
        [NonSerialized] private readonly Dictionary<Action<T>, Stack<UnityAction<T>>> _isolatedListeners = new();

        /// <summary>
        /// .. 리스너를 추가 제거 시킵니다. 리스너 등록과 동시에 이벤트가 한번 호출됩니다
        /// </summary>
        public event Action<T> ChangedEvent
        {
            add
            {
                if (value is null) return;
                changedEvent.AddListener(RegisterIsolated(value));
                value.Invoke(Value);
            }
            remove
            {
                if (value is null) return;
                if (_isolatedListeners.TryGetValue(value, out Stack<UnityAction<T>> stack) && stack.Count > 0)
                {
                    changedEvent.RemoveListener(stack.Pop());
                    if (stack.Count == 0) _isolatedListeners.Remove(value);
                }
            }
        }

        public abstract T Value { get; set; }

        public virtual void SetValueAndForceInvoke(T value)
        {
            Value = value;
            changedEvent.Invoke(Value);
        }


        public void AddListenerWithoutNotify(Action<T> onChangedValue)
        {
            if (onChangedValue is null) return;

            changedEvent.AddListener(RegisterIsolated(onChangedValue));
        }

        // Delegate.CreateDelegate(Target, Method)로 UnityAction을 재구성하던 이전 방식은 리플렉션
        // 비용 외에도, 이미 combine된 멀티캐스트 델리게이트가 들어오면 Target/Method가 마지막 호출
        // 대상만 가리켜 나머지 구독자가 조용히 누락되는 결함이 있었습니다. 원본을 그대로 호출하는
        // wrapper로 대체해 이 문제와 예외 격리를 함께 해결합니다.
        private UnityAction<T> RegisterIsolated(Action<T> original)
        {
            UnityAction<T> isolated = newValue =>
            {
                try
                {
                    original(newValue);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            };

            if (!_isolatedListeners.TryGetValue(original, out Stack<UnityAction<T>> stack))
            {
                stack = new Stack<UnityAction<T>>();
                _isolatedListeners[original] = stack;
            }
            stack.Push(isolated);

            return isolated;
        }

        public int GetPersistentEventCount() => changedEvent.GetPersistentEventCount();
        public UnityEngine.Object GetPersistentTarget(int index) => changedEvent.GetPersistentTarget(index);
        public void SetPersistentListenerState(UnityEventCallState callState) => changedEvent.SetPersistentListenerState(callState);
        public void SetPersistentListenerState(int index, UnityEventCallState callState) => changedEvent.SetPersistentListenerState(index, callState);
        public void RemoveAllListener() => changedEvent.RemoveAllListeners();
        public string GetPersistentMethodName(int index) => changedEvent.GetPersistentMethodName(index);
        public override string ToString() => Value.ToString();
    }
}
