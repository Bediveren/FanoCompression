using System;
using System.Collections;
using Nito.Collections;

namespace LZ77
{
    public class Compressor
    {
        private Func<byte?> mNextWord;
        private Action<BitArray> mWrite;
        private Deque<byte> mPresent;
        private Deque<byte> mHistory;
        private uint mMaxHistory;

        // Source: https://stackoverflow.com/a/20342282/2352507 WiegleyJ 12/3/2013
        private int Log2_WiegleyJ(uint n)
        {
            int bits = 0;

            if (n > 0xffff)
            {
                n >>= 16;
                bits = 0x10;
            }

            if (n > 0xff)
            {
                n >>= 8;
                bits |= 0x8;
            }

            if (n > 0xf)
            {
                n >>= 4;
                bits |= 0x4;
            }

            if (n > 0x3)
            {
                n >>= 2;
                bits |= 0x2;
            }

            if (n > 0x1)
            {
                bits |= 0x1;
            }
            return bits;
        }

        public Compressor(Func<byte?> nextWordFunc, Action<BitArray> write, uint historySize, uint presentSize)
        {
            mNextWord = nextWordFunc;
            mWrite = write;
            mMaxHistory = historySize;
            mPresent = new Deque<byte>((int) presentSize);
            for (int i = 0; i < presentSize; i++)
            {
                byte? nextWord = nextWordFunc();
                if (nextWord == null)
                {
                    mPresent.Capacity = i;
                    Console.WriteLine($"truncating present size to {i}");
                    break;
                }
                mPresent.AddToBack(nextWord.Value);
            }
            mHistory = new Deque<byte>((int) historySize);
        }
    }
}