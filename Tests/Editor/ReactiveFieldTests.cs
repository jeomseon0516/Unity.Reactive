using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Jeomseon.UnityReactive;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Jeomseon.Tests.Reactive
{
    // ROADMAP P0-01: ReactiveField<T>의 이벤트 정확성(같은 값 무시, 강제 발행, ValueProcessors
    // 파이프라인), 재진입, 리스너 제거, 예외 발생 시 동작을 검증합니다.
    public sealed class ReactiveFieldTests
    {
        [Test]
        public void Value_SetDifferentValue_FiresChangedEvent()
        {
            ReactiveField<int> field = new();
            List<int> received = new();
            field.ChangedEvent += received.Add;
            received.Clear();

            field.Value = 5;

            Assert.That(received, Is.EqualTo(new[] { 5 }));
            Assert.That(field.Value, Is.EqualTo(5));
        }

        [Test]
        public void Value_SetSameValue_DoesNotFireChangedEvent()
        {
            ReactiveField<int> field = new();
            field.Value = 5;
            bool fired = false;
            field.ChangedEvent += _ => fired = true; // 구독 시 현재 값을 replay하므로 fired가 먼저 true가 됩니다.
            fired = false;

            field.Value = 5;

            Assert.That(fired, Is.False);
        }

        [Test]
        public void ChangedEvent_Subscribe_ReplaysCurrentValue()
        {
            ReactiveField<int> field = new();
            field.Value = 42;
            List<int> received = new();

            field.ChangedEvent += received.Add;

            Assert.That(received, Is.EqualTo(new[] { 42 }));
        }

        [Test]
        public void AddListenerWithoutNotify_DoesNotReplayCurrentValue()
        {
            ReactiveField<int> field = new();
            field.Value = 42;
            bool fired = false;

            field.AddListenerWithoutNotify(_ => fired = true);

            Assert.That(fired, Is.False);
        }

        [Test]
        public void SetValueAndForceInvoke_SameValue_FiresChangedEventAnyway()
        {
            ReactiveField<int> field = new();
            field.Value = 5;
            int callCount = 0;
            field.ChangedEvent += _ => callCount++; // 구독 시 현재 값을 replay하므로 callCount가 먼저 1이 됩니다.
            callCount = 0;

            field.SetValueAndForceInvoke(5);

            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(field.Value, Is.EqualTo(5));
        }

        [Test]
        public void ValueProcessors_ClampIntProcessor_ClampsBeforeStoringAndFiring()
        {
            ReactiveField<int> field = new();
            field.ValueProcessors.Add(new ClampIntProcessor(0, 10));
            List<int> received = new();
            field.ChangedEvent += received.Add;
            received.Clear();

            field.Value = 100;

            Assert.That(field.Value, Is.EqualTo(10));
            Assert.That(received, Is.EqualTo(new[] { 10 }));
        }

        [Test]
        public void ValueProcessors_ChainMultipleProcessorsInOrder()
        {
            ReactiveField<int> field = new();
            field.ValueProcessors.Add(new MinIntProcessor(0));
            field.ValueProcessors.Add(new MaxIntProcessor(10));

            field.Value = -5;
            Assert.That(field.Value, Is.EqualTo(0));

            field.Value = 999;
            Assert.That(field.Value, Is.EqualTo(10));
        }

        [Test]
        public void ValueProcessors_DoNotAffectMismatchedType()
        {
            // IValueProcessor.Process<T>는 value is int 패턴 매칭이라, T가 int가 아니면 그대로
            // 통과시킵니다.
            ReactiveField<string> field = new();
            field.ValueProcessors.Add(new ClampIntProcessor(0, 10));

            field.Value = "hello";

            Assert.That(field.Value, Is.EqualTo("hello"));
        }

        [Test]
        public void ValueProcessors_NullElement_IsSkipped()
        {
            ReactiveField<int> field = new();
            field.ValueProcessors.Add(null);
            field.ValueProcessors.Add(new MaxIntProcessor(10));

            Assert.DoesNotThrow(() => field.Value = 100);
            Assert.That(field.Value, Is.EqualTo(10));
        }

        [Test]
        public void ValueProcessorTypes_ParameterlessConstruction_SucceedsForSelector()
        {
            Type[] processorTypes =
            {
                typeof(ClampIntProcessor),
                typeof(MinIntProcessor),
                typeof(MaxIntProcessor)
            };

            foreach (Type processorType in processorTypes)
            {
                Assert.That(Activator.CreateInstance(processorType), Is.InstanceOf<IValueProcessor>());
            }
        }

        [Test]
        public void ToString_ReturnsValueToString()
        {
            ReactiveField<int> field = new();
            field.Value = 7;

            Assert.That(field.ToString(), Is.EqualTo("7"));
        }

        [Test]
        public void RemoveListener_StopsReceivingFurtherEvents()
        {
            ReactiveField<int> field = new();
            int callCount = 0;
            Action<int> handler = _ => callCount++;
            field.ChangedEvent += handler; // 구독 시 replay 1회
            field.ChangedEvent -= handler;

            field.Value = 1;
            field.Value = 2;

            Assert.That(callCount, Is.EqualTo(1)); // replay 1회만, 이후 변경은 수신 안 함
        }

        [Test]
        public void Reentrancy_SetValueDuringChangedEvent_StabilizesWithoutInfiniteLoop()
        {
            ReactiveField<int> field = new();
            bool reentered = false;
            // ChangedEvent += 는 구독 시점에 현재 값을 즉시 재생하는데(아래
            // ChangedEvent_Subscribe_ReplayInvokeIsNotExceptionIsolated 테스트 참고), 그 replay
            // 자체가 재진입 체인을 한 번 미리 소모해버려 이 테스트의 관심사(명시적 Value 대입 중
            // 재진입)와 섞입니다. AddListenerWithoutNotify로 구독해 replay 없이 순수하게
            // 재진입만 검증합니다.
            field.AddListenerWithoutNotify(value =>
            {
                if (reentered) return;
                reentered = true;
                if (value < 3) field.Value = value + 1;
            });

            Assert.DoesNotThrow(() => field.Value = 1);

            Assert.That(field.Value, Is.EqualTo(2));
        }

        [Test]
        public void ChangedEvent_Subscribe_ReplayInvokeIsNotExceptionIsolated()
        {
            // ReactiveFieldBase.ChangedEvent의 add는 구독 직후 현재 값을 "value.Invoke(Value)"로
            // 직접 호출합니다(UnityEvent.Invoke 경유가 아님). 그래서 이 replay 시점의 예외는
            // UnityEvent처럼 격리되지 않고 구독 호출자에게 그대로 전파됩니다 — Value 변경으로
            // 발행되는 예외(changedEvent.Invoke 경유, 아래 테스트에서 격리됨을 확인)와는 다른
            // 동작입니다.
            ReactiveField<int> field = new();
            field.Value = 1;

            Assert.Throws<InvalidOperationException>(() =>
                field.ChangedEvent += _ => throw new InvalidOperationException("intentional test exception"));
        }

        [Test]
        public void ListenerException_DuringValueChange_DoesNotPreventOtherListenersOrStateChange()
        {
            ReactiveField<int> field = new();
            bool secondListenerCalled = false;
            // AddListenerWithoutNotify는 replay 없이 등록만 하므로, 이후 Value 변경 시
            // changedEvent.Invoke(UnityEvent, 리스너별 예외 격리)로만 호출됩니다.
            field.AddListenerWithoutNotify(_ => throw new InvalidOperationException("intentional test exception"));
            field.AddListenerWithoutNotify(_ => secondListenerCalled = true);

            LogAssert.Expect(LogType.Exception, new Regex(".*"));

            Assert.DoesNotThrow(() => field.Value = 10);
            Assert.That(field.Value, Is.EqualTo(10));
            Assert.That(secondListenerCalled, Is.True);
        }

        [Test]
        public void AddListenerWithoutNotify_SameDelegateSubscribedTwice_RemovingOnceStopsExactlyOneRegistration()
        {
            // 예외 격리를 위해 리스너를 wrapper로 감싸 등록할 때, 동일 델리게이트(같은 Target+Method)를
            // 두 번 구독한 뒤 한 번만 해지하면 여전히 한 번은 호출돼야 합니다(표준 멀티캐스트 이벤트
            // 관례). 매핑을 키당 값 하나만 저장하면 두 번째 구독이 첫 번째 wrapper 참조를 덮어써
            // 이후 제거가 불가능해지는 누수가 됩니다.
            ReactiveField<int> field = new();
            int callCount = 0;
            Action<int> handler = _ => callCount++;
            field.AddListenerWithoutNotify(handler);
            field.AddListenerWithoutNotify(handler);

            field.Value = 1;
            Assert.That(callCount, Is.EqualTo(2));

            field.ChangedEvent -= handler;
            callCount = 0;
            field.Value = 2;
            Assert.That(callCount, Is.EqualTo(1));

            field.ChangedEvent -= handler;
            callCount = 0;
            field.Value = 3;
            Assert.That(callCount, Is.EqualTo(0));
        }
    }
}
