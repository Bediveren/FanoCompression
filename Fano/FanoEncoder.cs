using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace FanoCompression
{
    class FanoEncoder
    {
        readonly int wordLength;

        public FanoEncoder(string filePath, int wordLength)
        {
            WordReader.LoadFile(filePath, 1024, wordLength);

            //Count probabilities
            var frequencies = CalculateFrequency();

        }

        Dictionary<BitArrayExtended, int> CalculateFrequency()
        {
            var frequencies = new Dictionary<BitArrayExtended, int>();

            BitArrayExtended currentWord;
            while((currentWord = WordReader.NextWord()) != null)
            {
                if (frequencies.ContainsKey(currentWord))
                {
                    frequencies[currentWord] += 1;
                }
                else
                {
                    frequencies.Add(currentWord, 1);
                }
            }
            WordReader.ResetHandle();
            return frequencies;
        }
    }
}
