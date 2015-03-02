using System.IO;
using NUnit.Framework;

namespace GZipCompressor.Tests
{
    [TestFixture]
    public class StreamExtensionsTests
    {
        private readonly byte[] inputBuffer = new byte[] { 31, 139, 8, 1, 2, 3, 31, 139, 8, 4, 5, 6, 31, 139, 8, 7 };

        [TestCase]
        public void StreamStartsWithTest()
        {
            using (var memoryStream = new MemoryStream(inputBuffer))
            {
                Assert.IsTrue(memoryStream.StartsWith(new byte[] { 31, 139, 8 }));
                Assert.AreEqual(0, memoryStream.Position);
            }
        }

        [TestCase]
        public void StreamNotStartsWithTest()
        {
            using (var memoryStream = new MemoryStream(inputBuffer))
            {
                Assert.IsFalse(memoryStream.StartsWith(new byte[] { 31, 139, 9 }));
                Assert.AreEqual(0, memoryStream.Position);
            }
        }

        [TestCase]
        public void EmptyStreamStartsWithTest()
        {
            using (var memoryStream = new MemoryStream())
            {
                Assert.IsFalse(memoryStream.StartsWith(new byte[] { 31, 139, 8 }));
                Assert.AreEqual(0, memoryStream.Position);
            }
        }

        [TestCase]
        public void GetBufferWithoutZeroTailTest()
        {
            using (var memoryStream = new MemoryStream())
            {
                memoryStream.Write(inputBuffer, 0, inputBuffer.Length);
                var bufferFromStream = memoryStream.GetBufferWithoutZeroTail();

                CollectionAssert.AreEqual(inputBuffer, bufferFromStream);
            }
        }

        [TestCase]
        public void GetBufferWithoutZeroTailFromEmptyStreamTest()
        {
            using (var memoryStream = new MemoryStream())
            {
                var bufferFromStream = memoryStream.GetBufferWithoutZeroTail();

                Assert.AreEqual(0, bufferFromStream.Length);
            }
        }

        [TestCase]
        public void GetBufferIndexTest()
        {
            var blockHeader = new byte[] { 31, 139, 8 };

            using (var memoryStream = new MemoryStream(inputBuffer))
            {
                Assert.AreEqual(0, memoryStream.GetBufferIndex(blockHeader));
                Assert.AreEqual(6, memoryStream.GetBufferIndex(blockHeader));
                Assert.AreEqual(12, memoryStream.GetBufferIndex(blockHeader));
                Assert.AreEqual(-1, memoryStream.GetBufferIndex(blockHeader));
            }
        }

        [TestCase]
        public void GetBufferIndexWithSmallReadBlockSizeTest()
        {
            var blockHeader = new byte[] { 31, 139, 8 };

            using (var memoryStream = new MemoryStream(inputBuffer))
            {
                Assert.AreEqual(0, memoryStream.GetBufferIndex(blockHeader, 5));
                Assert.AreEqual(6, memoryStream.GetBufferIndex(blockHeader, 5));
                Assert.AreEqual(12, memoryStream.GetBufferIndex(blockHeader, 5));
                Assert.AreEqual(-1, memoryStream.GetBufferIndex(blockHeader, 5));
            }
        }
    }
}