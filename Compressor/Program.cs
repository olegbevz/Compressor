using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Compressor
{
    internal class Program
    {
        private const long BLOCK_SIZE = 10 *1024;

        private static void Main(string[] args)
        {
            string inputPath = @"C:\BevzOD\Video\Stand.Up.36.SATRip.avi";
            string outputPath = @"C:\BevzOD\Video\Stand.Up.36.SATRip.avi.gz";
            string decompessedPath = @"C:\BevzOD\Video\Stand.Up.37.WEB-DLRip2.avi";

            //var singleBuffer = CompressFileToSingleBuffer(inputPath);
            var manyBuffers = CompressFileToManyBuffers(inputPath);
            //var aggregateBuffer = manyBuffers.SelectMany(x => x).ToArray();

            //var allBuffersStartsWithHeader = manyBuffers.All(x => (x[0] == 31 && x[1] == 139 && x[2] == 8));

            //var blockIndexes = new List<int>();

            //for (int i = aggregateBuffer.Length; i < singleBuffer.Length; i++)
            //{
                //if (0 != singleBuffer[i])
                //{
                    //break;
                //}
            //}

            //var allBlockIndexes = blockIndexes.All(x => x % 238 == 0);

            using (var file = File.OpenWrite(outputPath))
            {
                foreach (var manyBuffer in manyBuffers)
                {
                    file.Write(manyBuffer, 0, manyBuffer.Length);
                }
            }

            //Compress(inputPath, outputPath);
            //Decompress(outputPath, decompessedPath);
        }

        private static List<byte[]> CompressFileToManyBuffers(string inputPath)
        {
            var bufferSizes = new List<long>();
            var buffers = new List<byte[]>();

            using (var sourceStream = File.OpenRead(inputPath))
            {

                long totalBytesCount = sourceStream.Length;
                long currentBytesCount = 0;
                while (totalBytesCount > currentBytesCount)
                {
                    var buffer = new byte[BLOCK_SIZE];
                    var bytesRead = sourceStream.Read(buffer, 0, buffer.Length);
                    currentBytesCount += bytesRead;

                    using (var memoryStream = new MemoryStream())
                    {
                        using (var compressStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                        {
                            compressStream.Write(buffer, 0, bytesRead);
                        }

                        var memoryBuffer = new byte[memoryStream.Length];
                        memoryStream.Position = 0;
                        memoryStream.Read(memoryBuffer, 0, memoryBuffer.Length);
                        buffers.Add(memoryBuffer);
                        bufferSizes.Add(memoryStream.Length);
                    }

                    Console.WriteLine(((double)currentBytesCount / (double)totalBytesCount * 100) + " %");
                }
            }
            var totalSize = bufferSizes.Sum();
            return buffers;
        }

        private static void Decompress(string inputPath, string outputPath)
        {
            var buffer = new byte[1024 * 64];

            using (var outputStream = new FileStream(outputPath, FileMode.Create))
            {

                using (var inputStream = File.OpenRead(inputPath))
                {
                    using (var compressStream = new GZipStream(inputStream, CompressionMode.Decompress, true))
                    {
                        CopyTo(compressStream, outputStream);
                    }

                    using (var compressStream = new GZipStream(inputStream, CompressionMode.Decompress, true))
                    {
                        CopyTo(compressStream, outputStream);
                    }
                }
            }
        }

        public static long CopyTo(Stream source, Stream destination)
        {
            byte[] buffer = new byte[1];
            int bytesRead = 0;
            long totalBytes = 0;
            while (totalBytes < (long)(10 * (long)1024 * (long)1024 * (long)1024) /*(bytesRead = source.Read(buffer, 0, buffer.Length)) > 0*/)
            {
                bytesRead = source.Read(buffer, 0, buffer.Length);
                destination.Write(buffer, 0, bytesRead);
                totalBytes += bytesRead;
            }
            return totalBytes;
        }
    }
}
