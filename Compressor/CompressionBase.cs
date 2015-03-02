using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Compressor
{
    public abstract class CompressionBase : ICompressionUnit
    {
        private const int DEFAULT_THREADS_COUNT = 5;
        private const int WRITE_OUTPUT_STREAM_INTERVAL = 100;
        
        protected Thread readInputStreamThread;
        protected Thread writeOutputStreamThread;
        protected string inputPath;
        protected string outputPath;
        protected volatile bool cancellationPending;
        protected readonly OrderedQueue<byte[]> bufferQueue = new OrderedQueue<byte[]>();
        protected readonly List<Exception> innerExceptions = new List<Exception>();
        protected readonly ThreadScheduler threadScheduler = new ThreadScheduler(4);

        protected long inputStreamLength;
        protected long readenBytesCount;
        protected int totalBuffersCount;
        protected int compressedBuffersCount;
        protected int writtenBuffersCount;

        public CompressionBase()
        {
            readInputStreamThread = new Thread(ReadInputStream);
            writeOutputStreamThread = new Thread(WriteOutputStream);

            ThreadsCount = DEFAULT_THREADS_COUNT;
        }

        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        public event EventHandler<CompletedEventArgs> Completed;

        public int ThreadsCount { get; set; }

        public void Execute(string inputPath, string outputPath)
        {
            if (!File.Exists(inputPath))
                throw new FileNotFoundException(inputPath);

            this.inputPath = inputPath;
            this.outputPath = outputPath;

            threadScheduler.MaxThreads = ThreadsCount;

            readInputStreamThread.Start();
            writeOutputStreamThread.Start();
        }

        protected abstract void ReadInputStream();

        private void WriteOutputStream()
        {
            try
            {
                Debug.WriteLine("Write output stream thread was started " + Thread.CurrentThread.ManagedThreadId + " was started .");

                using (var outputStream = File.OpenWrite(outputPath))
                {
                    while (readInputStreamThread.IsAlive || 
                        bufferQueue.Size > 0 || 
                        threadScheduler.CurrentThreadsCount > 0)
                    {
                        if (cancellationPending)
                            break;

                        if (bufferQueue.Size > 0)
                        {
                            byte[] buffer;
                            if (bufferQueue.TryDequeue(out buffer))
                            {
                                outputStream.Write(buffer, 0, buffer.Length);
                                outputStream.Flush();

                                Interlocked.Increment(ref writtenBuffersCount);

                                // ������ ������ ��������� ����������� �������� ������ 8 ��, 
                                // ���������� ������� �������� ������ ������ �� Large Object Heap 
                                GC.Collect();

                                ReportProgress();
                            }
                        }

                        Thread.Sleep(WRITE_OUTPUT_STREAM_INTERVAL);
                    }
                }
            }
            catch (Exception ex)
            {
                innerExceptions.Add(ex);
            }

            ReportCompletion();
        }
        
        public void Cancel()
        {
            cancellationPending = true;
        }

        private void ReportCompletion()
        {
            var completedHandler = Completed;
            if (completedHandler != null)
            {
                CompletedEventArgs eventArgs = null;

                if (innerExceptions.Count > 0)
                {
                    eventArgs = CompletedEventArgs.Fault(innerExceptions);
                }
                else if (cancellationPending)
                {
                    eventArgs = CompletedEventArgs.Cancell();
                }
                else
                {
                    eventArgs = CompletedEventArgs.Success();
                }

                completedHandler.Invoke(this, eventArgs);
            }
        }

        protected void ReportProgress()
        {
            var progressChangedHandler = ProgressChanged;
            if (progressChangedHandler != null)
            {
                var processPercentage = CalculateProgress();
                progressChangedHandler.Invoke(this, new ProgressChangedEventArgs(processPercentage));
            }
        }

        /// <summary>
        /// ������ �������� �������� ���������� ��������.
        /// </summary>
        /// <returns>������� ���������� � ���� ������������� ����� �� 0.0 �� 1.0</returns>
        private double CalculateProgress()
        {
            // ��������� ��������� �������� ����������, 
            // �� ������ ���� �������� ����� �������� �� ����� �������
            long localReadenBytesCount = readenBytesCount,
                localInputStreamLength = inputStreamLength;

            int localTotalBuffersCount = totalBuffersCount,
                localCompressedBuffersCount = compressedBuffersCount,
                localWrittenBuffersCount = writtenBuffersCount;

            // ������������ ������� ��������� ������ �� ��������� �����
            var readInputStreamPercentage = (double)localReadenBytesCount / localInputStreamLength;

            // ������������ ������� ��������������� ������
            var compressionPercentage = (double)localCompressedBuffersCount / localTotalBuffersCount;

            // ������������ ������� ���������� ������ � �������� ���� 
            var writeOutputStreamPercentage = (double)localWrittenBuffersCount / localTotalBuffersCount;

            // �������� ����� ������� ���������� �������� ��� ������� ��������������
            return (readInputStreamPercentage + compressionPercentage + writeOutputStreamPercentage) / 3;
        }
    }
}