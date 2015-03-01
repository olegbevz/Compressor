using System;
using System.Threading;

namespace Compressor
{
    public class ThreadScheduler
    {
        private const int TIME_INTERVAL = 100;

        private int currentThreadsCount = 0;

        public ThreadScheduler(int maxThreads)
        {
            MaxThreads = maxThreads;
        }

        public int MaxThreads { get; set; }

        public int CurrentThreadsCount
        {
            get { return currentThreadsCount; }
        }

        public void Enqueue(Action threadAction)
        {
            if (currentThreadsCount >= MaxThreads)
            {
                while (currentThreadsCount >= MaxThreads)
                {
                    Thread.Sleep(TIME_INTERVAL);
                }
            }

            var thread = new Thread(ExceuteThread);

            thread.Start(threadAction);

            Interlocked.Increment(ref currentThreadsCount);
        }

        private void ExceuteThread(object state)
        {
            var threadAction = state as Action;
            if (threadAction != null)
                threadAction();

            Interlocked.Decrement(ref currentThreadsCount);
        }
    }
}