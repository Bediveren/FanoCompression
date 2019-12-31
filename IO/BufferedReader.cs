using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using System.Threading.Tasks;

namespace IO
{
    public delegate Task<long?> ReadDelegate(int length);
    public class BufferedReader
    {
        private long[] mBuffer;
        private long[] mBackupBuffer;
        private int mBufferOffset;
        private int mBitOffset;
        private Stream mInputStream;
        private int mBufferLength; // in BYTES!
        private int mBackupBufferLength;
        private bool mStreamEmpty; // false by default

        private readonly SemaphoreSlim mReadSemaphore = new SemaphoreSlim(1);

        public BufferedReader(int bufferLength, Stream input)
        {
            mBufferOffset = 0;
            mInputStream = input;
            mBufferLength = bufferLength;
            mBuffer = new long[(mBufferLength - 1) / 8 + 1];
            ReadBackup();
            GetNextBuffer().Wait();
        }

        public async Task<long?> ReadCustomLength(int length)
        {
            var bufferWordLength = GetBufferWordLength();

            if (mBitOffset == bufferWordLength)
            {
                mBitOffset = 0;
                mBufferOffset++;
                bufferWordLength = GetBufferWordLength();
            }

            if (mBufferOffset == (mBufferLength - 1) / 8 + 1)
            {
                await GetNextBuffer();
            }

            if (mBuffer == null)
                return null;

            if (length > mBufferLength * 8)
                throw new ArgumentOutOfRangeException();

            int newBitOffset = mBitOffset + length;

            if (newBitOffset <= bufferWordLength)
            {
                // the entire requested word fits in current byte
                mBitOffset = newBitOffset;
                long bitMask = length == 64 ? -1L : ((1L << length) - 1);
                return (mBuffer[mBufferOffset] >> (64 - mBitOffset)) & bitMask;
            }
            else
            {
                // first, store the part that fits in current byte
                int fits = bufferWordLength - mBitOffset;
                int notFits = length - fits;
                long bitMask = fits == 64 ? -1L : ((1L << fits) - 1);
                mBitOffset = 0;
                var firstPart = (mBuffer[mBufferOffset++] & bitMask) << notFits;
                var secondPart = await ReadCustomLength(notFits);
                if (secondPart == null)
                    return null;
                return firstPart | secondPart;
            }
        }

        private int GetBufferWordLength()
        {
            var bufferWordLength = (mStreamEmpty && mBufferOffset == (mBufferLength - 1) / 8) ? (mBufferLength % 8) * 8 : 64;
            if (bufferWordLength == 0)
                bufferWordLength = 64;
            return bufferWordLength;
        }

        public async Task<byte?> ReadByte()
        {
            return (byte?)(await ReadCustomLength(8));
        }

        private async Task GetNextBuffer()
        {
            await mReadSemaphore.WaitAsync();
            mBuffer = mBackupBuffer;
            mBufferLength = mBackupBufferLength;
            mBackupBuffer = null;
            mBufferOffset = 0;
            mReadSemaphore.Release();
            
            if(!mStreamEmpty)
                ReadBackup();
        }

        private async void ReadBackup()
        {
            await mReadSemaphore.WaitAsync();
            try
            {
                byte[] tempBuffer = new byte[mBufferLength];
                int bytesRead = await mInputStream.ReadAsync(tempBuffer, 0, mBufferLength);
                mBackupBuffer = bytesRead == 0
                    ? null
                    : Enumerable.Range(0, (mBufferLength - 1) / 8 + 1).Select(x =>
                        BitConverter.IsLittleEndian
                            ? BitConverter.ToInt64(tempBuffer.Skip(x * 8).Take(8).Reverse().ToArray(), 0)
                            : BitConverter.ToInt64(tempBuffer, x * 8)).ToArray();
                if (bytesRead != mBufferLength)
                    mStreamEmpty = true;
                mBackupBufferLength = bytesRead;
            }
            finally
            {
                mReadSemaphore.Release();
            }
        }
    }
}