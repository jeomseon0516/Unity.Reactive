using System;
using System.Collections.Generic;
using Jeomseon.Unity.Reactive.R3;
using Jeomseon.Unity.Reactive.ReactiveField;
using Jeomseon.Unity.Reactive.ReactiveList;
using NUnit.Framework;
using R3;

namespace Jeomseon.Tests.Reactive.R3
{
    public sealed class ReactiveR3ExtensionsTests
    {
        [Test]
        public void ObserveValue_ReplaysCurrentValueAndForwardsChangesUntilDisposed()
        {
            var field = new ReactiveField<int> { Value = 10 };
            var received = new List<int>();
            var subscription = field.ObserveValue().Subscribe(received.Add);

            field.Value = 20;
            subscription.Dispose();
            field.Value = 30;

            Assert.That(received, Is.EqualTo(new[] { 10, 20 }));
        }

        [Test]
        public void ObserveValue_NullSource_ThrowsArgumentNullException()
        {
            IReadOnlyReactiveField<int> source = null;

            var exception = Assert.Throws<ArgumentNullException>(() => source.ObserveValue());

            Assert.That(exception.ParamName, Is.EqualTo("source"));
        }

        [Test]
        public void ObserveAdded_ReplaysCurrentItemsAndForwardsRangeUntilDisposed()
        {
            var list = new ReactiveList<int>(new[] { 1, 2 });
            var received = new List<(int[] Indices, int[] Items)>();
            var subscription = list.ObserveAdded().Subscribe(received.Add);

            list.AddRange(new[] { 3, 4 });
            subscription.Dispose();
            list.Add(5);

            Assert.That(received.Count, Is.EqualTo(2));
            Assert.That(received[0].Indices, Is.EqualTo(new[] { 0, 1 }));
            Assert.That(received[0].Items, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(received[1].Indices, Is.EqualTo(new[] { 2, 3 }));
            Assert.That(received[1].Items, Is.EqualTo(new[] { 3, 4 }));
        }

        [Test]
        public void ObserveRemoved_ForwardsRemovalUntilDisposed()
        {
            var list = new ReactiveList<int>(new[] { 1, 2, 3 });
            var received = new List<(int[] Indices, int[] Items)>();
            var subscription = list.ObserveRemoved().Subscribe(received.Add);

            list.RemoveAt(1);
            subscription.Dispose();
            list.RemoveAt(0);

            Assert.That(received.Count, Is.EqualTo(1));
            Assert.That(received[0].Indices, Is.EqualTo(new[] { 1 }));
            Assert.That(received[0].Items, Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void ObserveChanged_ForwardsIndexAndValuesUntilDisposed()
        {
            var list = new ReactiveList<int>(new[] { 1, 2, 3 });
            var received = new List<(int Index, int Previous, int Current)>();
            var subscription = list.ObserveChanged().Subscribe(received.Add);

            list[1] = 20;
            subscription.Dispose();
            list[2] = 30;

            Assert.That(received, Is.EqualTo(new[] { (1, 2, 20) }));
        }

        [Test]
        public void ObserveReordered_ForwardsStableSnapshotUntilDisposed()
        {
            var list = new ReactiveList<int>(new[] { 1, 2, 3 });
            var received = new List<IReadOnlyList<int>>();
            var subscription = list.ObserveReordered().Subscribe(received.Add);

            list.Move(0, 2);
            var firstSnapshot = received[0];
            list.Add(4);

            Assert.That(firstSnapshot, Is.EqualTo(new[] { 2, 3, 1 }));
            Assert.That(firstSnapshot.Count, Is.EqualTo(3));

            subscription.Dispose();
            list.Reverse();

            Assert.That(received.Count, Is.EqualTo(1));
        }

        [Test]
        public void ListObservers_NullSource_ThrowArgumentNullException()
        {
            IReadOnlyReactiveList<int> source = null;

            Assert.That(Assert.Throws<ArgumentNullException>(() => source.ObserveAdded()).ParamName, Is.EqualTo("source"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => source.ObserveRemoved()).ParamName, Is.EqualTo("source"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => source.ObserveChanged()).ParamName, Is.EqualTo("source"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => source.ObserveReordered()).ParamName, Is.EqualTo("source"));
        }
    }
}
