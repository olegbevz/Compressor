using System.Threading;

namespace GZipCompressor
{
    /// <summary>
    /// Собственная реализация семафора.
    /// В отличие от System.Threading.Semaphore не вызывает функций ядра Windows.
    /// Ожидание выполняется в цикле.
    /// Таким образом повышается производительность за счет большей загрузки процессора.
    /// </summary>
    public class Semaphore
    {
        private const int THREAD_SLEEP_INTERVAL = 100; 

        private readonly object waitLock = new object();

        /// <summary>
        /// Максимальное количество вхождений
        /// </summary>
        private readonly int maximumCount;

        /// <summary>
        /// Счетчик текущих вхождений вхождений
        /// </summary>
        private int counter;

        public Semaphore(int maximumCount)
        {
            this.maximumCount = maximumCount;
        }

        public int Wait()
        {
            lock (waitLock)
            {
                while (counter >= maximumCount)
                {
                    Thread.Sleep(THREAD_SLEEP_INTERVAL);
                }

                return Interlocked.Increment(ref counter);
            }
        }

        /// <summary>
        /// Уменьшить количество вхождений
        /// </summary>
        /// <returns>Значение счетчика текущих вхождений</returns>
        public int Release()
        {
            return Interlocked.Decrement(ref counter);
        }
    }
}