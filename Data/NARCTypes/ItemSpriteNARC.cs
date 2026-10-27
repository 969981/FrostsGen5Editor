using NewEditor.Forms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace NewEditor.Data.NARCTypes
{
    public class ItemSpriteNARC : NARC
    {
        public List<ItemSpriteEntry> files;

        public override void ReadData()
        {
            base.ReadData();

            int pos = pointerStartAddress;
            int initialPosition = FileEntryStart;

            //Register data files
            files = new List<ItemSpriteEntry>();

            //Populate data types
            for (int i = 0; i < numFileEntries; i++)
            {
                int start = HelperFunctions.ReadInt(byteData, pos);
                int end = HelperFunctions.ReadInt(byteData, pos + 4);
                byte[] bytes = new byte[end - start];

                for (int j = 0; j < end - start; j++) bytes[j] = byteData[initialPosition + start + j];

                ItemSpriteEntry m = new ItemSpriteEntry(bytes);
                files.Add(m);

                pos += 8;
            }
        }

        public override void WriteData()
        {
            List<byte> newByteData = new List<byte>();
            List<byte> oldByteData = new List<byte>(byteData);

            newByteData.AddRange(oldByteData.GetRange(0, pointerStartAddress));
            newByteData.AddRange(oldByteData.GetRange(BTNFPosition, FileEntryStart - BTNFPosition));

            //Write Files
            int totalSize = 0;
            int pPos = pointerStartAddress;
            foreach (ItemSpriteEntry i in files)
            {
                newByteData.InsertRange(pPos, BitConverter.GetBytes(totalSize));
                pPos += 4;
                totalSize += i.bytes.Length;
                newByteData.InsertRange(pPos, BitConverter.GetBytes(totalSize));
                pPos += 4;
            }
            foreach (ItemSpriteEntry i in files)
            {
                newByteData.AddRange(i.bytes);
            }

            byteData = newByteData.ToArray();

            FixHeaders(files.Count);

            base.WriteData();
        }

        public override byte[] GetPatchBytes(NARC narc)
        {
            ItemDataNARC other = narc as ItemDataNARC;
            List<byte> bytes = new List<byte>();

            for (int i = 0; i < numFileEntries; i++)
            {
                if (i > other.items.Count || !files[i].bytes.SequenceEqual(other.items[i].bytes))
                {
                    bytes.AddRange(BitConverter.GetBytes(i));
                    bytes.AddRange(BitConverter.GetBytes(files[i].bytes.Length));
                    bytes.AddRange(files[i].bytes);
                }
            }

            return bytes.ToArray();
        }

        public override void ReadPatchBytes(byte[] bytes)
        {
            int pos = 0;
            while (pos < bytes.Length)
            {
                int id = HelperFunctions.ReadInt(bytes, pos);
                int size = HelperFunctions.ReadInt(bytes, pos + 4);
                pos += 8;

                if (id > files.Count)
                {
                    //Don't accept extra files here
                }
                else files[id].bytes = new List<byte>(bytes).GetRange(pos, size).ToArray();
                pos += size;
            }
        }

        static Dictionary<string, int> assignmentArray = new Dictionary<string, int>()
        {
            {"b2", 0x8CD50 },
            {"w2", 0x8CD7C },
            {"b", 0x9AB6C },
            {"w", 0x9AB84 },
        };

        public bool TryGetSprite(int itemID, out Bitmap sprite, bool transparentBG = true)
        {
            string key = (MainEditor.RomType == RomType.BW2 ? (MainEditor.BlackVersion ? "b2" : "w2") :
                MainEditor.RomType == RomType.BW1 ? (MainEditor.BlackVersion ? "b" : "w") : "");
            if (!assignmentArray.ContainsKey(key))
            {
                sprite = null;
                return false;
            }

            byte[] arm = MainEditor.fileSystem.GetDecompressedArm9();
            int spriteFile = HelperFunctions.ReadShort(arm, assignmentArray[key] + 4 * itemID);
            int paletteFile = HelperFunctions.ReadShort(arm, assignmentArray[key] + 2 + 4 * itemID);

            if (spriteFile >= MainEditor.itemSpriteNarc.files.Count || paletteFile >= MainEditor.itemSpriteNarc.files.Count || itemID >= MainEditor.itemDataNarc.items.Count)
            {
                sprite = null;
                return false;
            }

            Bitmap img = new Bitmap(32, 32);
            Color[] pal = new Color[16];
            for (int i = 0; i < 16; i++) pal[i] = DsDecmp.Read16BitColor(HelperFunctions.ReadShort(MainEditor.itemSpriteNarc.files[paletteFile].bytes, 40 + i * 2));
            byte[] b = MainEditor.itemSpriteNarc.files[spriteFile].bytes;
            for (int tile = 0; tile < 16; tile++)
            {
                int tileX = tile % 4;
                int tileY = tile / 4;
                for (int yT = 0; yT < 8; yT++)
                {
                    for (int xT = 0; xT < 8; xT++)
                    {
                        int value = b[tile * 32 + yT * 4 + xT / 2 + 48];
                        value = (value >> (xT % 2) * 4) & ((1 << 4) - 1);
                        if (transparentBG && value == 0) img.SetPixel(tileX * 8 + xT, tileY * 8 + yT, Color.Transparent);
                        else img.SetPixel(tileX * 8 + xT, tileY * 8 + yT, pal[value]);
                    }
                }
            }

            sprite = img;
            return true;
        }
    }

    public class ItemSpriteEntry
    {
        public byte[] bytes;

        public ItemSpriteEntry(byte[] bytes)
        {
            this.bytes = bytes;
        }
    }
}
