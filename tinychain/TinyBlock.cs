using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace tinychain
{
    public class TinyBlock
    {
        public int index;
        public byte[] previousHash;
        public DateTime timeStamp;
        public string data;
        public byte[] thisHash;
        public int POW;

        private SHA256 hash = SHA256.Create();

        public TinyBlock(int index, byte[] previousHash, string data, int POW)
        {
            this.index = index;
            this.previousHash = previousHash;
            timeStamp = DateTime.Now;
            this.data = data;
            this.POW = POW;
            thisHash = hash.ComputeHash(Encoding.UTF8.GetBytes(Serialize()));
        }

        public TinyBlock() // Generate the genesisblock.
        {
            this.index = 0;
            previousHash = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 , 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 , 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            timeStamp = DateTime.FromFileTime(0);
            this.data = "GenesisBlock";
            this.POW = 20817345;
            thisHash = hash.ComputeHash(Encoding.UTF8.GetBytes(Serialize()));
        }

        public string Serialize()
        {
            return index.ToString() + ":" + timeStamp.ToFileTime() + ":" + data + ":" + POW + ":" + BitConverter.ToString(previousHash).Replace("-", string.Empty);
        }

        public bool verifyBlock(TinyBlock previousBlock)
        {
            byte[] POWcheck = hash.ComputeHash(Encoding.UTF8.GetBytes(thisHash.ToString() + POW));
            //if(POWcheck[0] == 0 && POWcheck[1] == 0) // Difficuly hardcoded :)
            if(POWcheck[0] == 0 && POWcheck[1] == 0 && POWcheck[2] == 0) // Difficuly hardcoded :)
                if(previousHash.SequenceEqual(previousBlock.thisHash) || index == 0)
                    return true;

            return false;
        }

    }
}
