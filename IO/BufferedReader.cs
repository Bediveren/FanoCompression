using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IO
{
    public class BufferedReader
    {
        private BitArray mBuffer;
        private BitArray mBackupBuffer;
        private int mBufferOffset;
        private Stream mInputStream;
        private int mBufferLength;
        private bool mStreamEmpty; // false by default
        private readonly SemaphoreSlim mReadSemaphore = new SemaphoreSlim(1);

        public BufferedReader(int bufferLength, Stream input)
        {
            mBufferOffset = 0;
            mInputStream = input;
            mBufferLength = bufferLength;
            ReadBackup();
            GetNextBuffer();
        }

        public BitArray ReadBitArray(int length)
        {
            if (mBuffer == null)
                return null;

            if(length > mBufferLength)
                throw new ArgumentOutOfRangeException();

            var ba = new BitArray(length);
            int copied = 0;
            while (copied < length && mBufferOffset < mBuffer.Count)
            {
                ba.Set(copied, mBuffer.Get(mBufferOffset));
                copied++;
                mBufferOffset++;
            }

            if (copied == length)
                return ba;

            GetNextBuffer();
            if (mBuffer != null)
            {
                while (copied < length && mBufferOffset < mBuffer.Count)
                {
                    ba.Set(copied, mBuffer.Get(mBufferOffset));
                    copied++;
                    mBufferOffset++;
                }

                if (copied == length)
                    return ba;

                // We've consumed the last bit, set buffer to null to avoid unnecessary computations next time someone attempts read.
                mBuffer = null;
            }

            // all options exhausted, fill the remaining part with 0s.
            while (copied < length)
            {
                ba.Set(copied, false);
                copied++;
            }

            return ba;
        }

        private readonly byte[] mSingleByteBuffer = new byte[1];

        public byte? ReadByte()
        {
            var ba = ReadBitArray(8);
            if (ba == null)
                return null;
            ba.CopyTo(mSingleByteBuffer, 0);
            return mSingleByteBuffer[0];
        }

        private readonly short[] mSingleShortBuffer = new short[1];

        public short? ReadShort()
        {
            var ba = ReadBitArray(16);
            if (ba == null)
                return null;
            ba.CopyTo(mSingleShortBuffer, 0);
            return mSingleShortBuffer[0];
        }

        private readonly int[] mSingleIntBuffer = new int[1];

        public int? ReadInt()
        {
            var ba = ReadBitArray(32);
            if (ba == null)
                return null;
            ba.CopyTo(mSingleIntBuffer, 0);
            return mSingleIntBuffer[0];
        }

        private readonly long[] mSingleLongBuffer = new long[1];

        public long? ReadLong()
        {
            var ba = ReadBitArray(64);
            if (ba == null)
                return null;
            ba.CopyTo(mSingleLongBuffer, 0);
            return mSingleLongBuffer[0];
        }

        private void GetNextBuffer()
        {
            mReadSemaphore.Wait();
            mBuffer = mBackupBuffer;
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
                int bytesRead = await mInputStream.ReadAsync(tempBuffer);
                mBackupBuffer = bytesRead == 0 ? null : new BitArray(tempBuffer.Take(bytesRead).ToArray());
                if (bytesRead != mBufferLength)
                    mStreamEmpty = true;
            }
            finally
            {
                mReadSemaphore.Release();
            }
        }
    }
}