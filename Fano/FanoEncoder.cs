using FanoCompression.FanoTree;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FanoCompression
{
    class FanoEncoder
    {
        public void Encode(string fileToEncode, int wordLength, string locationToSave)
        {
            WordReader.LoadFile(fileToEncode, 1024, wordLength);

            //Count probabilities
            Dictionary<Word, int> frequencies = CalculateFrequency();

            List<KeyValuePair<Word, int>> frequencyList = frequencies.ToList();
            frequencyList.Sort((val1, val2) => (val1.Value.CompareTo(val2.Value)));

            Console.WriteLine("Test");
            //Create tree
            TreeNode root = CreateEncodeTree(frequencyList);

            //Encoding table
            var encodingTable = CreateEncodings(root);
            List<KeyValuePair<Word, Word>> encodingTableList = encodingTable.ToList();
            Console.WriteLine("--------------------------------------");
            for (int i = 0; i < encodingTableList.Count; i++)
            {
                for(int k = 0; k < encodingTableList[i].Key.Length; k++)
                {
                    Console.Write(encodingTableList[i].Key[k].Value == false ? 0 : 1);
                }
                Console.Write(" => ");
                for (int k = 0; k < encodingTableList[i].Value.Length; k++)
                {
                    Console.Write(encodingTableList[i].Value[k].Value == false ? 0 : 1);
                }
                Console.WriteLine();
            }
            Console.WriteLine("--------------------------------------");
            string example = "aaa\r\naaa\r\naaa\r\nbbb\r\nbbb";
            var a = Encoding.UTF8.GetBytes("a");
            Console.WriteLine("a: " + (byte)a[0]);
            a = Encoding.UTF8.GetBytes("b");
            Console.WriteLine("b: " + (byte)a[0]);
            a = Encoding.UTF8.GetBytes("\r");
            Console.WriteLine("\\r: " + (byte)a[0]);
            a = Encoding.UTF8.GetBytes("\n");
            Console.WriteLine("\\n: " + (byte)a[0]);
            //Encode 
            Console.WriteLine("Hello!!!!");
            //Create encode tree Sequence
            Bit[] treeSequence = EncodeTree(root);
            Console.WriteLine("Tree encoded!!!!");
            int t = 0;
            int u = 0;
            foreach(Bit bit in treeSequence)
            {
                if (t == 4)
                {
                    //Console.Write(" | ");
                    t = 0;
                }
                t++;
                if (u == 20)
                {
                    //Console.Write("\n");
                    u = 0;
                }
                u++;
                Console.Write(bit.Value == false ? 0 : 1);
            }
            Console.WriteLine();
            //Save it


            // throw new NotImplementedException();
        }

        private Bit[] EncodeTree(TreeNode node)
        {
            List<Bit> code = new List<Bit>();
            FindTreeCode(node, code);
            return code.ToArray();
        }

        private void FindTreeCode(TreeNode node, List<Bit> code)
        {
            if (node.value != null)
            {
                code.Add(new Bit(true));
                //var a = ((Word)node.value).Bits;
                code.AddRange(((Word)node.value).Bits);
                //code.Add(n);
                // encodings.Add((Word)node.value, new Word(code.GetBits(0, length)));
            }
            else
            {
                code.Add(new Bit(false));
                FindTreeCode(node.Left, code);
                FindTreeCode(node.Right, code);
                //FindEncodings(node.Left, encodings, code << 1, length + 1);
                //FindEncodings(node.Right, encodings, code << 1 | 1, length + 1);
            }
        }

        private Dictionary<Word, Word> CreateEncodings(TreeNode node)
        {
            var encodings = new Dictionary<Word, Word>();
            FindEncodings(node, encodings, 0,0);
            return encodings;
        }

        private void FindEncodings(TreeNode node, Dictionary<Word, Word> encodings, int code, int length)
        {
            Console.WriteLine("\n");
            if(node.value != null)
            {
                Word w = (Word) node.value;
                for (int i = 0; i < w.Length; i++)
                {
                    Console.Write(w[i].Value == false ? 0 : 1);
                }
                Console.WriteLine();
                encodings.Add((Word)node.value, new Word(code.GetBits(0, length)));
            }
            else
            {
                FindEncodings(node.Left, encodings, code << 1, length+1);
                FindEncodings(node.Right, encodings, code << 1 | 1, length+1);
            }
        }

        private int FindSplitIndex(List<KeyValuePair<Word, int>> frequencies)
        {
            long rightSum = frequencies[^1].Value;
            long leftSum = 0;

            int leftIndex = 0;
            int rightIndex = 2;
            for (int i = 1; i < frequencies.Count; i++)
            {
                if (rightSum > leftSum)
                {
                    leftSum += frequencies[leftIndex++].Value;
                }
                else
                {
                    rightSum += frequencies[^rightIndex++].Value;
                }
            }
            Console.WriteLine($"Left vs right: {leftSum} vs {rightSum}, split index is: {leftIndex} and rightindex{rightIndex}");
            return leftIndex;
        }
        private TreeNode CreateEncodeTree(List<KeyValuePair<Word, int>> frequencies)
        {
            if(frequencies.Count == 1)
            {
                Word w = frequencies[0].Key;
                for(int i = 0; i < w.Length; i++)
                {
                    Console.Write(w[i].Value == false ? 0 : 1);
                }
                Console.WriteLine();
                return new TreeNode(frequencies[0].Key);
                

            }
            else
            {
                TreeNode branch = new TreeNode();
                int splitIndex = FindSplitIndex(frequencies);

                branch.Left = CreateEncodeTree(frequencies.GetRange(0, splitIndex));
                branch.Right = CreateEncodeTree(frequencies.GetRange(splitIndex, frequencies.Count - splitIndex));

                return branch;
            }

        }
        //fano.encode("file.ex", wordlen, "location.tosave")
        //fano.decode("file.ex", "locationtosave";
        public void Decode()
        {

        }

        Dictionary<Word, int> CalculateFrequency()
        {
            var frequencies = new Dictionary<Word, int>();

            Word currentWord;
            while(WordReader.HasNextWord())
            {
                currentWord = WordReader.NextWord();
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
