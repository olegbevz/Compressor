using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using ThreadState = System.Threading.ThreadState;

namespace GZipCompressor
{
    public abstract class CompressionBase : ICompressionUnit
    {
        protected const int DEFAULT_THREADS_COUNT = 5;
        protected const int DEFAULT_QUEUE_MAX_SIZE = 10;
        protected const long DEFAULT_BLOCK_SIZE = 10 * 1024 * 1024;
        private const long MIN_BLOCK_SIZE = 1024;
        private const int THREAD_SLEEP_INTERVAL = 100;

        protected string inputPath;
        protected string outputPath;
        protected volatile bool cancellationPending;

        private readonly Thread initialThread;
        private readonly Thread writeOutputStreamThread;
        protected readonly OrderedQueue<byte[]> bufferQueue;
        protected readonly Semaphore bufferQueueSemaphore;
        protected readonly List<Exception> innerExceptions;
        protected readonly ThreadScheduler threadScheduler;
        protected readonly object reportProgressLock = new object();

        // Поля для рассчета процента выполнения задачи
        protected long inputStreamLength;
        protected long readenBytesCount;
        protected int totalBuffersCount;
        protected int compressedBuffersCount;
        protected int writtenBuffersCount;
        protected long writtenBytesCount;

        protected CompressionBase(
            long blockSize = DEFAULT_BLOCK_SIZE,
            int threadsCount = DEFAULT_THREADS_COUNT, 
            int maxQueueSize = DEFAULT_QUEUE_MAX_SIZE)
        {
            if (blockSize < MIN_BLOCK_SIZE)
                throw new ArgumentException("blockSize");

            if (threadsCount <= 0)
                throw new ArgumentException("threadsCount");

            if (maxQueueSize <= 0)
                throw new ArgumentException("maxQueueSize");
            
            BlockSize = blockSize;
            ThreadsCount = threadsCount;
            MaxQueueSize = maxQueueSize;

            initialThread = new Thread(ReadInputStream);
            writeOutputStreamThread = new Thread(WriteOutputStream);

            threadScheduler = new ThreadScheduler(threadsCount);
            bufferQueue = new OrderedQueue<byte[]>();
            bufferQueueSemaphore = new Semaphore(maxQueueSize);
            innerExceptions = new List<Exception>();
        }

        /// <summary>
        /// Событие изменения процента выполнения операции
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// Событие завершения операции
        /// </summary>
        public event EventHandler<CompletedEventArgs> Completed;

        /// <summary>
        /// Размер блока данных, который преобразуется в одном потоке
        /// </summary>
        public long BlockSize { get; private set; }

        /// <summary>
        /// Количество доступных потоков для преобразования блоков данных
        /// </summary>
        public int ThreadsCount { get; private set; }

        /// <summary>
        /// Максимальное количество блоков данных, находящихся в очереди на запись
        /// </summary>
        public int MaxQueueSize { get; private set; }

        public void Execute(string inputPath, string outputPath)
        {
            if (initialThread.ThreadState != ThreadState.Unstarted ||
                writeOutputStreamThread.ThreadState != ThreadState.Unstarted)
                throw new Exception("This instance is already used to execute the operation.");

            if (!File.Exists(inputPath))
                throw new FileNotFoundException(string.Format("File not found: {0}.", inputPath));

            this.inputPath = inputPath;
            this.outputPath = outputPath;
            this.cancellationPending = false;

            initialThread.Start();
            writeOutputStreamThread.Start();
        }

        protected abstract void ReadInputStream();

        private void WriteOutputStream()
        {
            try
            {
                Debug.WriteLine("Write output stream thread was started " + Thread.CurrentThread.ManagedThreadId +
                                " was started.");

                using (var outputStream = File.OpenWrite(outputPath))
                {
                    // Запись считается завершенной, если завершился основной поток, не выполняются отдельные потоки 
                    // по преобразованию блоков данных и в очереди на запись отсутвуют данные
                    while (initialThread.IsAlive || threadScheduler.CurrentThreadsCount > 0 || bufferQueue.Size > 0)
                    {
                        if (cancellationPending)
                            break;

                        if (bufferQueue.Size > 0)
                        {
                            byte[] buffer;
                            if (bufferQueue.TryDequeue(out buffer))
                            {
                                bufferQueueSemaphore.Release();

                                // Записываем массив байтов, полученный из очереди, в файл
                                outputStream.Write(buffer, 0, buffer.Length);
                                outputStream.Flush();

                                // Сообщаем об изменении процента выполнения операции
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
            }
            catch (Exception ex)
            {
                innerExceptions.Add(ex);
            }
            finally
            {
                // Удаляем выходной файл если была выполнена отмена операции
                DeleteOutputFileIfCanceled();

                // Сообщаем о завершении операции
                ReportCompletion();
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

                // Освобождаем данные для последующих операций
                innerExceptions.Clear();
                bufferQueue.Clear();
                bufferQueueSemaphore.ReleaseAll();

                completedHandler.Invoke(this, eventArgs);
            }
        }

        /// <summary>
        /// Сообщить об изменении процента выполнения задачи
        /// </summary>
        protected void ReportProgress()
        {
            var progressChangedHandler = ProgressChanged;
            if (progressChangedHandler != null)
            {
                // Заводим блокировку, чтобы гарантировать последовательный вызов событий
                lock (reportProgressLock)
                {
                    var processPercentage = CalculateProgress();
                    progressChangedHandler.Invoke(this, new ProgressChangedEventArgs(processPercentage));
                }
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