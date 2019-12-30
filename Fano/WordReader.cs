using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;

namespace FanoCompression
{
    public struct Bit
    {
        bool _value;
        public bool Value {
            get { return _value; }
            set { _value = value; }
        }

        public Bit(bool value)
        {
            _value = value;
        }

        public static bool operator !=(Bit lhs, Bit rhs)
        {
            return lhs.Value != rhs.Value;
        }
        public static bool operator ==(Bit lhs, Bit rhs)
        {
            return lhs.Value == rhs.Value;
        }

    }
    public struct Word : IEquatable<Word>
    {
        readonly Bit[] letters;

        public int Length
        {
            get { return letters.Length; }
        }
        public Bit[] Bits
        {
            get { return letters; }
        }

        public Bit this[int i]
        {
            get { return letters[i]; }
        }

        public Word(int size)
        {
            letters = new Bit[size];
        }
        public Word(Bit[] letters)
        {
            this.letters = letters;
        }

        public bool Equals([AllowNull] Word other)
        {
            if(letters.Length == other.Length)
            {
                for(int i = 0; i < letters.Length; i++)
                {
                    if (letters[i] != other[i]) return false;
                }
                return true;
            }
            return false;
        }
        public override int GetHashCode()
        {
            string sequence = "";
            foreach (Bit bit in this.letters)
            {
                sequence += (bit.Value.ToString());
            }

            return sequence.GetHashCode();
        }
    }

    public static class WordReader
    {
        static int _bufferSize;
        static int _wordLength;
        static byte[] _buffer;    //Visas buffer

        private static int _bitsRead = 0;

        public static void LoadFile(string filePath, int bufferSize, int wordLength)
        {
            _bufferSize = bufferSize;                                               //TO-DO make reading in chuncks
            _wordLength = wordLength;
            _buffer = File.ReadAllBytes(filePath);
        }

        public static Word NextWord()
        {
            if (_buffer.Length * 8 >= _bitsRead + _wordLength)
            {
                Console.WriteLine($"Byte value! {_buffer[_bitsRead / 8]}");
                Word nextWord = new Word(_buffer[_bitsRead / 8].GetBits(0, _wordLength));
              //  var word = _buffer.GetWord(_bitsRead, _wordLength);
                _bitsRead += _wordLength;
                return nextWord;
            }
            else return new Word(); //TO-DO add missing bits, return null only if end reached fully
        }
        public static bool HasNextWord()
        {
            if (_buffer.Length * 8 >= _bitsRead + _wordLength)
            {
                return true;
            }
            else return false;
        }

        public static void ResetHandle()
        {
            _bitsRead = 0;
        }

        static Bit[] GetBits(this byte source, int offset, int length)
        {
            var bits = new Bit[length];
            for (int i = 0; i < length; i++)
            {
                bits[^(i+1)] = new Bit((source & (1 << ((i+offset)))) != 0);
            }
            return bits;
        }
        public static Bit[] GetBits(this int source, int offset, int length)
        {
            var bits = new Bit[length];
            for (int i = 0; i < length; i++)
            {
                var a = (source & (1 << ((i + offset)))) != 0;
                bits[^(i + 1)] = new Bit((source & (1 << ((i + offset)))) != 0);
            }
            return bits;
        }

    }
}
