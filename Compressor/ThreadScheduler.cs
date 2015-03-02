using System;
using System.Diagnostics;
using System.Threading;

namespace GZipCompressor
{
    /// <summary>
    /// Планировщик потоков.
    /// Гарантирует, что для выполнения задач будет запущено не более указанного количества потоков
    /// </summary>
    public class ThreadScheduler
    {
        private const int TIME_INTERVAL = 100;

        /// <summary>
        /// Счетчик запущенных потоков
        /// </summary>
        private int currentThreadsCount = 0;

        public ThreadScheduler(int maxThreads)
        {
            MaxThreads = maxThreads;
        }

        /// <summary>
        /// Максимальное число потоков
        /// </summary>
        public int MaxThreads { get; set; }

        public int CurrentThreadsCount
        {
            get { return currentThreadsCount; }
        }

        /// <summary>
        /// Выполнить действие в отдельном потоке.
        /// Если было достигнуто максимальное количество потоков
        /// планировщик ожидает завершения одного из запущенных потоков.
        /// </summary>
        /// <param name="threadAction">Выполняемое действие</param>
        public void Enqueue(Action threadAction)
        {
            WaitForAvailableThreads();

            // Запускаем действие в отдельном потоке
            var thread = new Thread(ExceuteThread);
            thread.Start(threadAction);

            // Увеличиваем счетчик запущенных потоков
            Interlocked.Increment(ref currentThreadsCount);

            Debug.WriteLine(string.Format("Thread started. Current threads count: {0}.", currentThreadsCount));
        }

        private void WaitForAvailableThreads()
        {
            while (currentThreadsCount >= MaxThreads)
            {
                Thread.Sleep(TIME_INTERVAL);
            }
        }

        private void ExceuteThread(object state)
        {
            var threadAction = state as Action;
            if (threadAction != null)
                threadAction();

            // Уменьшаем счетчик запущенных потоков
            Interlocked.Decrement(ref currentThreadsCount);

            Debug.WriteLine(string.Format("Thread disposed. Current threads count: {0}.", currentThreadsCount));
        }
    }
}