using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tinychain
{
    class SyncList
    {
        public List<TinyBlock> blocks;

        public SyncList(List<TinyBlock> list)
        {
            blocks = new List<TinyBlock>();
            blocks.AddRange(list);
        }
    }
}
