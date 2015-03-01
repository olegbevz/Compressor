using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace Compressor
{
    public class Decompressor : CompressionBase
    {
        private readonly byte[] gzipHeader = new byte[] { 31, 139, 8, 0, 0, 0, 0, 0, 4, 0 };

        private const int DECOMPRESS_BUFFER_SIZE = 1*1024*1024;

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

                    while (inputStream.Position < inputStream.Length)
                    {
                        if (cancellationPending)
                            break;

                        var nextBlockIndex = inputStream.GetNextBlockIndex(gzipHeader);
                        //if (nextBlockIndex == inputStreamLength)
                            //throw new Exception("GZip archive was compressed by another program.");

                        var localBlockOrder = blockOrder;

                        threadScheduler.Enqueue(() => DecompressBlock(nextBlockIndex, localBlockOrder));

                        blockOrder += 1;
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

                using (var memoryStream = new MemoryStream())
                {
                    using (var compressStream = new GZipStream(inputStream, CompressionMode.Decompress, true))
                    {
                        compressStream.CopyTo(memoryStream);

                        var uncompressedBuffer = memoryStream.GetBufferWithoutZeroTail();

                        bufferQueue.Enqueue(blockOrder, uncompressedBuffer);
                    }
                }
            }
        }
    }
}