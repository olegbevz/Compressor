using System.IO;
using NUnit.Framework;

namespace Compressor.Tests
{
    [TestFixture]
    public class StreamExtensionsTests
    {
        [TestCase]
        public void GetNextBufferTests()
        {
            var inputBuffer = new byte[] { 31, 139, 8, 1, 2, 3, 31, 139, 8, 4, 5, 6, 31, 139, 8, 7 };
            var blockHeader = new byte[] { 31, 139, 8 };

            using (var memoryStream = new MemoryStream(inputBuffer))
            {
                var i1 = memoryStream.GetNextBlockIndex(blockHeader);
                var i2 = memoryStream.GetNextBlockIndex(blockHeader);
                var i3 = memoryStream.GetNextBlockIndex(blockHeader);
                var i4 = memoryStream.GetNextBlockIndex(blockHeader);

                //CollectionAssert.AreEqual(new long[] { 0, 6, 12 }, memoryStream.GetBlockIndexes(blockHeader));
            }

            using (var memoryStream = new MemoryStream(inputBuffer))
            {
                var i1 = memoryStream.GetNextBlockIndex(blockHeader);
                var i2 = memoryStream.GetNextBlockIndex(blockHeader);
                var i3 = memoryStream.GetNextBlockIndex(blockHeader);
                var i4 = memoryStream.GetNextBlockIndex(blockHeader);

                //CollectionAssert.AreEqual(new long[] { 0, 6, 12 }, memoryStream.GetBlockIndexes(blockHeader, 5));
            }
        }
    }
}