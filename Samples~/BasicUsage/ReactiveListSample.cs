using Jeomseon.Unity.Reactive.ReactiveList;
using UnityEngine;

namespace Jeomseon.Samples.Reactive
{
    public sealed class ReactiveListSample : MonoBehaviour
    {
        [SerializeField] private ReactiveList<string> items = new();

        private void Awake()
        {
            items.AddListenerToAddedEventWithoutNotify(
                (indices, addedItems) => Debug.Log($"항목 추가: {addedItems[0]}"));
            items.Add("Reactive Item");
        }
    }
}
