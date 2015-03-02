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
        /// ��������� �� ������� ������, ������� ������������ � ������� GZipStream
        /// � ������ ������� ������� ����� ������.
        /// ���������� ��������� ������������ RFC ��� ������� GZip (https://www.ietf.org/rfc/rfc1952.txt).
        /// </summary>
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

                    // ���� ���� �� ���������� �� ������������ ���������, ������ ����� ��� ������ � ������� ��������� ���������.
                    // � ���� ������ ������� ���� �� ��������� ����� �� �������, ��������� ���������� ������ � ����� ������.
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

                            var nextBlockIndex = inputStream.GetBufferIndex(gzipHeader, 1 * 1024 * 1024);
                            if (nextBlockIndex == -1)
                                break;

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
                        if (bytesRead < DECOMPRESS_BUFFER_SIZE)
                        {
                            Array.Resize(ref buffer, bytesRead);
                        }

                        bufferQueue.Enqueue(blockOrder, bufferNumber, buffer);

                        buffer = new byte[DECOMPRESS_BUFFER_SIZE];

                        Interlocked.Increment(ref totalBuffersCount);
                        Interlocked.Increment(ref compressedBuffersCount);
                        ReportProgress();

                        bufferNumber++;
                    }

                    bufferQueue.SetLastSubOrder(blockOrder, --bufferNumber);
                }
            }
        }
    }
}