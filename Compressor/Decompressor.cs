using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GZipCompressor
{
    public class Decompressor : CompressionBase
    {
        /// <summary>
        /// Заголовок из массива байтов, который записывается с помощью GZipStream
        /// в начало каждого сжатого блока данных.
        /// Содержимое заголовка соответсвует RFC для формата GZip (https://www.ietf.org/rfc/rfc1952.txt).
        /// </summary>
        private readonly byte[] gzipHeader = new byte[] { 31, 139, 8, 0, 0, 0, 0, 0, 4, 0 };

        private const int READ_INPUT_STREAM_BUFFER_SIZE = 1024 * 1024;

        /// <summary>
        /// Последнее рассчитаноое значение процента выполнения задачи
        /// </summary>
        private double lastProgress;

        public Decompressor(
             long blockSize = DEFAULT_BLOCK_SIZE,
             int threadsCount = DEFAULT_THREADS_COUNT,
             int maxQueueSize = DEFAULT_QUEUE_MAX_SIZE) : base(blockSize, threadsCount, maxQueueSize)
        {
        }

        protected override void ReadInputStream()
        {
            try
            {
                Debug.WriteLine("Read input stream thread with id " + Thread.CurrentThread.ManagedThreadId + " was started .");

                using (var inputStream = File.OpenRead(inputPath))
                {
                    inputStreamLength = inputStream.Length;
                    readenBytesCount = 0;

                    int blockOrder = 0;

                    // Если файл не начинается со стандартного заголовка, значит архив был создан с помощью сторонней программы.
                    // В этом случае разбить файл на отдельные части не удастся, выполняем распаковку архива в одном потоке.
                    if (!inputStream.StartsWith(gzipHeader))
                    {
                        readenBytesCount = inputStreamLength;
                        DecompressBlock(0, 0);
                    }
                    else
                    {
                        while (inputStream.Position < inputStream.Length)
                        {
                            if (cancellationPending)
                                break;

                            var nextBlockIndex = inputStream.GetBufferIndex(gzipHeader, READ_INPUT_STREAM_BUFFER_SIZE);
                            if (nextBlockIndex == -1)
                            {
                                readenBytesCount = inputStreamLength;
                                break;
                            }

                            // Сообщаем об изменении процента выполнения операции
                            readenBytesCount = nextBlockIndex;
                            ReportProgress();

                            var localBlockOrder = blockOrder;
                            threadScheduler.Enqueue(() => DecompressBlock(nextBlockIndex, localBlockOrder));

                            blockOrder += 1;

                            // Размер буфера превышает ограничение сборщика мусора 85000 байтов, 
                            // необходимо вручную очистить данные буфера из Large Object Heap 
                            GC.Collect();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                innerExceptions.Add(ex);

                Cancel();
            }
        }

        /// <summary>
        /// Метод выполняет распаковку (декомпрессию) отдельного блока данных из исходного файла
        /// </summary>
        /// <param name="streamStartPosition">Смещение блока данных от начала файла</param>
        /// <param name="blockOrder">Порядок блока</param>
        private void DecompressBlock(long streamStartPosition, int blockOrder)
        {
            using (var inputStream = File.OpenRead(inputPath))
            {
                inputStream.Seek(streamStartPosition, SeekOrigin.Begin);

                using (var compressStream = new GZipStream(inputStream, CompressionMode.Decompress, true))
                {
                    int bufferNumber = 0;

                    byte[] buffer = new byte[BlockSize];
                    int bytesRead = compressStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead < BlockSize)
                        Array.Resize(ref buffer, bytesRead);

                    byte[] nextBuffer = new byte[BlockSize];
                    while (bytesRead > 0)
                    {
                        if (cancellationPending)
                            break;

                        bytesRead = compressStream.Read(nextBuffer, 0, nextBuffer.Length);

                        if (bytesRead < BlockSize)
                            Array.Resize(ref nextBuffer, bytesRead);

                        bufferQueueSemaphore.Wait();
                        bufferQueue.Enqueue(blockOrder, bufferNumber, buffer, nextBuffer.Length == 0);

                        buffer = nextBuffer;
                        nextBuffer = new byte[BlockSize];

                        bufferNumber++;

                        // Сообщаем об изменении процента выполнения операции
                        Interlocked.Increment(ref totalBuffersCount);
                        Interlocked.Increment(ref compressedBuffersCount);
                        ReportProgress();

                        // Размер буфера превышает ограничение сборщика мусора 85000 байтов, 
                        // необходимо вручную очистить данные буфера из Large Object Heap 
                        GC.Collect();
                    }
                }
            }
        }
        
        protected override double CalculateProgress(
            double readInputStreamPercentage,
            double compressionPercentage,
            double writeOutputStreamPercentage)
        {
            // Получаем общий процент выполнения операции умножая процент выполнения задач
            var progress = readInputStreamPercentage * compressionPercentage * writeOutputStreamPercentage;

            // Для предотвращения "колебания" процента выполнения в обратную сторону
            // сравниваем процент выполнения с последним рассчитанным 
            if (progress > lastProgress)
            {
                lastProgress = progress;
            }
            else
            {
                progress = lastProgress;
            }

            return progress;
        }
    }
}