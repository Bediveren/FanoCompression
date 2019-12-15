using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text;
using System.Linq;

namespace FanoCompression
{
    public class BitArrayExtended : IEnumerable<bool>, IEquatable<BitArrayExtended>
    {
        private BitArray ba;

        public int Length => ba.Length;


        public bool this[int index] => this.ba[index];

        public BitArrayExtended(byte[] data)
        {
            this.ba = new BitArray(data);
        }
        public BitArrayExtended(BitArray array)
        {
            this.ba = array;
        }

        IEnumerator<bool> IEnumerable<bool>.GetEnumerator()
        {
            foreach (bool bit in this.ba)
            {
                yield return bit;
            }
        }

        public IEnumerator GetEnumerator()
        {
            foreach (bool bit in this.ba)
            {
                yield return bit;
            }
        }


        public bool Equals(BitArrayExtended other)
        {
            if (ba.Length == other.Length)
            {
                for (int i = 0; i < ba.Length; i++)
                {
                    if (ba[i] != other[i]) return false;
                }

                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            string a = "";
            foreach (bool bit in this.ba)
            {
                a +=(bit.ToString());
            }
            
            return a.GetHashCode();
        }
    }
}
