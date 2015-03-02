using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GZipCompressor
{
    public class Compressor : CompressionBase
    {
        private const long DEFAULT_BLOCK_SIZE = 10 * 1024 * 1024;

        public Compressor()
        {
            BlockSize = DEFAULT_BLOCK_SIZE;
        }

        public long BlockSize { get; set; }

        protected override void ReadInputStream()
        {
            try
            {
                Debug.WriteLine("Read input stream thread with id " + Thread.CurrentThread.ManagedThreadId + " was started .");
                
                var inputFileInfo = new FileInfo(inputPath);
                inputStreamLength = inputFileInfo.Length;
                readenBytesCount = 0;
                totalBuffersCount = (int)Math.Ceiling((double)inputFileInfo.Length / BlockSize);

                for (int i = 0; i < totalBuffersCount; i++)
                {
                    if (cancellationPending)
                        break;

                    int blockIndex = i;
                    long currentPosition = i * BlockSize;
                    long blockLength = Math.Min(BlockSize, inputStreamLength - currentPosition);

                    while (bufferQueue.Size > ThreadsCount)
                    {
                        Thread.Sleep(100);
                    }

                    threadScheduler.Enqueue(() => { CompressBlock(currentPosition, (int)blockLength, blockIndex); });
                }
            }
            catch (Exception ex)
            {
                innerExceptions.Add(ex);
                Cancel();
            }
        }

        protected void CompressBlock(long streamStartIndex, int blockLength, int blockOrder)
        {
            try
            {
                byte[] readenBuffer = new byte[blockLength];

                using (var inputStream = File.OpenRead(inputPath))
                {
                    inputStream.Seek(streamStartIndex, SeekOrigin.Begin);
                    inputStream.Read(readenBuffer, 0, blockLength);
                }

                Interlocked.Add(ref readenBytesCount, readenBuffer.Length);
                ReportProgress();

                byte[] compressedBuffer;

                using (var memoryStream = new MemoryStream())
                {
                    using (var compressStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                    {
                        compressStream.Write(readenBuffer, 0, readenBuffer.Length);
                    }

                    compressedBuffer = memoryStream.GetBufferWithoutZeroTail();
                }

                // Размер буфера превышает ограничение сборщика мусора 8 Кб, 
                // необходимо вручную очистить данные буфера из Large Object Heap 
                GC.Collect();

                bufferQueue.Enqueue(blockOrder, compressedBuffer);

                Interlocked.Increment(ref compressedBuffersCount);
                ReportProgress();
            }
            catch (Exception ex)
            {
                innerExceptions.Add(ex);
                Cancel();
            }
        }
    }
}