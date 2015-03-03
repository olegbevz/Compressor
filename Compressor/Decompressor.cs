using System;
using System.Collections.Generic;
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

        private const int DECOMPRESS_BUFFER_SIZE = 10 * 1024 * 1024;

        private double lastProgress;

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

                            readenBytesCount = nextBlockIndex;
                            ReportProgress();

                            var localBlockOrder = blockOrder;
                            threadScheduler.Enqueue(() => DecompressBlock(nextBlockIndex, localBlockOrder));

                            blockOrder += 1;
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

        private void DecompressBlock(long streamStartPosition, int blockOrder)
        {
            using (var inputStream = File.OpenRead(inputPath))
            {
                inputStream.Seek(streamStartPosition, SeekOrigin.Begin);

                using (var compressStream = new GZipStream(inputStream, CompressionMode.Decompress, true))
                {
                    int bytesRead;
                    int bufferNumber = 0;
                    byte[] buffer = new byte[DECOMPRESS_BUFFER_SIZE];
                    while ((bytesRead = compressStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (cancellationPending)
                            break;

                        if (bytesRead < DECOMPRESS_BUFFER_SIZE)
                        {
                            Array.Resize(ref buffer, bytesRead);
                        }

                        Interlocked.Increment(ref totalBuffersCount);
                        Interlocked.Increment(ref compressedBuffersCount);
                        ReportProgress();

                        bufferQueue.Enqueue(blockOrder, bufferNumber, buffer);

                        buffer = new byte[DECOMPRESS_BUFFER_SIZE];

                        bufferNumber++;
                    }

                    bufferQueue.SetLastSubOrder(blockOrder, --bufferNumber);
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