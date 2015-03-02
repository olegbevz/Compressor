using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace GZipCompressor
{
    /// <summary>
    /// ”пор¤доченна¤ очередь элементов.
    /// ƒл¤ каждого элемента указываетс¤ пор¤дковый номер в очереди.
    /// ќчередь гарантирует, что элементы будут извлечены в пор¤дке, соответствующем номерам элементов, 
    /// независимо от того, в каком пор¤дке элементы добавл¤лись в очередь. 
    /// </summary>
    /// <typeparam name="T">“ип элементов очереди</typeparam>
    public class OrderedQueue<T>
    {
        private readonly Dictionary<QueueOrder, T> queueDictionary 
            = new Dictionary<QueueOrder, T>();

        private readonly Dictionary<int, int> subOrderLimits 
            = new Dictionary<int, int>(); 

        private int currentOrder;

        private int currentSubOrder;

        private readonly object innerLock = new object();

        public int Size
        {
            get { return queueDictionary.Count; }
        }

        public void SetLastSubOrder(int order, int subOrder)
        {
            if (subOrderLimits.ContainsKey(order))
                throw new Exception("Order already has last sub order");

            subOrderLimits[order] = subOrder;

            if (currentOrder == order && subOrder < currentSubOrder)
            {
                Interlocked.Increment(ref currentOrder);
                currentSubOrder = 0;
            }
        }

        public void Enqueue(int order, T item)
        {
            SetLastSubOrder(order, 0);
            Enqueue(order, 0, item);
        }

        public void Enqueue(int order, int subOrder, T item)
        {
            var queueOrder = new QueueOrder(order, subOrder);

            if (queueDictionary.ContainsKey(queueOrder))
                throw new Exception("Item with the same order already exists in queue.");

            if (order < currentOrder)
                throw new Exception("Item with the same order already have been in queue and was dequeued.");

            if (order == currentOrder && subOrder < currentSubOrder)
                throw new Exception("Item with the same order already have been in queue and was dequeued.");

            lock (innerLock)
            {
                queueDictionary.Add(queueOrder, item);
            }

            Debug.WriteLine(string.Format("OrderedQueue: an item with order {0} {1} was enqueued. Current queue size {2}.", queueOrder.Order, queueOrder.SubOrder, Size));
        }

        public bool TryDequeue(out T item)
        {
            var queueOrder = new QueueOrder(currentOrder, currentSubOrder);

            lock (innerLock)
            {
                if (queueDictionary.TryGetValue(queueOrder, out item))
                {
                    Debug.WriteLine(string.Format("OrderedQueue: an item with order {0} {1} was dequeued. Current queue size {2}.", queueOrder.Order, queueOrder.SubOrder, Size));

                    queueDictionary.Remove(queueOrder);

                    if (subOrderLimits.ContainsKey(currentOrder) && currentSubOrder == subOrderLimits[currentOrder])
                    {
                        Interlocked.Increment(ref currentOrder);
                        currentSubOrder = 0;
                    }
                    else
                    {
                        Interlocked.Increment(ref currentSubOrder);
                    }

                    return true;
                }
            }

            item = default(T);
            return false;
        }

        private struct QueueOrder
        {
            public QueueOrder(int order, int subOrder = 0)
            {
                Order = order;
                SubOrder = subOrder;
            }

            public int Order;
            public int SubOrder;

            public override string ToString()
            {
                return string.Format("{0} {1}", Order, SubOrder);
            }
        }
    }
}