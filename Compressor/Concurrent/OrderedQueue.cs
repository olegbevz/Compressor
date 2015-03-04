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

        /// <summary>
        /// Размер очереди
        /// </summary>
        public int Size
        {
            get { return queueDictionary.Count; }
        }

        /// <summary>
        /// Занести элемент в очередь
        /// </summary>
        /// <param name="order">Порядковый номер элемента</param>
        /// <param name="item">Добавляемый элемент</param>
        public void Enqueue(int order, T item)
        {
            Enqueue(order, 0, item, true);
        }

        /// <summary>
        /// Занести элемент в очередь
        /// </summary>
        /// <param name="order">Основной порядковый номер элемента</param>
        /// <param name="subOrder">Второстепенный порядковый номер</param>
        /// <param name="item">Добавляемый элемент</param>
        /// <param name="lastSubOrder">
        /// Указанный второстепенный порядковый номер является последним
        /// для основного порядкового номера
        /// </param>
        public void Enqueue(int order, int subOrder, T item, bool lastSubOrder = false)
        {
            lock (innerLock)
            {
                var queueOrder = new QueueOrder(order, subOrder);

                if (queueDictionary.ContainsKey(queueOrder))
                    throw new Exception("Item with the same order already exists in queue.");

                if (order < currentOrder)
                    throw new Exception("Item with the same order already have been in queue and was dequeued.");

                if (order == currentOrder && subOrder < currentSubOrder)
                    throw new Exception("Item with the same order already have been in queue and was dequeued.");

                if (lastSubOrder && subOrderLimits.ContainsKey(order))
                    throw new Exception("Last sub order was already set.");
            
                if (lastSubOrder)
                    subOrderLimits[order] = subOrder;

                queueDictionary.Add(queueOrder, item);

                Debug.WriteLine(string.Format("OrderedQueue: an item with order {0} was enqueued. Current queue size {1}.", queueOrder, Size));
            }
        }

        /// <summary>
        /// Получить элемент из очереди
        /// </summary>
        /// <param name="item">Полученный из очереди элемент</param>
        /// <returns>Признак успешного получения элемента</returns>
        public bool TryDequeue(out T item)
        {
            lock (innerLock)
            {
                var queueOrder = new QueueOrder(currentOrder, currentSubOrder);

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

                    Debug.WriteLine(string.Format("OrderedQueue: an item with order {0} was dequeued. Current queue size {1}.", queueOrder, Size));

                    return true;
                }
            }

            item = default(T);
            return false;
        }

        /// <summary>
        /// Очистка элементов из очереди
        /// </summary>
        public void Clear()
        {
            lock (innerLock)
            {
                currentOrder = 0;
                currentSubOrder = 0;

                subOrderLimits.Clear();
                queueDictionary.Clear();
            }
        }

        /// <summary>
        /// Составной порядковый номер элемента в очереди
        /// </summary>
        private struct QueueOrder
        {
            public QueueOrder(int order, int subOrder = 0)
            {
                Order = order;
                SubOrder = subOrder;
            }

            /// <summary>
            /// Основной порядковый номер
            /// </summary>
            public int Order;

            /// <summary>
            /// Второстепенный порядковый номер
            /// </summary>
            public int SubOrder;

            public override string ToString()
            {
                return string.Format("{0} {1}", Order, SubOrder);
            }
        }
    }
}