namespace Jeomseon.UnityReactive
{
    public interface IReactiveField<T> : IReadOnlyReactiveField<T>
    {
        /// <summary>
        /// .. 값을 읽거나 쓸 수 있습니다. 값을 변경시 이전의 값과 다른 값일 경우에만 이벤트를 트리거합니다
        /// </summary>
        new T Value { get; set; }

        /// <summary>
        /// .. 설정할 값이 이전의 값과 같은 값이어도 이벤트를 강제로 트리거시킵니다
        /// </summary>
        /// <param name="value"> value </param>
        void SetValueAndForceInvoke(T value);
    }
}
