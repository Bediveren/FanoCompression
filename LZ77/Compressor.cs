using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IO;
using Nito.Collections;

namespace LZ77
{
    public class Compressor
    {
        public event Action<long> WordsWritten;

        private Func<Task<byte?>> mNextWord;
        private WriteDelegate mWrite;
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

        public static async Task<Compressor> Create(Func<Task<byte?>> nextWordFunc, WriteDelegate write,
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
            Console.WriteLine($"History length in bits: {historyLengthInBits}; present length in bits: {presentLengthInBits}");
            int typeThreshold = (1 + historyLengthInBits + presentLengthInBits) / (1 + sizeof(byte));
            // write file size, most significant part first
            await mWrite(unchecked((long)size), 64);
            // write history size (in bytes)
            await mWrite(mMaxHistory, 32);
            // write present size (in bits)
            await mWrite(presentLengthInBits, 8);

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
                    await mWrite(1, 1);
                    byte word = mPresent.First.Value;
                    mPresent.RemoveFirst();
                    await mWrite(word, 8);
                    mHistory.AddLast(word);

                    WordsWritten?.Invoke(1);
                    nextWord = await mNextWord();
                    if (nextWord != null)
                        mPresent.AddLast(nextWord.Value);
                }
                else
                {
                    // found a matching word
                    await mWrite(0, 1);
                    await mWrite(bestPosition, historyLengthInBits);
                    await mWrite(bestLength, presentLengthInBits);

                    WordsWritten?.Invoke(bestLength);
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