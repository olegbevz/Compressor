using System;
using System.Diagnostics;
using System.Threading;

namespace Compressor
{
    /// <summary>
    /// ����������� �������.
    /// �����������, ��� ��� ���������� ����� ����� �������� �� ����� ���������� ���������� �������
    /// </summary>
    public class ThreadScheduler
    {
        private const int TIME_INTERVAL = 100;

        /// <summary>
        /// ������� ���������� �������
        /// </summary>
        private int currentThreadsCount = 0;

        public ThreadScheduler(int maxThreads)
        {
            MaxThreads = maxThreads;
        }

        /// <summary>
        /// ������������ ����� �������
        /// </summary>
        public int MaxThreads { get; set; }

        public int CurrentThreadsCount
        {
            get { return currentThreadsCount; }
        }

        /// <summary>
        /// ��������� �������� � ��������� ������.
        /// ���� ���� ���������� ������������ ���������� �������
        /// ����������� ������� ���������� ������ �� ���������� �������.
        /// </summary>
        /// <param name="threadAction">����������� ��������</param>
        public void Enqueue(Action threadAction)
        {
            WaitForAvailableThreads();

            // ��������� �������� � ��������� ������
            var thread = new Thread(ExceuteThread);
            thread.Start(threadAction);

            // ����������� ������� ���������� �������
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

            // ��������� ������� ���������� �������
            Interlocked.Decrement(ref currentThreadsCount);

            Debug.WriteLine(string.Format("Thread disposed. Current threads count: {0}.", currentThreadsCount));
        }
    }
}