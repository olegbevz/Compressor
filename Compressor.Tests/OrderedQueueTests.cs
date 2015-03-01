using NUnit.Framework;

namespace Compressor.Tests
{
    [TestFixture]
    public class OrderedQueueTests
    {
        [TestCase]
        public void DequeueFromEmptryQueueTest()
        {
            var orderedQueue = new OrderedQueue<int>();
            int item;
            Assert.IsFalse(orderedQueue.TryDequeue(out item));
        }

        [TestCase]
        public void EnqueueTest()
        {
            var orderedQueue = new OrderedQueue<int>();
            Assert.AreEqual(0, orderedQueue.Count);

            orderedQueue.Enqueue(0, 1);
            Assert.AreEqual(1, orderedQueue.Count);
            orderedQueue.Enqueue(2, 3);
            Assert.AreEqual(2, orderedQueue.Count);
            orderedQueue.Enqueue(1, 2);
            Assert.AreEqual(3, orderedQueue.Count);
        }

        [TestCase()]
        public void SequentalDequeueTest()
        {
            var orderedQueue = new OrderedQueue<int>();
            orderedQueue.Enqueue(0, 1);
            orderedQueue.Enqueue(1, 2);
            orderedQueue.Enqueue(2, 3);

            int item;
            Assert.IsTrue(orderedQueue.TryDequeue(out item));
            Assert.AreEqual(1, item);
            Assert.AreEqual(2, orderedQueue.Count);

            Assert.IsTrue(orderedQueue.TryDequeue(out item));
            Assert.AreEqual(2, item);
            Assert.AreEqual(1, orderedQueue.Count);

            Assert.IsTrue(orderedQueue.TryDequeue(out item));
            Assert.AreEqual(3, item);
            Assert.AreEqual(0, orderedQueue.Count);

            Assert.IsFalse(orderedQueue.TryDequeue(out item));
        }

        [TestCase]
        public void UnsequentalDequeueTest()
        {
            var orderedQueue = new OrderedQueue<int>();

            int item;

            orderedQueue.Enqueue(0, 1);

            orderedQueue.Enqueue(4, 5);
            Assert.IsTrue(orderedQueue.TryDequeue(out item));
            Assert.AreEqual(1, item);
            Assert.IsFalse(orderedQueue.TryDequeue(out item));
            Assert.AreEqual(1, orderedQueue.Count);

            orderedQueue.Enqueue(3, 4);
            Assert.IsFalse(orderedQueue.TryDequeue(out item));

            orderedQueue.Enqueue(1, 2);
            Assert.IsTrue(orderedQueue.TryDequeue(out item));
            Assert.AreEqual(2, item);
            Assert.IsFalse(orderedQueue.TryDequeue(out item));
            Assert.AreEqual(2, orderedQueue.Count);

            orderedQueue.Enqueue(2, 3);

            Assert.IsTrue(orderedQueue.TryDequeue(out item));
            Assert.AreEqual(3, item);
            Assert.AreEqual(2, orderedQueue.Count);

            Assert.IsTrue(orderedQueue.TryDequeue(out item));
            Assert.AreEqual(4, item);
            Assert.AreEqual(1, orderedQueue.Count);

            Assert.IsTrue(orderedQueue.TryDequeue(out item));
            Assert.AreEqual(5, item);
            Assert.AreEqual(0, orderedQueue.Count);
        }

        [TestCase]
        public void UnsequentalDequeueWithSubOrdersTest()
        {
            var orderedQueue = new OrderedQueue<int>();

            int item;

            orderedQueue.Enqueue(0, 0, 1);
            orderedQueue.Enqueue(1, 1, 4);

            Assert.IsTrue(orderedQueue.TryDequeue(out item));
            Assert.AreEqual(1, item);
            Assert.IsFalse(orderedQueue.TryDequeue(out item));
            Assert.AreEqual(1, orderedQueue.Count);

            orderedQueue.Enqueue(1, 2, 5, true);
            Assert.IsFalse(orderedQueue.TryDequeue(out item));

            orderedQueue.Enqueue(1, 0, 3);
            Assert.IsFalse(orderedQueue.TryDequeue(out item));

            orderedQueue.Enqueue(0, 1, 2, true);
            Assert.IsTrue(orderedQueue.TryDequeue(out item));
            Assert.AreEqual(2, item);

            Assert.IsTrue(orderedQueue.TryDequeue(out item));
            Assert.AreEqual(3, item);

            Assert.IsTrue(orderedQueue.TryDequeue(out item));
            Assert.AreEqual(4, item);

            Assert.IsTrue(orderedQueue.TryDequeue(out item));
            Assert.AreEqual(5, item);
        }
    }
}