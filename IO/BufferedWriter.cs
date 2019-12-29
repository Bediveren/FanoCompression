using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace IO
{
    public delegate Task WriteDelegate(long data, int length);
    public class BufferedWriter
    {
        private long[] mBuffer;
        private int mBufferOffset;
        private int mBitOffset;
        private long[] mOutputBuffer;
        private Stream mOutputStream;
        private int mBufferLength;
        private readonly SemaphoreSlim mWriteSemaphore = new SemaphoreSlim(1);

        public BufferedWriter(int bufferLength, Stream output)
        {
            mBufferOffset = 0;
            mOutputStream = output;
            // buffer length is given in bytes, long is 8 bytes wide.
            mBufferLength = bufferLength / 8;
            mBuffer = new long[mBufferLength];
            mOutputBuffer = null;
        }

        public async Task WriteCustomLength(long data, int length)
        {
            if (mBitOffset >= 64)
            {
                mBitOffset = 0;
                mBufferOffset++;
            }
            if (mBufferOffset >= mBuffer.Length)
                await GetFreshBuffer();

            int newBitOffset = mBitOffset + length;
            if (newBitOffset <= 64)
            {
                // the entire data word will fit in current byte
                mBitOffset = newBitOffset;
                long bitMask = length == 64 ? -1 : ((1l << length) - 1);
                mBuffer[mBufferOffset] |= (data & bitMask) << (64 - mBitOffset);
            }
            else
            {
                // first, store the part that fits in current byte
                int fits = 64 - mBitOffset;
                int notFits = length - fits;
                long bitMask = fits == 64 ? -1 : ((1 << fits) - 1);
                mBuffer[mBufferOffset] |= (data >> notFits) & bitMask;

                mBufferOffset++;
                mBitOffset = 0;
                await WriteCustomLength(data, notFits);
            }
        }

        private async Task GetFreshBuffer()
        {
            await mWriteSemaphore.WaitAsync();
            try
            {
                mOutputBuffer = mBuffer;
                mBuffer = new long[mBufferLength];
                mBufferOffset = 0;
            }
            finally
            {
                mWriteSemaphore.Release();
            }

            WriteOutput();
        }

        private async void WriteOutput()
        {
            await mWriteSemaphore.WaitAsync();
            try
            {
                byte[] buffer = mOutputBuffer.SelectMany(l => BitConverter.IsLittleEndian ? BitConverter.GetBytes(l).Reverse() : BitConverter.GetBytes(l)).ToArray();
                await mOutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            finally
            {
                mWriteSemaphore.Release();
            }
        }

        public async Task FlushBuffer()
        {
            mOutputBuffer = mBuffer.Take(mBufferOffset + 1).ToArray();
            WriteOutput();
            await mWriteSemaphore.WaitAsync();
            mWriteSemaphore.Release();
        }
    }
}