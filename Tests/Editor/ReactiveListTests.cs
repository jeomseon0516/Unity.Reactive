using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Jeomseon.Unity.Reactive.ReactiveList;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Jeomseon.Tests.Reactive
{
    // ROADMAP P0-01: Add/Remove/Replace/Move/Clear와 재진입, listener 제거, 예외 발생 시 동작을
    // 검증합니다. ReactiveList<T>는 이제 ObservableList<T>(ObservableCollections)에 위임하므로,
    // 여기서는 "우리 UnityEvent 어댑터가 정확히 중계하는지"와 "Clear/Sort/Reverse의 구독 일시
    // 해제-재구독-수동 발행이 안전한지"를 중점적으로 검증합니다.
    public sealed class ReactiveListTests
    {
        [Test]
        public void AddedEvent_SubscribeReplaysCurrentItems()
        {
            ReactiveList<int> list = new(new[] { 1, 2, 3 });
            List<(int[] indices, int[] items)> received = new();

            list.AddedEvent += (indices, items) => received.Add((indices, items));

            Assert.That(received.Count, Is.EqualTo(1));
            Assert.That(received[0].indices, Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(received[0].items, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void Add_FiresAddedEventWithCorrectIndexAndItem()
        {
            ReactiveList<int> list = new();
            List<(int[] indices, int[] items)> received = new();
            list.AddedEvent += (indices, items) => received.Add((indices, items));
            received.Clear();

            list.Add(10);
            list.Add(20);

            // NUnit 3.5(Unity 6000.5.7f1 번들)는 배열을 담은 ValueTuple을 구조적으로 비교하지
            // 않고 참조 동일성만 봐서 항상 실패합니다. 필드를 개별로 비교합니다.
            Assert.That(received.Count, Is.EqualTo(2));
            Assert.That(received[0].indices, Is.EqualTo(new[] { 0 }));
            Assert.That(received[0].items, Is.EqualTo(new[] { 10 }));
            Assert.That(received[1].indices, Is.EqualTo(new[] { 1 }));
            Assert.That(received[1].items, Is.EqualTo(new[] { 20 }));
        }

        [Test]
        public void AddRange_FiresSingleAddedEventWithAllIndices()
        {
            ReactiveList<int> list = new(new[] { 1 });
            List<(int[] indices, int[] items)> received = new();
            list.AddedEvent += (indices, items) => received.Add((indices, items));
            received.Clear();

            list.AddRange(new[] { 2, 3, 4 });

            Assert.That(received.Count, Is.EqualTo(1));
            Assert.That(received[0].indices, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(received[0].items, Is.EqualTo(new[] { 2, 3, 4 }));
        }

        [Test]
        public void Insert_AtMiddle_FiresAddedEventWithGivenIndex()
        {
            ReactiveList<string> list = new(new[] { "a", "c" });
            List<(int[] indices, string[] items)> received = new();
            list.AddedEvent += (indices, items) => received.Add((indices, items));
            received.Clear();

            list.Insert(1, "b");

            Assert.That(list.ToArray(), Is.EqualTo(new[] { "a", "b", "c" }));
            Assert.That(received[0].indices, Is.EqualTo(new[] { 1 }));
            Assert.That(received[0].items, Is.EqualTo(new[] { "b" }));
        }

        [Test]
        public void Remove_ExistingItem_FiresRemovedEventAndReturnsTrue()
        {
            ReactiveList<int> list = new(new[] { 1, 2, 3 });
            List<(int[] indices, int[] items)> received = new();
            list.RemovedEvent += (indices, items) => received.Add((indices, items));

            bool removed = list.Remove(2);

            Assert.That(removed, Is.True);
            Assert.That(received.Count, Is.EqualTo(1));
            Assert.That(received[0].indices, Is.EqualTo(new[] { 1 }));
            Assert.That(received[0].items, Is.EqualTo(new[] { 2 }));
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 3 }));
        }

        [Test]
        public void Remove_MissingItem_DoesNotFireRemovedEventAndReturnsFalse()
        {
            ReactiveList<int> list = new(new[] { 1, 2, 3 });
            bool fired = false;
            list.RemovedEvent += (_, _) => fired = true;

            bool removed = list.Remove(99);

            Assert.That(removed, Is.False);
            Assert.That(fired, Is.False);
        }

        [Test]
        public void RemoveAt_FiresRemovedEventWithCorrectIndexAndItem()
        {
            ReactiveList<int> list = new(new[] { 1, 2, 3 });
            List<(int[] indices, int[] items)> received = new();
            list.RemovedEvent += (indices, items) => received.Add((indices, items));

            list.RemoveAt(0);

            Assert.That(received[0].indices, Is.EqualTo(new[] { 0 }));
            Assert.That(received[0].items, Is.EqualTo(new[] { 1 }));
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public void RemoveRange_FiresSingleRemovedEventWithAllItems()
        {
            ReactiveList<int> list = new(new[] { 1, 2, 3, 4, 5 });
            List<(int[] indices, int[] items)> received = new();
            list.RemovedEvent += (indices, items) => received.Add((indices, items));

            list.RemoveRange(1, 2);

            Assert.That(received.Count, Is.EqualTo(1));
            Assert.That(received[0].indices, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(received[0].items, Is.EqualTo(new[] { 2, 3 }));
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 4, 5 }));
        }

        [Test]
        public void RemoveAll_FiresExactlyOneCombinedRemovedEvent()
        {
            ReactiveList<int> list = new(new[] { 1, 2, 3, 4, 5, 6 });
            List<(int[] indices, int[] items)> received = new();
            list.RemovedEvent += (indices, items) => received.Add((indices, items));

            int removedCount = list.RemoveAll(value => value % 2 == 0);

            Assert.That(removedCount, Is.EqualTo(3));
            // 개별 RemoveAt마다 이벤트가 따로 나가면 안 되고, 한 번에 묶여서 나가야 합니다.
            Assert.That(received.Count, Is.EqualTo(1));
            Assert.That(received[0].indices, Is.EqualTo(new[] { 1, 3, 5 }));
            Assert.That(received[0].items, Is.EqualTo(new[] { 2, 4, 6 }));
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 3, 5 }));
        }

        [Test]
        public void RemoveAll_NoMatches_DoesNotFireRemovedEvent()
        {
            ReactiveList<int> list = new(new[] { 1, 3, 5 });
            bool fired = false;
            list.RemovedEvent += (_, _) => fired = true;

            int removedCount = list.RemoveAll(value => value % 2 == 0);

            Assert.That(removedCount, Is.EqualTo(0));
            Assert.That(fired, Is.False);
        }

        [Test]
        public void Clear_FiresExactlyOneCombinedRemovedEventWithAllItems()
        {
            ReactiveList<int> list = new(new[] { 1, 2, 3 });
            List<(int[] indices, int[] items)> received = new();
            list.RemovedEvent += (indices, items) => received.Add((indices, items));

            list.Clear();

            Assert.That(received.Count, Is.EqualTo(1));
            Assert.That(received[0].indices, Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(received[0].items, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(list.Count, Is.EqualTo(0));
        }

        [Test]
        public void Clear_EmptyList_DoesNotFireRemovedEvent()
        {
            ReactiveList<int> list = new();
            bool fired = false;
            list.RemovedEvent += (_, _) => fired = true;

            list.Clear();

            Assert.That(fired, Is.False);
        }

        [Test]
        public void IndexerSet_FiresChangedEventWithOldAndNewValue()
        {
            ReactiveList<int> list = new(new[] { 1, 2, 3 });
            List<(int index, int previous, int current)> received = new();
            list.ChangedEvent += (index, previous, current) => received.Add((index, previous, current));

            list[1] = 20;

            Assert.That(received.Count, Is.EqualTo(1));
            Assert.That(received[0], Is.EqualTo((1, 2, 20)));
            Assert.That(list[1], Is.EqualTo(20));
        }

        [Test]
        public void IndexerSet_OutOfRange_IsSilentNoOp()
        {
            ReactiveList<int> list = new(new[] { 1, 2, 3 });
            bool fired = false;
            list.ChangedEvent += (_, _, _) => fired = true;

            list[5] = 99;

            Assert.That(fired, Is.False);
        }

        [Test]
        public void Move_ReordersItemsAndFiresReorderedEvent()
        {
            ReactiveList<int> list = new(new[] { 1, 2, 3, 4 });
            int reorderedCount = 0;
            IReadOnlyList<int> lastSnapshot = null;
            list.ReorderedEvent += snapshot => { reorderedCount++; lastSnapshot = snapshot; };

            list.Move(0, 2);

            Assert.That(list.ToArray(), Is.EqualTo(new[] { 2, 3, 1, 4 }));
            Assert.That(reorderedCount, Is.EqualTo(1));
            Assert.That(lastSnapshot, Is.EqualTo(new[] { 2, 3, 1, 4 }));
        }

        [Test]
        public void Move_OutOfRange_IsSilentNoOp()
        {
            ReactiveList<int> list = new(new[] { 1, 2, 3 });
            bool fired = false;
            list.ReorderedEvent += _ => fired = true;

            list.Move(0, 10);
            list.Move(-1, 1);

            Assert.That(fired, Is.False);
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void Sort_FiresExactlyOneReorderedEvent()
        {
            ReactiveList<int> list = new(new[] { 3, 1, 2 });
            int reorderedCount = 0;
            list.ReorderedEvent += _ => reorderedCount++;

            list.Sort();

            Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(reorderedCount, Is.EqualTo(1));
        }

        [Test]
        public void Reverse_FiresExactlyOneReorderedEvent()
        {
            ReactiveList<int> list = new(new[] { 1, 2, 3 });
            int reorderedCount = 0;
            list.ReorderedEvent += _ => reorderedCount++;

            list.Reverse();

            Assert.That(list.ToArray(), Is.EqualTo(new[] { 3, 2, 1 }));
            Assert.That(reorderedCount, Is.EqualTo(1));
        }

        [Test]
        public void RemoveAt_OutOfRange_IsSilentNoOp()
        {
            ReactiveList<int> list = new(new[] { 1, 2, 3 });
            bool fired = false;
            list.RemovedEvent += (_, _) => fired = true;

            list.RemoveAt(10);

            Assert.That(fired, Is.False);
            Assert.That(list.Count, Is.EqualTo(3));
        }

        [Test]
        public void Reentrancy_AddDuringRemovedEvent_DoesNotThrowAndBothMutationsApply()
        {
            ReactiveList<int> list = new(new[] { 1, 2 });
            bool reentered = false;
            list.RemovedEvent += (_, _) =>
            {
                if (reentered) return;
                reentered = true;
                list.Add(99); // 리스너 안에서 같은 리스트를 재진입 변경
            };

            Assert.DoesNotThrow(() => list.Remove(1));

            Assert.That(list.ToArray(), Is.EqualTo(new[] { 2, 99 }));
        }

        [Test]
        public void RemoveListener_DuringDispatch_StopsReceivingFurtherEvents()
        {
            ReactiveList<int> list = new();
            int callCount = 0;
            AddOrRemoveHandler<int> handler = null;
            handler = (_, _) =>
            {
                callCount++;
                list.RemovedEvent -= handler;
            };
            list.RemovedEvent += handler;

            list.Add(1);
            list.Add(2);
            list.Remove(1);
            list.Remove(2);

            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void ListenerException_DoesNotPreventListStateChangeOrOtherListeners()
        {
            ReactiveList<int> list = new();
            bool secondListenerCalled = false;
            // AddedEvent += 는 구독 시점에 현재 항목을 replay하므로(빈 리스트라도 빈 배열로 1회
            // 호출됨), 예외를 던지는 리스너를 여기 등록하면 Add(1) 호출 전에 구독 자체에서 예외가
            // 터집니다. AddListenerToAddedEventWithoutNotify로 replay 없이 등록해 검증 대상(Add
            // 중 예외 격리)에만 집중합니다.
            list.AddListenerToAddedEventWithoutNotify((_, _) => throw new InvalidOperationException("intentional test exception"));
            list.AddListenerToAddedEventWithoutNotify((_, _) => secondListenerCalled = true);

            LogAssert.Expect(LogType.Exception, new Regex(".*"));

            Assert.DoesNotThrow(() => list.Add(1));
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(secondListenerCalled, Is.True);
        }

        [Test]
        public void AddListenerToAddedEventWithoutNotify_SameDelegateSubscribedTwice_RemovingOnceStopsExactlyOneRegistration()
        {
            // 예외 격리를 위해 리스너를 wrapper로 감싸 등록할 때, 동일 델리게이트(같은 Target+Method)를
            // 두 번 구독한 뒤 한 번만 해지하면 여전히 한 번은 호출돼야 합니다(표준 멀티캐스트 이벤트
            // 관례). 매핑을 키당 값 하나만 저장하면 두 번째 구독이 첫 번째 wrapper 참조를 덮어써
            // 이후 제거가 불가능해지는 누수가 됩니다.
            ReactiveList<int> list = new();
            int callCount = 0;
            AddOrRemoveHandler<int> handler = (_, _) => callCount++;
            list.AddListenerToAddedEventWithoutNotify(handler);
            list.AddListenerToAddedEventWithoutNotify(handler);

            list.Add(1);
            Assert.That(callCount, Is.EqualTo(2));

            list.AddedEvent -= handler;
            callCount = 0;
            list.Add(2);
            Assert.That(callCount, Is.EqualTo(1));

            list.AddedEvent -= handler;
            callCount = 0;
            list.Add(3);
            Assert.That(callCount, Is.EqualTo(0));
        }
    }
}
