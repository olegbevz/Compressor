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
            readInputStreamThread = new Thread(ReadInputStream);
            writeOutputStreamThread = new Thread(WriteOutputStream);

            BlockSize = DEFAULT_BLOCK_SIZE;
        }

        public long BlockSize { get; set; }

        public void Compress(string inputPath, string outputPath)
        {
            using (var inputStream = File.OpenRead(inputPath))
            {
                using (var outputStream = File.OpenWrite(outputPath))
                {
                    long totalBytesCount = inputStream.Length;
                    long currentBytesCount = 0;

                    while (totalBytesCount > currentBytesCount)
                    {
                        var buffer = new byte[DEFAULT_BLOCK_SIZE];
                        var bytesRead = inputStream.Read(buffer, 0, buffer.Length);
                        currentBytesCount += bytesRead;

                        byte[] compressedBuffer = null;

                        using (var memoryStream = new MemoryStream())
                        {
                            using (var compressStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                            {
                                compressStream.Write(buffer, 0, bytesRead);
                            }

                            compressedBuffer = memoryStream.GetBufferWithoutZeroTail();
                        }

                        outputStream.Write(compressedBuffer, 0, compressedBuffer.Length);
                        outputStream.Flush();

                        // ������ ������ ��������� ����������� �������� ������ 8 ��, 
                        // ���������� ������� �������� ������ ������ �� Large Object Heap 
                        GC.Collect();

                        Console.WriteLine(((double) currentBytesCount/(double) totalBytesCount*100) + " %");
                    }
                }
            }
        }

        protected override long[] CalculateBlockIndexes(Stream inputStream)
        {
            var buffersCount = (int)Math.Ceiling((double)inputStream.Length / DEFAULT_BLOCK_SIZE);

            return Enumerable.Range(0, buffersCount).Select(x => x * DEFAULT_BLOCK_SIZE).ToArray();
        }

        protected override void TransformStreamBuffer(long streamStartIndex, int blockLength, int blockOrder)
        {
            try
            {
                //Debug.WriteLine("Transform readen buffer with id " + Thread.CurrentThread.ManagedThreadId + " was started.");

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
            }
            catch (Exception ex)
            {
                innerExceptions.Add(ex);

                Cancel();
            }
        }
    }
}