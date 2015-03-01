using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace Compressor
{
    public class Decompressor : CompressionBase
    {
        private readonly byte[] gzipHeader = new byte[] { 31, 139, 8, 0, 0, 0, 0, 0, 4, 0 };

        private const int DECOMPRESS_BUFFER_SIZE = 10 * 1024 * 1024;

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
                        DecompressBlock(0, 0);
                    }
                    else
                    {
                        while (inputStream.Position < inputStream.Length)
                        {
                            if (cancellationPending)
                                break;

                            var nextBlockIndex = inputStream.GetNextBlockIndex(gzipHeader);

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
                    do
                    {
                        byte[] buffer = new byte[DECOMPRESS_BUFFER_SIZE];
                        bytesRead = compressStream.Read(buffer, 0, buffer.Length);
                        if (bytesRead == 0)
                            break;

                        if (bytesRead < DECOMPRESS_BUFFER_SIZE)
                        {
                            Array.Resize(ref buffer, bytesRead);
                        }

                        bufferQueue.Enqueue(blockOrder, bufferNumber, buffer);
                        bufferNumber++;
                    } while (bytesRead > 0);

                    bufferQueue.SetLastSubOrder(blockOrder, --bufferNumber);
                }
            }
        }
    }
}