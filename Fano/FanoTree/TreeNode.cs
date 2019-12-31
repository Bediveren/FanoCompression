using System;
using System.Collections.Generic;
using System.Text;

namespace FanoCompression.FanoTree
{
    class TreeNode
    {
        public TreeNode Left { get; set; }
        public TreeNode Right { get; set; }

        public long? leaf;

        public TreeNode(){}
        public TreeNode(long? leaf)
        {
            this.leaf = leaf;
        }
    }
}
