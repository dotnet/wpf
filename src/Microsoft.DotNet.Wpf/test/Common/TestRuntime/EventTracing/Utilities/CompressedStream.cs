// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Compression;
using System.IO;
using System.Diagnostics;
using Microsoft.Test.EventTracing;

namespace Microsoft.Test.EventTracing.Utilities
{
    /// This is basically a DeflateStream that supports seaking ON READ and does good buffering. To do this
    /// we compress in independent chunks of bufferSize code:CompressedWriteStream.bufferSize. The output stream
    /// looks like the following
    /// 
    /// * DWORD Block Size (units that uncompressed data is compressed), Valid for the whole file
    /// * Block 1
    ///   * DWORD compressed bufferSize (in bytes) of first Chunk
    ///   * compressed Data for first chunk
    /// * Block 2
    ///   * DWORD compressed bufferSize (in bytes) of first Chunk
    ///   * compressed Data for first chunk  
    /// * ...
    /// * Block LAST
    ///   * Negative DWORD compressed bufferSize (in bytes) of first Chunk (indicates last chunk
    ///   * DWORD bufferSize of uncompressed data;
    ///   * compressed Data for last chunk
    /// * BlockTable (array of QWORDS of file offsets to the begining of block 0 through N)
    /// * DWORD number of QWORDS entries in BlockTable. 
    /// * DWORD number of uncompressed bytes in the last block
    /// 
    /// This layout allows the reader to efficiently present an uncompressed view of the stream. 
    /// 
    class CompressedWriteStream : Stream, IDisposable
    {
        public static void CompressFile(string inputFilePath, string compressedFilePath)
        {
            using (Stream compressor = new CompressedWriteStream(compressedFilePath))
                StreamUtilities.CopyFromFile(inputFilePath, compressor);
        }
        public CompressedWriteStream(string filePath) : this(File.Create(filePath)) { }
        public CompressedWriteStream(Stream outputStream) : this(outputStream, DefaultBlockSize, false) { }
        /// <summary>
        ///  Create a compressed stream. If blocksize is less than 1K you are likely to NOT achieve
        ///  good compression. Generally a block size of 8K - 256K is a good range (64K is the default and
        ///  generally there is less than .5% to be gained by making it bigger).    
        /// </summary>
        public CompressedWriteStream(Stream outputStream, int blockSize, bool leaveOpen)
        {
            Debug.Assert(64 <= blockSize && blockSize <= 1024 * 1024 * 4);     // sane values.   
            this.outputStream = outputStream;
            this.blockSize = blockSize;
            this.leaveOpen = leaveOpen;
            outputBuffer = new MemoryStream();
            compressor = new DeflateStream(outputBuffer, CompressionMode.Compress, true);
            compressedBlockSizes = new List<int>();
            WriteSignature(outputStream, Signature);
            WriteInt(outputStream, 1);                                // Version number;
            WriteInt(outputStream, blockSize);
            positionOfFirstBlock = outputStream.Position;
            spaceLeftInBlock = blockSize;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            while (count > spaceLeftInBlock)
            {
                if (spaceLeftInBlock > 0)
                {
                    compressor.Write(buffer, offset, spaceLeftInBlock);
                    offset += spaceLeftInBlock;
                    count -= spaceLeftInBlock;
                    spaceLeftInBlock = 0;
                }
                FlushBlock(false);

                // Set up for the next block, create a new 
                outputBuffer.SetLength(0);
                compressor = new DeflateStream(outputBuffer, CompressionMode.Compress, true);
                spaceLeftInBlock = blockSize;
            }
            compressor.Write(buffer, offset, count);
            spaceLeftInBlock -= count;
        }
        public override void Close()
        {
            int lastBlockByteCount = blockSize - spaceLeftInBlock;
            // Write out the last block
            FlushBlock(true);

            // Write out the table of block sizes (to allow for efficient arbitrary seeking). 
            CompressedWriteStream.LogLine("Writing offset table starting at 0x" + outputStream.Position.ToString("x"));
            long blockOffset = positionOfFirstBlock;
            foreach (int compressedBlockSize in compressedBlockSizes)
            {
                WriteLong(outputStream, blockOffset);
                blockOffset += (compressedBlockSize + 4);       // Add the total bufferSize (with header) of the previous block
            }
            WriteLong(outputStream, blockOffset);

            CompressedWriteStream.LogLine("Writing offset table count " + (compressedBlockSizes.Count + 1) +
                " uncompressed Left = 0x" + lastBlockByteCount.ToString("x"));
            // remember the count of the table. 
            WriteInt(outputStream, compressedBlockSizes.Count + 1);
            // and the number of uncompressed bytes in the last block
            WriteInt(outputStream, lastBlockByteCount);
            if (!leaveOpen)
                outputStream.Close();
        }

