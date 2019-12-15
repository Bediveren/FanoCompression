using System;
using System.Collections;
using System.Globalization;
using System.Text;

namespace FanoCompression
{
    class Program
    {

        static void Main(string[] args)
        {
            int wordLength = 8;
            WordReader.LoadFile("../../../sample.txt", 64, wordLength);
            BitArrayExtended word;
            int line = 0;
            string example = "aaa\r\naaa\r\naaa\r\nbbb\r\nbbb";
            while(true)
            {
                line++;
                if ((word = WordReader.NextWord()) == null) break;
                Console.Write(line + ": ");
                for (int i = 0; i < wordLength; i++)
                {
                    Console.Write(word[^(i+1)] ? 1 : 0);
                }

                var a = Encoding.UTF8.GetBytes((example[line - 1]).ToString());
                Console.WriteLine(": " + (byte)a[0]);
            }
            WordReader.ResetHandle();

            var sevenItems = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
            var sevenItems2 = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
            var e = new BitArrayExtended(sevenItems);
            var ax = new BitArrayExtended(sevenItems2);

            Console.WriteLine(e.Equals(ax));
            var fano = new FanoEncoder("../../../sample.txt", wordLength);

        }
    }
}
