using System.Collections.Generic;
using System.Linq;
using Jeomseon.Unity.Attributes;
using Jeomseon.Unity.Reactive.ValueProcessor;
using UnityEngine;

namespace Jeomseon.Unity.Reactive.ReactiveField
{
    [System.Serializable]
    public class ReactiveField<T> : ReactiveFieldBase<T>
    {
        private static readonly EqualityComparer<T> DefaultEqualityComparer = EqualityComparer<T>.Default;

        // 프로퍼티(Value)가 이미 이 이름을 쓰고 있어 PascalCase(Value)를 쓸 수 없습니다. 오토
        // 프로퍼티([field: SerializeField])로 두면 컴파일러가 생성하는 백킹 필드 이름
        // (<_value>k__BackingField)에 직렬화가 묶여 상태가 불안정해지므로, 일반
        // [SerializeField] 필드로 내려 명명 규칙(SerializeField 필드는 camelCase)을 그대로
        // 따르게 했습니다. 이 변경으로 기존에 직렬화된 Scene/Prefab의 값은 초기화됩니다(의도적).
        [SerializeField] protected T value;

        protected virtual EqualityComparer<T> EqualityComparer => DefaultEqualityComparer;

        /// <summary>
        /// .. SetValueAndForceInvoke 메서드와 Value의 Setter 호출 시 인자로 들어온 값을
        /// 필터링하는 프로세서 인터페이스입니다 사용자 정의 프로세서를 추가시킬시 값을
        /// 프로세서에 한번 필터 시킨후 값을 초기화 시킵니다.
        /// </summary>
        [field: SerializeReference, SerializeReferenceSelector]
        public List<IValueProcessor> ValueProcessors { get; set; } = new();

        public override T Value
        {
            get => value;
            set
            {
                // set 접근자 안에서 "value"는 세터의 암시적 매개변수(새로 들어온 값)를 가리키므로,
                // 필드 자체를 읽거나 쓸 때는 반드시 this.value로 구분합니다. 그냥 value를 쓰면
                // 매개변수에만 대입되고 필드는 갱신되지 않는 조용한 버그가 됩니다.
                T processedValue = GetProcessedValue(value);

                if (!EqualityComparer.Equals(this.value, processedValue))
                {
                    this.value = processedValue;
                    changedEvent.Invoke(this.value);
                }
            }
        }

        public override void SetValueAndForceInvoke(T value)
        {
            this.value = GetProcessedValue(value);
            changedEvent.Invoke(this.value);
        }

        protected T GetProcessedValue(T value) => ValueProcessors is null
            ? value :
            ValueProcessors
                .Where(processor => processor is not null)
                .Aggregate(value, (current, processor) => processor.Process(current));
    }
}
