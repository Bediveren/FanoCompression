using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;

namespace FanoCompression
{

    public static class WordReader
    {
        static int _bufferSize;
        static int _wordLength;
        static BitArray _buffer;    //Visas buffer

        private static int _bitsRead = 0;

        public static void LoadFile(string filePath, int bufferSize, int wordLength)
        {
            _bufferSize = bufferSize;                                               //TO-DO make reading in chuncks
            _wordLength = wordLength;
            _buffer = new BitArray(File.ReadAllBytes(filePath));
        }

        public static BitArrayExtended NextWord()
        {
            if (_buffer.Length >= _bitsRead + _wordLength)
            {
                var word = _buffer.GetWord(_bitsRead, _wordLength);
                _bitsRead += _wordLength;
                return new BitArrayExtended(word);
            }
            else return null; //TO-DO add missing bits, return null only if end reached fully
        }

        public static void ResetHandle()
        {
            _bitsRead = 0;
        }

        static BitArray GetWord(this BitArray source, int offset, int length)
        {
            BitArray word = new BitArray(length);
            for (int i = 0; i < length; i++)
            {
                word[i] = source[offset + i];
            }

            return word;
        }

    }
}
