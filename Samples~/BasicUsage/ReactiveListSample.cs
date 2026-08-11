using Jeomseon.UnityReactive;
using UnityEngine;

namespace Jeomseon.Samples.Reactive
{
    public sealed class ReactiveListSample : MonoBehaviour
    {
        private readonly ReactiveList<string> _items = new();

        private void Awake()
        {
            _items.AddListenerToAddedEventWithoutNotify(
                (indices, items) => Debug.Log($"항목 추가: {items[0]}"));
            _items.Add("Reactive Item");
        }
    }
}