        public override bool CanRead { get { return false; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return true; } }

        // This is stored at the begining as ASCII to mark this stream  
        public static readonly string Signature = "!BlockDeflateStream";

        // methods that are purposely not implemented 
        public override long Length { get { throw new NotSupportedException(); } }
        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
        public override void Flush()
        {
            throw new NotSupportedException();
        }

        #region private
        private void FlushBlock(bool lastBlock)
        {
            compressor.Close();
            int compressedBlockSize = (int)outputBuffer.Length;
            Debug.Assert(spaceLeftInBlock == 0 || lastBlock);
            CompressedWriteStream.LogLine("FlushBlock: lastBlock " + lastBlock + " compressedBlockSize = 0x" + compressedBlockSize.ToString("x") +
                " uncompresseSize=0x" + (blockSize - spaceLeftInBlock).ToString("x"));
            CompressedWriteStream.LogLine("Block header placed at filePosition=0x" + outputStream.Position.ToString("x"));
            // Write the block out prepended with its bufferSize
            if (lastBlock)
            {
                WriteInt(outputStream, -compressedBlockSize);
                WriteInt(outputStream, blockSize - spaceLeftInBlock);   // write the uncompressed bufferSize too. 
            }
            else
            {
                compressedBlockSizes.Add(compressedBlockSize);
                WriteInt(outputStream, compressedBlockSize);
            }

            outputStream.Write(outputBuffer.GetBuffer(), 0, compressedBlockSize);
            // TODO remove outputStream.Write(new byte[compressedBlockSize], 0, compressedBlockSize);
            CompressedWriteStream.LogLine("After write, filePosition=0x" + outputStream.Position.ToString("x"));
        }
        static void WriteInt(Stream stream, int number)
        {
            for (int i = 0; i < 4; i++)
            {
                stream.WriteByte((byte)number);
                number >>= 8;
            }
        }
        static void WriteLong(Stream stream, long number)
        {
            for (int i = 0; i < 8; i++)
            {
                stream.WriteByte((byte)number);
                number >>= 8;
            }
        }
        private static void WriteSignature(Stream outputStream, string sig)
        {
            int i = 0;
            while (i < sig.Length)
            {
                outputStream.WriteByte((byte)sig[i]);
                i++;
            }
            // DWORD align it.  
            while (i % 4 != 0)
            {
                outputStream.WriteByte(0);
                i++;
            }
        }
        [Conditional("DEBUG")]
        internal static void LogLine(string line)
        {
            Debugger.Log(1, "Compressor", line + "\r\n");
        }
        void IDisposable.Dispose()
        {
            Close();
        }

        const int DefaultBlockSize = 64 * 1024;

        Stream outputStream;            // Where the compressed bytes end up.
        int blockSize;
        bool leaveOpen;                 // do not close the underlying stream on close.  
        long positionOfFirstBlock;
        List<int> compressedBlockSizes;

        // represents the current position in the file. 
        DeflateStream compressor;       // Feed writes to this. 
        MemoryStream outputBuffer;      // We need to buffer output to determine its bufferSize 
        int spaceLeftInBlock;

        #endregion
    }

    class CompressedReadStream : Stream, IDisposable
    {
        public CompressedReadStream(string filePath) : this(File.OpenRead(filePath), false) { }
        /// <summary>
        /// Reads the stream of bytes written by code:CompressedWriteStream 
        /// </summary>
        CompressedReadStream(Stream compressedData, bool leaveOpen)
        {
            if (!compressedData.CanRead || !compressedData.CanSeek)
                throw new ArgumentException("Stream must be readable and seekable", "compressedData");
            this.compressedData = compressedData;
            this.leaveOpen = leaveOpen;
            ReadSignature(compressedData, CompressedWriteStream.Signature);
            int versionNumber = ReadInt(compressedData);
            if (versionNumber != 1)
                throw new NotSupportedException("Version number Mismatch");
            maxUncompressedBlockSize = ReadInt(compressedData);
            Debug.Assert(64 <= maxUncompressedBlockSize && maxUncompressedBlockSize <= 1024 * 1024 * 4);      // check for sane values. 
            nextCompressedBlockStartPosition = compressedData.Position;
            // uncompressedBlockStartPosition = 0;
            // uncompressedBlockSize = 0;
        }

