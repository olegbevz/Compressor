using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace Compressor
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
                totalBuffersCount = (int)Math.Ceiling((double)inputFileInfo.Length / DEFAULT_BLOCK_SIZE);
                var blockIndexes = Enumerable.Range(0, totalBuffersCount).Select(x => x * DEFAULT_BLOCK_SIZE).ToArray();

                for (int i = 0; i < blockIndexes.Length; i++)
                {
                    if (cancellationPending)
                        break;

                    var blockIndex = i;

                    long blockLength = i < blockIndexes.Length - 1
                        ? blockIndexes[i + 1] - blockIndexes[i]
                        : inputStreamLength - blockIndexes[i];

                    threadScheduler.Enqueue(() => { CompressBlock(blockIndexes[blockIndex], (int)blockLength, blockIndex); });
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

                Interlocked.Increment(ref compressedBuffersCount);
                ReportProgress();

                bufferQueue.Enqueue(blockOrder, compressedBuffer);

                // Размер буфера превышает ограничение сборщика мусора 8 Кб, 
                // необходимо вручную очистить данные буфера из Large Object Heap 
                GC.Collect();
            }
            catch (Exception ex)
            {
                innerExceptions.Add(ex);
                Cancel();
            }
        }
    }
}