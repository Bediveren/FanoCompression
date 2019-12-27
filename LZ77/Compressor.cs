using System;
using System.Collections;
using System.Threading.Tasks;
using Nito.Collections;

namespace LZ77
{
    public class Compressor
    {
        private Func<Task<byte?>> mNextWord;
        private Action<BitArray> mWrite;
        private Deque<byte> mPresent;
        private Deque<byte> mHistory;
        private uint mMaxHistory;

        // Source: https://stackoverflow.com/a/20342282/2352507 WiegleyJ 12/3/2013
        private byte Log2_WiegleyJ(uint n)
        {
            byte bits = 0;

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

        public static async Task<Compressor> Create(Func<Task<byte?>> nextWordFunc, Action<BitArray> write, uint historySize,
            uint presentSize)
        {
            Compressor result = new Compressor();
            result.mNextWord = nextWordFunc;
            result.mWrite = write;
            result.mMaxHistory = historySize;
            result.mPresent = new Deque<byte>((int)presentSize);
            for (int i = 0; i < presentSize; i++)
            {
                byte? nextWord = await nextWordFunc();
                if (nextWord == null)
                {
                    result.mPresent.Capacity = i;
                    Console.WriteLine($"truncating present size to {i}");
                    break;
                }
                result.mPresent.AddToBack(nextWord.Value);
            }
            result.mHistory = new Deque<byte>((int)historySize);
            return result;
        }

        private Compressor() { }
    }
}