        public static void DecompressFile(string compressedFilePath, string outputFilePath)
        {
            using (Stream decompressor = new CompressedWriteStream(compressedFilePath))
                StreamUtilities.CopyToFile(decompressor, outputFilePath);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;
            int bytesRead;
            while (count > (uncompressedDataLeft + totalBytesRead))
            {
                bytesRead = 0;
                if (uncompressedDataLeft > 0)
                    bytesRead += decompressor.Read(buffer, offset + totalBytesRead, uncompressedDataLeft);
                totalBytesRead += bytesRead;
                uncompressedDataLeft -= bytesRead;
                if (uncompressedDataLeft == 0)
                {
                    if (lastBlock)
                        return totalBytesRead;

                    FillBlock(uncompressedBlockStartPosition + uncompressedBlockSize, nextCompressedBlockStartPosition);
                }
            }
            bytesRead = decompressor.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
            totalBytesRead += bytesRead;
            uncompressedDataLeft -= bytesRead;
            Debug.Assert(totalBytesRead <= count);
            return totalBytesRead;
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.End)
                offset = offset + Length;
            else if (origin == SeekOrigin.Current)
                offset += Position;

            Position = offset;
            return offset;
        }
        public override long Length
        {
            get
            {
                InitBlockStarts();
                return uncompressedStreamLength;
            }
        }
        public override long Position
        {
            get
            {
                return uncompressedBlockStartPosition + (uncompressedBlockSize - uncompressedDataLeft);
            }
            set
            {
                long relativeOffset = value - Position;
                // Optimization: are we seeking a small forward offset
                if (0 <= relativeOffset && relativeOffset < uncompressedBlockStartPosition + uncompressedDataLeft)
                {
                    Skip((int)relativeOffset);
                    return;
                }

                int blockNumber = (int)(value / maxUncompressedBlockSize);
                long newUncompressedBlockStartPosition = blockNumber * maxUncompressedBlockSize;
                int remainder = (int)(value - newUncompressedBlockStartPosition);

                FillBlock(newUncompressedBlockStartPosition, GetCompressedPositionForBlock(blockNumber));
                Skip(remainder);
            }
        }
        public override void Close()
        {
            if (!leaveOpen)
                compressedData.Close();
            if (decompressor != null)
                decompressor.Close();
            base.Dispose(true);
        }

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return true; } }
        public override bool CanWrite { get { return false; } }

        // Methods purposefully left unimplemented since they apply to writable streams
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
        public override void Flush() { throw new NotSupportedException(); }

        #region private
        void IDisposable.Dispose()
        {
            Close();
        }
        private void ReadSignature(Stream inputStream, string sig)
        {
            int i = 0;
            bool badSig = false;
            while (i < sig.Length)
            {
                if (inputStream.ReadByte() != sig[i])
                    badSig = true;
                i++;
            }
            // DWORD align it.  
            while (i % 4 != 0)
            {
                if (inputStream.ReadByte() != 0)
                    badSig = true;
                i++;
            }
            if (badSig)
                throw new Exception("Stream signature mismatch.  Bad data format.");
        }
        /// <summary>
        /// Initializes the current block to point at the begining of the block that starts at the
        /// uncompressed locatin 'uncompressedBlockStart' which as the cooresponding compressed location
        /// 'compressedBlockStart'.
        /// </summary>
        private void FillBlock(long uncompressedBlockStart, long compressedBlockStart)
        {
            CompressedWriteStream.LogLine("FillBlock: uncompressedBlockStart 0x" + uncompressedBlockStart.ToString("x") +
                " compressedBlockStart 0x" + compressedBlockStart.ToString("x"));
            // Advance the uncompressed position
            uncompressedBlockStartPosition = uncompressedBlockStart;
            // and set the compressed stream to just past this block's data
            compressedData.Position = compressedBlockStart;

            // Read in the next block' bufferSize (both compressed and uncompressed)
            uncompressedBlockSize = maxUncompressedBlockSize;
            int compressedBlockSize = ReadInt(compressedData);
            lastBlock = false;
            if (compressedBlockSize < 0)
            {
                compressedBlockSize = -compressedBlockSize;
                uncompressedBlockSize = ReadInt(compressedData);
                lastBlock = true;
            }
            Debug.Assert(compressedBlockSize <= maxUncompressedBlockSize * 3);       // I have never seen expansion more than 2X
            Debug.Assert(uncompressedBlockSize <= maxUncompressedBlockSize);
            if (decompressor != null)
                decompressor.Close();
            // Get next clump of data. 
            decompressor = new DeflateStream(compressedData, CompressionMode.Decompress, true);

            // Set the uncompressed and compressed data pointers. 
            uncompressedDataLeft = uncompressedBlockSize;
            nextCompressedBlockStartPosition = compressedData.Position + compressedBlockSize;

            CompressedWriteStream.LogLine("FillBlock compressedBlockSize = 0x" + compressedBlockSize.ToString("x") + " lastblock = " + lastBlock);
            CompressedWriteStream.LogLine("FillBlock: DONE: uncompressedDataLeft 0x" + uncompressedDataLeft.ToString("x") +
                " nextCompressedBlockStartPosition 0x" + nextCompressedBlockStartPosition.ToString("x"));
        }

        private long GetCompressedPositionForBlock(int blockNumber)
        {
            InitBlockStarts();

            int blockStartPos = blockNumber * sizeof(long);

            int low =
                 blockStarts[blockStartPos] +
                (blockStarts[blockStartPos + 1] << 8) +
                (blockStarts[blockStartPos + 2] << 16) +
                (blockStarts[blockStartPos + 3] << 24);
            int high =
                 blockStarts[blockStartPos + 4] +
                (blockStarts[blockStartPos + 5] << 8) +
                (blockStarts[blockStartPos + 6] << 16) +
                (blockStarts[blockStartPos + 7] << 24);

            return (long)(uint)low + (((long)high) << 32);
        }
        private bool Skip(int bytesToSkip)
        {
            int readSize = bytesToSkip;
            if (readSize > 1024)
                readSize = 1024;

            byte[] buffer = new byte[readSize];
            do
            {
                int count = Read(buffer, 0, readSize);
                if (count == 0)
                    return false;
                bytesToSkip -= count;

            } while (bytesToSkip > 0);
            return true;
        }
        private static int ReadInt(Stream compressedData)
        {
            int ret = compressedData.ReadByte() +
                (compressedData.ReadByte() << 8) +
                (compressedData.ReadByte() << 16) +
                (compressedData.ReadByte() << 24);
            return ret;
        }
        private void InitBlockStarts()
        {
            if (blockStarts != null)
                return;

            long origPosition = compressedData.Position;
            compressedData.Seek(-8, SeekOrigin.End);
            int numberOfBlocks = ReadInt(compressedData);
            int lastBlockLength = ReadInt(compressedData);
            Debug.Assert(lastBlockLength <= maxUncompressedBlockSize);
            Debug.Assert(numberOfBlocks <= compressedData.Length * 50 / maxUncompressedBlockSize);

            int blockStartLength = numberOfBlocks * sizeof(long);
            blockStarts = new byte[blockStartLength];
            compressedData.Seek(-blockStartLength - 8, SeekOrigin.End);
            compressedData.Read(blockStarts, 0, blockStartLength);
            compressedData.Position = origPosition;

            uncompressedStreamLength = (numberOfBlocks - 1) * (long)maxUncompressedBlockSize + lastBlockLength;
            Debug.Assert(GetCompressedPositionForBlock(0) == 0x1C);         // First block position is just skips past the header. 
        }

        // fields associated with the stream as a whole.
        Stream compressedData;              // The original stream (assumed to be compressed data)
        int maxUncompressedBlockSize;       // The uncompressed bufferSize of all blocks except the last (which might be shorter) 
        byte[] blockStarts;                 // The blockStarts table, which allows random seeking (lazily inited)
        long uncompressedStreamLength;      // total bufferSize of the uncompressed stream
        bool leaveOpen;

        // fields associated with the current position
        long uncompressedBlockStartPosition;    // uncompressed stream position begining of the current block
        long nextCompressedBlockStartPosition;  // compresed stream position for the NEXT block 
        DeflateStream decompressor;         // The real stream2, we create a new one on each block. 
        int uncompressedBlockSize;          // The logical bufferSize of the current uncompressed block.
        int uncompressedDataLeft;           // The number of bytes left in the current uncompressed block. 
        bool lastBlock;                     // True if this is the last block in the compressed stream.  

        #endregion
    }

