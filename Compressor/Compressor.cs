using System;
using System.Collections.Generic;
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

                            compressedBuffer = GetBufferWithoutZeroTail(memoryStream);
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

        public void Decompress(string inputPath, string outputPath)
        {
            using (var inputStream = File.OpenRead(inputPath))
            {
                using (var outputStream = File.OpenWrite(outputPath))
                {
                    long totalBytesCount = inputStream.Length;
                    long currentBytesCount = 0;
                    while (totalBytesCount > currentBytesCount)
                    {
                        var buffer = ReadNextGZipBlock(inputStream);

                        currentBytesCount = inputStream.Position;

                        byte[] uncompressedBuffer = null;

                        using (var memoryStream = new MemoryStream())
                        {
                            using (var compressStream = new GZipStream(new MemoryStream(buffer), CompressionMode.Decompress, false))
                            {
                                CopyTo(compressStream, memoryStream);
                            }

                            uncompressedBuffer = GetBufferWithoutZeroTail(memoryStream);
                        }

                        outputStream.Write(uncompressedBuffer, 0, uncompressedBuffer.Length);
                        outputStream.Flush();

                        // Размер буфера превышает ограничение сборщика мусора 8 Кб, 
                        // необходимо вручную очистить данные буфера из Large Object Heap 
                        GC.Collect();

                        Console.WriteLine(((double) currentBytesCount/(double) totalBytesCount*100) + " %");
                    }
                }
            }
        }

        public byte[] ReadNextGZipBlock(Stream inputStream)
        {
            byte[] gzipHeader = new byte[] { 31, 139, 8 };

            var buffer = new List<byte>();
            long startPosition = inputStream.Position;

            while (inputStream.Position < inputStream.Length)
            {
                var currentByte = (byte) inputStream.ReadByte();
                buffer.Add(currentByte);

                if (inputStream.Position > startPosition + gzipHeader.Length &&
                    buffer[buffer.Count - 1] == gzipHeader[2] &&
                    buffer[buffer.Count - 2] == gzipHeader[1] &&
                    buffer[buffer.Count - 3] == gzipHeader[0])
                {
                    inputStream.Position -= gzipHeader.Length;
                    buffer.RemoveRange(buffer.Count - gzipHeader.Length, gzipHeader.Length);
                    break;
                }
            }
            return buffer.ToArray();
        }

        public static long CopyTo(Stream source, Stream destination)
        {
            byte[] buffer = new byte[1024];
            int bytesRead = 0;
            long totalBytes = 0;
            while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                destination.Write(buffer, 0, bytesRead);
                totalBytes += bytesRead;
            }
            return totalBytes;
        }

        public byte[] GetBufferWithoutZeroTail(MemoryStream memoryStream)
        {
            memoryStream.Position = 0;

            var buffer = new byte[memoryStream.Length];
            memoryStream.Read(buffer, 0, buffer.Length);
            return buffer;
        }
    }
}