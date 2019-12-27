using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nito.Collections;

namespace LZ77
{
    public class Compressor
    {
        public int WordsWritten { get; private set; } = 0;

        private Func<Task<byte?>> mNextWord;
        private Action<BitArray> mWrite;
        private LinkedList<byte> mPresent;
        private LinkedList<byte> mHistory;
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

        public static async Task<Compressor> Create(Func<Task<byte?>> nextWordFunc, Action<BitArray> write,
            uint historySize,
            uint presentSize)
        {
            Compressor result = new Compressor();
            result.mNextWord = nextWordFunc;
            result.mWrite = write;
            result.mMaxHistory = historySize;
            result.mPresent = new LinkedList<byte>();
            for (int i = 0; i < presentSize; i++)
            {
                byte? nextWord = await nextWordFunc();
                if (nextWord == null)
                {
                    Console.WriteLine($"truncating present size to {i}");
                    break;
                }

                result.mPresent.AddLast(nextWord.Value);
            }

            result.mHistory = new LinkedList<byte>();
            return result;
        }

        private Compressor() { }

        public async Task Compress(ulong size)
        {
            byte historyLengthInBits = (byte) (Log2_WiegleyJ(mMaxHistory - 1) + 1);
            byte presentLengthInBits = (byte) (Log2_WiegleyJ((uint) mPresent.Count) + 1);
            int typeThreshold = (1 + historyLengthInBits + presentLengthInBits) / (1 + 8);
            // write file size, most significant part first
            mWrite(new BitArray(new[]
            {
                (int) (size >> 32), // most significant digit (32bit)
                (int) (size & 0xffffffff) // least significant digit (32bit)
            }));
            // write history size (in bytes)
            mWrite(new BitArray(new[] { unchecked((int) mMaxHistory) }));
            // write present size (in bits)
            mWrite(new BitArray(new[] { presentLengthInBits }));

            byte? nextWord;
            while (mPresent.Count > 0)
            {
                int bestPosition = 0;
                int bestLength = 0;
                var historyStart = mHistory.Last;
                int currentPosition = 0;
                while (historyStart != null)
                {
                    var present = mPresent.First;
                    var history = historyStart;
                    if (history.Value == present.Value)
                    {
                        int maxPossibleLength = Math.Min(mPresent.Count, currentPosition + 1);
                        int currentLength = 1;
                        history = history.Next;
                        present = present.Next;
                        while (currentLength < maxPossibleLength && history.Value == present.Value)
                        {
                            currentLength++;
                            history = history.Next;
                            present = present.Next;
                        }

                        if (currentLength > bestLength)
                        {
                            bestLength = currentLength;
                            bestPosition = currentPosition;
                        }
                    }

                    currentPosition++;
                    historyStart = historyStart.Previous;
                }

                if (bestLength <= typeThreshold)
                {
                    // couldn't find matching word
                    mWrite(new BitArray(new[] { true }));
                    byte word = mPresent.First.Value;
                    mPresent.RemoveFirst();
                    mWrite(new BitArray(new[] { word }));
                    mHistory.AddLast(word);

                    WordsWritten++;
                    nextWord = await mNextWord();
                    if (nextWord != null)
                        mPresent.AddLast(nextWord.Value);
                }
                else
                {
                    // found a matching word
                    mWrite(new BitArray(new[] { false }));
                    var position = new BitArray(new[] { bestPosition }) { Length = historyLengthInBits };
                    mWrite(position);
                    var length = new BitArray(new[] { bestLength }) { Length = presentLengthInBits };
                    mWrite(length);

                    WordsWritten += bestLength;
                    for (int i = 0; i < bestLength; i++)
                    {
                        byte word = mPresent.First.Value;
                        mPresent.RemoveFirst();
                        mHistory.AddLast(word);
                        nextWord = await mNextWord();
                        if (nextWord != null)
                            mPresent.AddLast(nextWord.Value);
                    }
                }

                while (mHistory.Count > mMaxHistory)
                    mHistory.RemoveFirst();
            }
        }
    }
}