#if UNIT_TESTS
    public static class CompressedStreamTests
    {
        public static void SizeTest(string inputFilePath)
        {
            Console.WriteLine("In size tests");
            for (int blockSize = 1024; blockSize <= 256 * 1024; blockSize *= 2)
            {
                string compressedFilePath = Path.ChangeExtension(inputFilePath, "." + blockSize.ToString() + ".compressed");
                using (Stream compressor = new CompressedWriteStream(File.Create(compressedFilePath), blockSize, false))
                    StreamUtilities.CopyFromFile(inputFilePath, compressor);

                double percent = 100.0 * (new FileInfo(compressedFilePath).Length) / (new FileInfo(inputFilePath).Length);
                Console.WriteLine("Blocksize " + blockSize.ToString().PadLeft(7) + 
                    " compression " + percent.ToString("f2") + "%. Placed in file " + compressedFilePath);
            }
        }
        public static void Tests()
        {
            string testOrig = "text.orig";
            string testCompressed = "text.compressed";

            for (int fileSize = 1023; fileSize <= 1025; fileSize++)
            {
                CreateDataFile(testOrig, fileSize);
                FileStream origData = File.OpenRead(testOrig);

                // Try writing in various block sizes
                // TODO more sizes?
                for (int i = 1; i < 256; i += 37)
                {
                    CompressedWriteStream compressor = new CompressedWriteStream(File.Create(testCompressed), 64, false);
                    origData.Position = 0;
                    CopyInChunks(origData, compressor, i);
                    compressor.Close();

                    CompressedReadStream decompressor = new CompressedReadStream(testCompressed);
                    origData.Position = 0;
                    Debug.Assert(CompareStreams(origData, decompressor, 1024 * 16));
                    decompressor.Close();
                }

                CompressedReadStream lengthTest = new CompressedReadStream(testCompressed);
                Debug.Assert(lengthTest.Length == origData.Length);
                lengthTest.Close();

                // Try reading back in various seek positions. 
                for (int blockSize = 20; blockSize < 300; blockSize += 47)
                {
                    CompressedReadStream decompressor = new CompressedReadStream(testCompressed);
                    for (int seekPosition = 0; seekPosition <= 1024; seekPosition += 16 * 3)
                        CompareStreams(origData, decompressor, blockSize, seekPosition);
                    decompressor.Close();
                }

                origData.Close();
            }
        }

        private static void CreateDataFile(string name, int length)
        {
            StreamWriter writer = File.CreateText(name);
            Random r = new Random(3454);

            string textLine = "The quick brown fox jumped over the lazy dog";
            int writtenBytes = 0;
            for (; ; )
            {
                if (writtenBytes + textLine.Length + 2 >= length)
                {
                    writer.WriteLine(textLine.Substring(0, length - writtenBytes - 2));
                    break;
                }
                int start = r.Next(textLine.Length - 3);
                int end = r.Next(start + 3, textLine.Length);
                writer.WriteLine(textLine.Substring(start, end - start));
                writtenBytes += (end - start + 2);
            }
            writer.Close();
        }
        private static bool FileCompare(string fileName1, string fileName2)
        {
            using (FileStream stream1 = File.OpenRead(fileName1))
            using (FileStream stream2 = File.OpenRead(fileName2))
                return CompareStreams(stream1, stream2, 1024);
        }
        private static void CompareStreams(Stream stream1, Stream stream2, int blockSize, int startPos)
        {
            stream1.Position = startPos;
            stream2.Position = startPos;
            CompareStreams(stream1, stream2, blockSize);
        }
        private static bool CompareStreams(Stream stream1, Stream stream2, int blockSize)
        {
            byte[] block1 = new byte[blockSize];
            byte[] block2 = new byte[blockSize];
            for (; ; )
            {
                Debug.Assert(stream1.Position == stream2.Position);
                int stream1Count = stream1.Read(block1, 0, blockSize);
                int stream2Count = stream2.Read(block2, 0, blockSize);
                if (stream1Count != stream2Count)
                {
                    Debug.Assert(false);
                    return false;
                }
                if (stream1Count == 0)
                    break;
                if (!ByteCompare(block1, 0, block2, 0, blockSize))
                    return false;
            }
            return true;
        }
        private static bool ByteCompare(byte[] buffer1, int offset1, byte[] buffer2, int offset2, int blockSize)
        {
            for (int i = 0; i < blockSize; i++)
                if (buffer1[i + offset1] != buffer2[i + offset2])
                {
                    Debug.Assert(false);
                    return false;
                }
            return true;
        }
        private static void CopyInChunks(Stream fromStream, Stream toStream, int blockSize)
        {
            byte[] block = new byte[blockSize];

            for (; ; )
            {
                int count = fromStream.Read(block, 0, blockSize);
                if (count == 0)
                    break;
                toStream.Write(block, 0, count);
            }
        }
    }
#endif
}
