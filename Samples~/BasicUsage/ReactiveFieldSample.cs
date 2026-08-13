using Jeomseon.Unity.Reactive.ReactiveField;
using Jeomseon.Unity.Reactive.ValueProcessor;
using UnityEngine;

namespace Jeomseon.Samples.Reactive
{
    public sealed class ReactiveFieldSample : MonoBehaviour
    {
        [SerializeField] private ReactiveField<int> health = new();

        private void Awake()
        {
            if (health.ValueProcessors.Count == 0)
            {
                health.ValueProcessors.Add(new ClampIntProcessor(0, 10));
            }

            health.ChangedEvent += value => Debug.Log($"체력 변경: {value}");

            health.Value = 15; // ClampIntProcessor에 의해 10으로 저장·발행됩니다.
            health.Value = 10; // 이전 값과 같아 ChangedEvent가 다시 발행되지 않습니다.
            health.SetValueAndForceInvoke(10); // 같은 값이어도 강제로 발행됩니다.
        }
    }
}
