using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Compressor
{
    /// <summary>
    /// Упорядоченная очередь элементов.
    /// Для каждого элемента указывается порядковый номер в очереди.
    /// Очередь гарантирует, что элементы будут извлечены в порядке, соответствующем номерам элементов, 
    /// независимо от того, в каком порядке элементы добавлялись в очередь. 
    /// </summary>
    /// <typeparam name="T">Тип элементов очереди</typeparam>
    public class OrderedQueue<T> where T : class
    {
        private readonly Dictionary<int, T> innerDictionary 
            = new Dictionary<int, T>();

        private int currentIndex = 0;

        public int Count
        {
            get { return innerDictionary.Count; }
        }

        public int CurrentOrder
        {
            get { return currentIndex; }
        }

        public void Enqueue(int order, T item)
        {
            if (innerDictionary.ContainsKey(order))
                throw new Exception("Item with the same order already exists in queue.");

            if (order < currentIndex)
                throw new Exception("Item with the same order already have been in queue and was dequeued.");

            innerDictionary.Add(order, item);

            Debug.WriteLine(string.Format("OrderedQueue: an item with order {0} was enqueued.", order));
        }

        public bool TryDequeue(out T item)
        {
            if (innerDictionary.TryGetValue(currentIndex, out item))
            {
                Debug.WriteLine(string.Format("OrderedQueue: an item with order {0} was dequeued.", currentIndex));

                innerDictionary.Remove(currentIndex);
                currentIndex++;
                return true;
            }

            return false;
        }
    }
}