using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using IO;

namespace LZ77
{
    public class Extractor
    {

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

        public long WordsWritten { get; private set; } = 0;
        public long TotalWords { get; private set; } = 1;

        private ReadDelegate mRead;
        private WriteDelegate mWrite;
        private LinkedList<byte> mHistory;
        private uint mMaxHistory;
        private int mPresentLength;

        public Extractor(ReadDelegate read, WriteDelegate write)
        {
            mRead = read;
            mWrite = write;
            mHistory = new LinkedList<byte>();
        }

        public async Task Extract()
        {
            TotalWords = (await mRead(64)).GetValueOrDefault();
            mMaxHistory = Convert.ToUInt32((await mRead(32)).GetValueOrDefault());
            mPresentLength = Convert.ToInt32((await mRead(8)).GetValueOrDefault());
            int historyLengthInBits = Log2_WiegleyJ(mMaxHistory - 1) + 1;

            while (WordsWritten < TotalWords)
            {
                var type = await mRead(1);
                if(type == null)
                    throw new EndOfStreamException();

                if (type.Value == 1)
                {
                    var word = await mRead(8);
                    if (word == null)
                        throw new EndOfStreamException();
                    await mWrite(word.Value, 8);
                    mHistory.AddLast(Convert.ToByte(word.Value));
                    if (mHistory.Count > mMaxHistory)
                        mHistory.RemoveFirst();

                    WordsWritten++;
                }
                else if (type.Value == 0)
                {
                    var offset = await mRead(historyLengthInBits);
                    var length = await mRead(mPresentLength);
                    if(offset == null || length == null)
                        throw new EndOfStreamException();

                    if(offset >= mHistory.Count)
                        throw new ArgumentOutOfRangeException(nameof(offset));

                    if(offset < length - 1)
                        throw new ArgumentOutOfRangeException(nameof(length));

                    var pointer = mHistory.Last;
                    for (int i = 0; i < offset.Value; i++)
                        pointer = pointer.Previous;
                    for (int i = 0; i < length.Value; i++)
                    {
                        mHistory.AddLast(pointer.Value);
                        if (mHistory.Count > mMaxHistory)
                            mHistory.RemoveFirst();
                        await mWrite(pointer.Value, 8);
                        pointer = pointer.Next;
                    }

                    WordsWritten += length.Value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(type));
                }
            }
        }
    }
}