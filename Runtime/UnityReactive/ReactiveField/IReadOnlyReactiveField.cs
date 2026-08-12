using System;

namespace Jeomseon.UnityReactive
{
    /// <summary>
    /// .. 읽기 전용 인터페이스 입니다 가장 기본적인 메서드만 제공합니다
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IReadOnlyReactiveField<out T>
    {
        T Value { get; }
        event Action<T> ChangedEvent;

        /// <summary>
        /// .. 리스너를 추가시 이벤트를 발생시키지 않습니다
        /// </summary>
        /// <param name="onChangedValue"> .. 이벤트 메서드 </param>
        void AddListenerWithoutNotify(Action<T> onChangedValue);
    }
}
