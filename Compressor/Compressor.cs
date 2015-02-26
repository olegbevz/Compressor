using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Compressor
{
    public class Compressor
    {
        private const long BLOCK_SIZE = 10 * 1024;

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
                        var buffer = new byte[BLOCK_SIZE];
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

                        // Размер буфера превышает ограничение сборщика мусора 8 Кб, 
                        // необходимо вручную очистить данные буфера из Large Object Heap 
                        GC.Collect();

                        Console.WriteLine(((double) currentBytesCount/(double) totalBytesCount*100) + " %");
                    }
                }
            }
        }
    }
}