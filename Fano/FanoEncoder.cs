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
    public static class County
    {
        public static int countForBits = 0;
    }

    class FanoEncoder
    {
        private readonly BufferedReader reader;
        private readonly BufferedWriter writer;
        private byte wordLength;


        public FanoEncoder(BufferedReader reader, BufferedWriter writer)
        {
            this.reader = reader;
            this.writer = writer;
        }

        public async Task Encode(byte wordLength)
        {
            this.wordLength = wordLength;
            //Calculate word frequencies
            //-------------------------------------------------------------------------
            Dictionary<long?, long> frequencies = await CalculateFrequencyAsync();

            //Create encoding tree
            //-------------------------------------------------------------------------

            //Create sorted list
            List<KeyValuePair<long?, long>> frequencyList = frequencies.ToList();
            frequencyList.Sort((val1, val2) => (val1.Value.CompareTo(val2.Value)));

            //Creating tree
            TreeNode root = await CreateEncodeTree(frequencyList);


            //Create encoding table from tree
            //-------------------------------------------------------------------------
            Dictionary<long?, (long? code, int codeLength)> encodingTable = await CreateEncodingsAsync(root);

            //Encode file
            //-------------------------------------------------------------------------
            /* --Structure--
             * word length : byte
             * original file length in bytes : long
             * tree : Bit...
             * code : Bit...
             */

            //Compute code length
            //long codeLength = 0;
            //codeLength = frequencyList.Select(x => encodingTable[x.Key].codeLength * x.Value).Sum();

            
            //Possible misscount?
            long originalFileLength = frequencyList.Select(x => x.Value).Sum() * this.wordLength / 8;

            //Writing
            await this.writer.WriteCustomLength((long)this.wordLength, sizeof(byte) * 8);
            await this.writer.WriteCustomLength(originalFileLength, sizeof(long) * 8);
            //await this.writer.WriteCustomLength(codeLength, sizeof(long) * 8);
            
            //Writing tree
            await WriteEncodingTreeAsync(root);

            long? currentWord;
            long bitsWritten = 0;
            while ((currentWord = await this.reader.ReadCustomLength(this.wordLength)) != null)
            {
                bitsWritten += encodingTable[currentWord].codeLength;
                await this.writer.WriteCustomLength((long)encodingTable[currentWord].code, encodingTable[currentWord].codeLength);
            }
            Console.WriteLine($"\nFano tree bits written: {County.countForBits}");
            Console.WriteLine($"Encode bits written: {bitsWritten}");
            Console.WriteLine($"TOTAL bits: {bitsWritten + County.countForBits + sizeof(byte) * 8 + sizeof(long) * 8} ({(bitsWritten + (float) County.countForBits + sizeof(byte) * 8 + sizeof(long) * 8) / 8} bytes)");
            
            await this.writer.FlushBuffer();
        }

        public async Task Decode()
        {
            /* --Structure--
             * word length : byte
             * original file length in bytes : long
             * tree : Bit...
             * code : Bit...
             */

            //Reading word length, original file length
            this.wordLength = (byte) await reader.ReadCustomLength(8);
            Console.WriteLine($"Word length: {wordLength}");
            long originalFileLength = (long) await reader.ReadCustomLength(64);
            Console.WriteLine($"Original file size in bytes: {originalFileLength}");

            //Parsing tree from bits
            TreeNode root = await ParseDecodeTreeAsync();


            //Creating decode table
            Dictionary<long?, (long? code, int codeLength)> encodingTable = await CreateEncodingsAsync(root);

            decimal bytesWritten = 0;
            decimal wordToBytes = ((decimal)this.wordLength) / 8;

            DateTime start = DateTime.Now;
            while(bytesWritten + wordToBytes <= originalFileLength)
            {
               
                TreeNode branch = root;
                while(true)
                {
                    long? currentBit = await reader.ReadCustomLength(1);

                    branch = currentBit == 1 ? branch.Right : branch.Left;
                    if (branch.leaf != null)
                    {
                        //bitsWritten += this.wordLength;
                        bytesWritten += wordToBytes;
                        await this.writer.WriteCustomLength((long)branch.leaf, this.wordLength);
                        break;
                    }
                 
                }
                
            }
            TreeNode branch2 = root;
            var da = (((decimal)originalFileLength) - bytesWritten);
            var remainder = (8 * (((decimal)originalFileLength) - bytesWritten));
            if (remainder > 0)
            {
                while (true)
                {
                    long? currentBit = await reader.ReadCustomLength(1);

                    branch2 = currentBit == 1 ? branch2.Right : branch2.Left;
                    if (branch2.leaf != null)
                    {
                        //bitsWritten += this.wordLength;
                        bytesWritten += wordToBytes;
                        var d = (int)(remainder * this.wordLength);
                        await this.writer.WriteCustomLength((long)branch2.leaf, (int) (remainder));
                        break;
                    }
                }
            }

            TimeSpan timeSpent = DateTime.Now - start;
            Console.WriteLine($"Decoding finished! It took {timeSpent.Minutes}:{timeSpent.Seconds}:{timeSpent.Milliseconds} to do so");
            await this.writer.FlushBuffer();
        }

        private async Task<TreeNode> ParseDecodeTreeAsync()
        {
            TreeNode parent = new TreeNode();
            await ParseTreeBitAsync(parent);
            return parent;
        }
        private async Task<TreeNode> ParseTreeBitAsync(TreeNode parent)
        {
            long? bit = await this.reader.ReadCustomLength(1);

            //Leaf
            if (bit == 1)
            {
                long? word = await this.reader.ReadCustomLength(this.wordLength);
                parent.leaf = word;
                return parent;
            }
            //Branch
            else
            {
                parent.Left = await ParseTreeBitAsync(new TreeNode());
                parent.Right = await ParseTreeBitAsync(new TreeNode());
                return parent;
            }
        }

        async Task<Dictionary<long?, long>> CalculateFrequencyAsync()
        {
            var frequencies = new Dictionary<long?, long>();

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
        private async Task<TreeNode> CreateEncodeTree(List<KeyValuePair<long?, long>> frequencies)
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
        private int FindSplitIndex(List<KeyValuePair<long?, long>> frequencies)
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
                County.countForBits += this.wordLength + 1;
                await this.writer.WriteCustomLength(1, 1);
                await this.writer.WriteCustomLength((long)node.leaf, this.wordLength);
            }
            else
            {
                County.countForBits += 1;
                await writer.WriteCustomLength(0, 1);
                await WriteEncodingTreeAsync(node.Left);
                await WriteEncodingTreeAsync(node.Right);
            }
        }
    }
    
}
