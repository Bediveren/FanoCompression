using System;
using System.Collections.Generic;
using System.Text;

namespace FanoCompression.FanoTree
{
    class TreeNode
    {
        public TreeNode Left { get; set; }
        public TreeNode Right { get; set; }

        public Word? value;

        public TreeNode(){}
        public TreeNode(Word? value)
        {
            this.value = value;
        }
    }
}
