using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace Compressor
{
    public class Decompressor : CompressionBase
    {
        private readonly byte[] gzipHeader = new byte[] { 31, 139, 8, 0, 0, 0, 0, 0, 4, 0 };

        public void Decompress(string inputPath, string outputPath)
        {
            using (var inputStream = File.OpenRead(inputPath))
            {
                using (var outputStream = File.OpenWrite(outputPath))
                {
                    //var blockIndexes = inputStream.GetBlockIndexes(gzipHeader, 10 * 1024 * 1024);

                    //inputStream.Seek(0, SeekOrigin.Begin);

                    using (var compressStream = new GZipStream(inputStream, CompressionMode.Decompress, false))
                    {
                        compressStream.CopyTo(outputStream, 10 * 1024 * 1024);
                    }

                    return;

                    long totalBytesCount = inputStream.Length;
                    long currentBytesCount = 0;
                    while (totalBytesCount > currentBytesCount)
                    {

                        using (var compressStream = new GZipStream(inputStream, CompressionMode.Decompress, false))
                        {
                            compressStream.CopyTo(outputStream, 10*1024*1024);
                        }

                        //var buffer = inputStream.ReadNextBlock(gzipHeader);

                        //currentBytesCount = inputStream.Position;

                        //byte[] uncompressedBuffer = null;

                        //using (var memoryStream = new MemoryStream())
                        //{
                        //    using (var compressStream = new GZipStream(new MemoryStream(buffer), CompressionMode.Decompress, false))
                        //    {
                        //        compressStream.CopyTo(memoryStream);
                        //    }

                        //    uncompressedBuffer = memoryStream.GetBufferWithoutZeroTail();
                        //}

                        //outputStream.Write(uncompressedBuffer, 0, uncompressedBuffer.Length);
                        //outputStream.Flush();

                        //// Размер буфера превышает ограничение сборщика мусора 8 Кб, 
                        //// необходимо вручную очистить данные буфера из Large Object Heap 
                        //GC.Collect();

                        //Console.WriteLine(((double)currentBytesCount / (double)totalBytesCount * 100) + " %");
                    }
                }
            }
        }

        protected override long[] CalculateBlockIndexes(Stream inputStream)
        {
            return inputStream.GetBlockIndexes(gzipHeader, 10 * 1024 * 1024);
        }

        protected override void TransformStreamBuffer(long streamStartIndex, int blockLength, int blockOrder)
        {
            try
            {
                byte[] uncompressedBuffer = null;

                using (var inputStream = File.OpenRead(inputPath))
                {
                    inputStream.Seek(streamStartIndex, SeekOrigin.Begin);
                    var buffer = new byte[blockLength];
                    var readenBytes = inputStream.Read(buffer, 0, blockLength);

                    Interlocked.Add(ref readenBytesCount, buffer.Length);

                    ReportProgress();

                    using (var memoryStream = new MemoryStream())
                    {
                        using (var compressStream = new GZipStream(new MemoryStream(buffer), CompressionMode.Decompress, true))
                        {
                            compressStream.CopyTo(memoryStream);
                        }

                        uncompressedBuffer = memoryStream.GetBufferWithoutZeroTail();
                    }

                    Interlocked.Increment(ref compressedBuffersCount);

                    ReportProgress();
                }

                bufferQueue.Enqueue(blockOrder, uncompressedBuffer);
            }
            catch (Exception ex)
            {
                innerExceptions.Add(ex);

                Cancel();
            }
        }
    }
}