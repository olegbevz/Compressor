using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Compressor
{
    public class Decompressor
    {
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
                                compressStream.CopyTo(memoryStream);
                            }

                            uncompressedBuffer = memoryStream.GetBufferWithoutZeroTail();
                        }

                        outputStream.Write(uncompressedBuffer, 0, uncompressedBuffer.Length);
                        outputStream.Flush();

                        // Размер буфера превышает ограничение сборщика мусора 8 Кб, 
                        // необходимо вручную очистить данные буфера из Large Object Heap 
                        GC.Collect();

                        Console.WriteLine(((double)currentBytesCount / (double)totalBytesCount * 100) + " %");
                    }
                }
            }
        }

        public byte[] ReadNextGZipBlock(Stream inputStream)
        {
            byte[] gzipHeader = new byte[] { 31, 139, 8, 0, 0, 0, 0, 0, 4, 0 };

            var buffer = new List<byte>();
            long startPosition = inputStream.Position;

            while (inputStream.Position < inputStream.Length)
            {
                var currentByte = (byte)inputStream.ReadByte();
                buffer.Add(currentByte);

                if (inputStream.Position > startPosition + gzipHeader.Length)
                {
                    bool bufferEndsWithGZipFooter = true;
                    for (int i = 0; i < gzipHeader.Length; i++)
                    {
                        if (buffer[buffer.Count - 1 - i] != gzipHeader[gzipHeader.Length - 1 - i])
                        {
                            bufferEndsWithGZipFooter = false;
                            break;
                        }
                    }

                    if (bufferEndsWithGZipFooter)
                    {
                        inputStream.Position -= gzipHeader.Length;
                        buffer.RemoveRange(buffer.Count - gzipHeader.Length, gzipHeader.Length);
                        break;
                    }
                }
            }
            return buffer.ToArray();
        }
    }
}