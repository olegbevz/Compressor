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
        /// <summary>
        /// Счетчик запущенных потоков
        /// </summary>
        private int currentThreadsCount;

        private readonly Semaphore threadSemaphore;

        public ThreadScheduler(int maxThreads)
        {
            MaxThreads = maxThreads;

            threadSemaphore = new Semaphore(maxThreads);
        }

        /// <summary>
        /// Максимальное число потоков
        /// </summary>
        public int MaxThreads { get; private set; }

        public int CurrentThreadsCount
        {
            get { return currentThreadsCount; }
        }

        /// <summary>
        /// Выполнить действие в отдельном потоке.
        /// Если было достигнуто максимальное количество потоков
        /// планировщик блокирует вызывающий поток и ожидает завершения одного из запущенных потоков.
        /// </summary>
        /// <param name="threadAction">Выполняемое действие</param>
        public void Enqueue(Action threadAction)
        {
            currentThreadsCount = threadSemaphore.Wait();

            // Запускаем действие в отдельном потоке
            var thread = new Thread(ExceuteThread);
            thread.Start(threadAction);

            Debug.WriteLine(string.Format("Thread started. Current threads count: {0}.", currentThreadsCount));
        }

        private void ExceuteThread(object state)
        {
            var threadAction = state as Action;
            if (threadAction != null)
                threadAction();

            currentThreadsCount = threadSemaphore.Release();

            Debug.WriteLine(string.Format("Thread disposed. Current threads count: {0}.", currentThreadsCount));
        }
    }
}