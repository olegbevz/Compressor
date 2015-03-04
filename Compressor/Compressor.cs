using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GZipCompressor
{
    public class Compressor : CompressionBase
    {
        public Compressor(
            long blockSize = DEFAULT_BLOCK_SIZE, 
            int threadsCount = DEFAULT_THREADS_COUNT,
            int maxQueueSize = DEFAULT_QUEUE_MAX_SIZE) : base(blockSize, threadsCount, maxQueueSize)
        {
        }

        protected override void ReadInputStream()
        {
            try
            {
                Debug.WriteLine("Read input stream thread with id " + Thread.CurrentThread.ManagedThreadId + " was started.");
                
                var inputFileInfo = new FileInfo(inputPath);
                inputStreamLength = inputFileInfo.Length;
                totalBuffersCount = (int)Math.Ceiling((double)inputFileInfo.Length / BlockSize);

                for (int i = 0; i < totalBuffersCount; i++)
                {
                    if (cancellationPending)
                        break;

                    int blockIndex = i;
                    long currentPosition = i * BlockSize;
                    long blockLength = Math.Min(BlockSize, inputStreamLength - currentPosition);

                    threadScheduler.Enqueue(() => { CompressBlock(currentPosition, (int)blockLength, blockIndex); });
                }
            }
            catch (Exception ex)
            {
                innerExceptions.Add(ex);
                Cancel();
            }
        }

        /// <summary>
        /// Метод выполняет сжатие отдельного блока данных из исходного файла
        /// </summary>
        /// <param name="streamStartPosition">Смещение блока данных от начала файла</param>
        /// <param name="blockLength">Длина блока</param>
        /// <param name="blockOrder">Порядок блока</param>
        protected void CompressBlock(long streamStartPosition, int blockLength, int blockOrder)
        {
            try
            {
                // Считываем массив байтов из исходного файла
                byte[] readenBuffer = new byte[blockLength];
                using (var inputStream = File.OpenRead(inputPath))
                {
                    inputStream.Seek(streamStartPosition, SeekOrigin.Begin);
                    inputStream.Read(readenBuffer, 0, blockLength);
                }

                // Сообщаем об изменении процента выполнения операции
                Interlocked.Add(ref readenBytesCount, readenBuffer.Length);
                ReportProgress();

                // Сжимаем исходный массив байтов  
                byte[] compressedBuffer;
                using (var memoryStream = new MemoryStream())
                {
                    using (var compressStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                    {
                        compressStream.Write(readenBuffer, 0, readenBuffer.Length);
                    }

                    compressedBuffer = memoryStream.GetBufferWithoutZeroTail();
                }

                // Заносим сжатый массив байтов в очередь на запись
                bufferQueueSemaphore.Wait();
                bufferQueue.Enqueue(blockOrder, compressedBuffer);

                // Сообщаем об изменении процента выполнения операции
                Interlocked.Increment(ref compressedBuffersCount);
                ReportProgress();

                // Размер буфера превышает ограничение сборщика мусора 85000 байтов, 
                // необходимо вручную очистить данные буфера из Large Object Heap 
                GC.Collect();
            }
            catch (Exception ex)
            {
                innerExceptions.Add(ex);
                Cancel();
            }
        }

        protected override double CalculateProgress(
           double readInputStreamPercentage,
           double compressionPercentage,
           double writeOutputStreamPercentage)
        {
            // Получаем общий процент выполнения операции как среднее арифметическое
            return (readInputStreamPercentage + compressionPercentage + writeOutputStreamPercentage) / 3;
        }
    }
}