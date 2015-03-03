using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace GZipCompressor
{
    /// <summary>
    /// Упорядоченная очередь элементов.
    /// Для каждого элемента указываетс¤ пор¤дковый номер в очереди.
    /// Очередь гарантирует, что элементы будут извлечены в соответствии с порядковыми номерами элементов, 
    /// независимо от того, в каком пор¤дке элементы добавлялись в очередь. 
    /// </summary>
    /// <typeparam name="T">Тип элементов очереди</typeparam>
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

        public void Enqueue(int order, T item)
        {
            Enqueue(order, 0, item, true);
        }

        public void Enqueue(int order, int subOrder, T item, bool lastSubOrder = false)
        {
            var queueOrder = new QueueOrder(order, subOrder);

            if (queueDictionary.ContainsKey(queueOrder))
                throw new Exception("Item with the same order already exists in queue.");

            if (order < currentOrder)
                throw new Exception("Item with the same order already have been in queue and was dequeued.");

            if (order == currentOrder && subOrder < currentSubOrder)
                throw new Exception("Item with the same order already have been in queue and was dequeued.");

            if (lastSubOrder && order == currentOrder && subOrder < currentSubOrder)
                throw new Exception("Item with sith this sub order is not the last.");

            lock (innerLock)
            {
                if (lastSubOrder)
                    subOrderLimits[order] = subOrder;

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

                    Debug.WriteLine(string.Format("OrderedQueue: an item with order {0} {1} was dequeued. Current queue size {2}.", queueOrder.Order, queueOrder.SubOrder, Size));

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