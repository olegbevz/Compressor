using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Threading;

namespace GZipCompressor
{
    public abstract class CompressionBase : ICompressionUnit
    {
        protected const int DEFAULT_THREADS_COUNT = 5;
        protected const int DEFAULT_QUEUE_MAX_SIZE = 10;
        private const int THREAD_SLEEP_INTERVAL = 100;

        private readonly Thread readInputStreamThread;
        private readonly Thread writeOutputStreamThread;

        protected string inputPath;
        protected string outputPath;
        protected volatile bool cancellationPending;
        protected readonly OrderedQueue<byte[]> bufferQueue;
        protected readonly Semaphore bufferQueueSemaphore;
        protected readonly List<Exception> innerExceptions;
        protected readonly ThreadScheduler threadScheduler;

        protected long inputStreamLength;
        protected long readenBytesCount;
        protected int totalBuffersCount;
        protected int compressedBuffersCount;
        protected int writtenBuffersCount;
        protected long writtenBytesCount;

        protected CompressionBase(
            int threadsCount = DEFAULT_THREADS_COUNT, 
            int maxQueueSize = DEFAULT_QUEUE_MAX_SIZE)
        {
            ThreadsCount = threadsCount;
            MaxQueueSize = maxQueueSize;

            readInputStreamThread = new Thread(ReadInputStream);
            writeOutputStreamThread = new Thread(WriteOutputStream);

            threadScheduler = new ThreadScheduler(threadsCount);

            bufferQueue = new OrderedQueue<byte[]>();
            bufferQueueSemaphore = new Semaphore(maxQueueSize);

            innerExceptions = new List<Exception>();
        }

        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        public event EventHandler<CompletedEventArgs> Completed;

        public int ThreadsCount { get; private set; }

        public int MaxQueueSize { get; private set; }

        public void Execute(string inputPath, string outputPath)
        {
            if (!File.Exists(inputPath))
                throw new FileNotFoundException(inputPath);

            this.inputPath = inputPath;
            this.outputPath = outputPath;

            readInputStreamThread.Start();
            writeOutputStreamThread.Start();
        }

        protected abstract void ReadInputStream();

        private void WriteOutputStream()
        {
            try
            {
                Debug.WriteLine("Write output stream thread was started " + Thread.CurrentThread.ManagedThreadId + " was started.");

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
                                bufferQueueSemaphore.Release();

                                // Записываем массив байтов, полученный из очереди в файл
                                outputStream.Write(buffer, 0, buffer.Length);
                                outputStream.Flush();

                                // Сообщаем обизменении проуента выполнения операции
                                Interlocked.Increment(ref writtenBuffersCount);
                                Interlocked.Add(ref writtenBytesCount, buffer.Length);
                                ReportProgress();

                                // Размер буфера превышает ограничение сборщика мусора 85000 байтов, 
                                // необходимо вручную очистить данные буфера из Large Object Heap
                                GC.Collect();
                            }
                        }

                        Thread.Sleep(THREAD_SLEEP_INTERVAL);
                    }
                }

                // Удаляем выходной файл если была выполнена отмена операции
                DeleteOutputFileIfCanceled();

                // Сообщаем о завершении операции
                ReportCompletion();
            }
            catch (Exception ex)
            {
                innerExceptions.Add(ex);
            }
        }
        
        public void Cancel()
        {
            cancellationPending = true;
        }

        private void DeleteOutputFileIfCanceled()
        {
            if (cancellationPending && File.Exists(outputPath))
                File.Delete(outputPath);
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
                    eventArgs = CompletedEventArgs.Success(inputStreamLength, writtenBytesCount);
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
        /// Расчет текущего процента выполнения операции.
        /// </summary>
        /// <returns>Процент выполнения в виде вещественного числа от 0.0 до 1.0</returns>
        private double CalculateProgress()
        {
            // Сохраняем локальные значения переменных, 
            // на случай если значения будут изменены во время расчета
            long localReadenBytesCount = readenBytesCount,
                localInputStreamLength = inputStreamLength;

            int localTotalBuffersCount = totalBuffersCount,
                localCompressedBuffersCount = compressedBuffersCount,
                localWrittenBuffersCount = writtenBuffersCount;

            // Рассчитываем процент считанных данных из исходного файла
            var readInputStreamPercentage = localInputStreamLength == 0 ? 0 : (double)localReadenBytesCount / localInputStreamLength;

            // Рассчитываем процент преобразованных данных
            var compressionPercentage = localTotalBuffersCount == 0 ? 0 : (double)localCompressedBuffersCount / localTotalBuffersCount;

            // Рассчитываем процент записанных данных в выходной файл 
            var writeOutputStreamPercentage = localTotalBuffersCount == 0 ? 0 : (double)localWrittenBuffersCount / localTotalBuffersCount;

            // Получаем общий процент выполнения операции
            return CalculateProgress(readInputStreamPercentage, compressionPercentage, writeOutputStreamPercentage);
        }

        protected abstract double CalculateProgress(
            double readInputStreamPercentage, 
            double compressionPercentage, 
            double writeOutputStreamPercentage);
    }
}