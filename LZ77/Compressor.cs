using System;
using System.Collections;

namespace LZ77
{
    public class Compressor
    {
        private Func<byte> mNextWord;
        private Action<BitArray> mWrite;

        public Compressor(Func<byte> nextWordFunc, Action<BitArray> write)
        {
            mNextWord = nextWordFunc;
            mWrite = write;
        }
    }
}