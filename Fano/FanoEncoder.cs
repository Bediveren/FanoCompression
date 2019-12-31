using FanoCompression.FanoTree;
using IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanoCompression
{
    class FanoEncoder
    {
        private readonly BufferedReader reader;
        private readonly BufferedWriter writer;
        private readonly int wordLength;


        public FanoEncoder(BufferedReader reader, BufferedWriter writer, int worldLength)
        {
            this.reader = reader;
            this.writer = writer;
            this.wordLength = worldLength;
        }

        public async Task Encode()
        {

            //Calculate word frequencies
            //-------------------------------------------------------------------------
            Dictionary<long?, int> frequencies = await CalculateFrequencyAsync();

            //Create encoding tree
            //-------------------------------------------------------------------------

            //Create sorted list
            List<KeyValuePair<long?, int>> frequencyList = frequencies.ToList();
            frequencyList.Sort((val1, val2) => (val1.Value.CompareTo(val2.Value)));

            //Creating tree
            TreeNode root = await CreateEncodeTree(frequencyList);


            //Create encoding table from tree
            //-------------------------------------------------------------------------
            Dictionary<long?, (long? code, int codeLength)> encodingTable = await CreateEncodingsAsync(root);

            //Encode file
            //-------------------------------------------------------------------------
            /* --Structure--
             * long code length
             * long word length
             * bits tree
             * bits code
             */

            //Compute code length
            long codeLength = 0;
            codeLength = frequencyList.Select(x => encodingTable[x.Key].codeLength * x.Value).Sum();

            //Writing
            await this.writer.WriteCustomLength(codeLength, sizeof(long) * 8);
            await this.writer.WriteCustomLength((long)this.wordLength, sizeof(long) * 8);

            //Writing tree
            await WriteEncodingTreeAsync(root);

            long? currentWord;
            while ((currentWord = await this.reader.ReadCustomLength(this.wordLength)) != null)
            {
                await this.writer.WriteCustomLength((long)encodingTable[currentWord].code, encodingTable[currentWord].codeLength);
            }
            await this.writer.FlushBuffer();
        }

        public async void Decode()
        {
            throw new NotImplementedException();
        }

        async Task<Dictionary<long?, int>> CalculateFrequencyAsync()
        {
            var frequencies = new Dictionary<long?, int>();

            long? currentWord;

            while((currentWord = await this.reader.ReadCustomLength(this.wordLength)) != null)
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

            await this.reader.ResetBufferedReader();
            return frequencies;
        }
        private async Task<TreeNode> CreateEncodeTree(List<KeyValuePair<long?, int>> frequencies)
        {
            if (frequencies.Count == 1)
            {
                long? w = frequencies[0].Key;
                return new TreeNode(frequencies[0].Key);
            }
            else
            {
                TreeNode branch = new TreeNode();
                int splitIndex = FindSplitIndex(frequencies);

                branch.Left = await CreateEncodeTree(frequencies.GetRange(0, splitIndex));
                branch.Right = await CreateEncodeTree(frequencies.GetRange(splitIndex, frequencies.Count - splitIndex));

                return branch;
            }

        }
        private int FindSplitIndex(List<KeyValuePair<long?, int>> frequencies)
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
        private async Task<Dictionary<long?, (long? code, int codeLength)>> CreateEncodingsAsync(TreeNode node)
        {
            //Encoding <Key, (code, code length)>
            var encodings = new Dictionary<long?, (long?, int)>();
            await FindEncodingsAsync(node, encodings, 0, 0);
            return encodings;
        }
        private async Task FindEncodingsAsync(TreeNode node, Dictionary<long?, (long?, int)> encodings, long code, int length)
        {
            if (node.leaf != null)
            {
                long? w = node.leaf;
                encodings.Add(node.leaf, (code, length));
            }
            else
            {
                await FindEncodingsAsync(node.Left, encodings, code << 1, length + 1);
                await FindEncodingsAsync(node.Right, encodings, code << 1 | 1, length + 1);
            }
        }

        private async Task WriteEncodingTreeAsync(TreeNode node)
        {
            if (node.leaf != null)
            {
                Console.Write($"1|{(long)node.leaf }|");
                await this.writer.WriteCustomLength(1, 1);
                await this.writer.WriteCustomLength((long)node.leaf, this.wordLength);
            }
            else
            {
                Console.Write($"0");
                await writer.WriteCustomLength(0, 1);
                await WriteEncodingTreeAsync(node.Left);
                await WriteEncodingTreeAsync(node.Right);
            }
        }
    }
    
}
