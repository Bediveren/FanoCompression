using System.Collections;
using System.IO;

namespace IO
{
    public class BufferedWriter
    {
        private BitArray mBuffer;
        private int mBufferOffset;
        private BitArray mOutputBuffer;
        private Stream mOutputStream;

        public BufferedWriter(int bufferLength, Stream output)
        {
            mBufferOffset = 0;
            mOutputStream = output;
            mBuffer = new BitArray(bufferLength);
            mOutputBuffer = null;
        }

        public void WriteBitArray(BitArray data)
        {
            int copied = 0;
            while (copied < data.Count && mBufferOffset < mBuffer.Count)
            {

            }
        }
    }
}