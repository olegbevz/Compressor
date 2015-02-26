using System.IO;

namespace Compressor
{
    public static class StreamExtensions
    {
        public static long CopyTo(this Stream sourceStream, Stream targetStream)
        {
            byte[] buffer = new byte[1024];
            int bytesRead = 0;
            long totalBytes = 0;
            while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                targetStream.Write(buffer, 0, bytesRead);
                totalBytes += bytesRead;
            }
            return totalBytes;
        }

        public static byte[] GetBufferWithoutZeroTail(this MemoryStream memoryStream)
        {
            memoryStream.Position = 0;

            var buffer = new byte[memoryStream.Length];
            memoryStream.Read(buffer, 0, buffer.Length);
            return buffer;
        }
    }
}