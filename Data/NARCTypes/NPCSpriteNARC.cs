using LibNDSFormats.NSBMD;
using LibNDSFormats.NSBTX;
using NewEditor.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewEditor.Data.NARCTypes
{
    public class NPCSpriteNARC : NARC
    {
        public List<NPCSpriteEntry> files;

        public override void ReadData()
        {
            base.ReadData();

            int pos = pointerStartAddress;
            int initialPosition = FileEntryStart;

            //Register data files
            files = new List<NPCSpriteEntry>();

            //Populate data types
            for (int i = 0; i < numFileEntries; i++)
            {
                int start = HelperFunctions.ReadInt(byteData, pos);
                int end = HelperFunctions.ReadInt(byteData, pos + 4);
                byte[] bytes = new byte[end - start];

                for (int j = 0; j < end - start; j++) bytes[j] = byteData[initialPosition + start + j];

                NPCSpriteEntry m = new NPCSpriteEntry(bytes) { nameID = i };
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
            foreach (NPCSpriteEntry m in files)
            {
                newByteData.InsertRange(pPos, BitConverter.GetBytes(totalSize));
                pPos += 4;
                totalSize += m.bytes.Length;
                newByteData.InsertRange(pPos, BitConverter.GetBytes(totalSize));
                pPos += 4;
            }
            foreach (NPCSpriteEntry m in files)
            {
                newByteData.AddRange(m.bytes);
            }
            byteData = newByteData.ToArray();

            FixHeaders(files.Count);
            base.WriteData();
        }
    }

    public class NPCSpriteEntry
    {
        public byte[] bytes;
        public int nameID;

        public NPCSpriteEntry(byte[] bytes)
        {
            this.bytes = bytes;
        }

        public Bitmap GetSprite(int direction, bool removeBG = true)
        {
            List<NSBMDMaterial> mats = NSBTXLoader.LoadNsbtx(new MemoryStream(bytes), out var tex, out var pals).ToList();

            Bitmap img = new Bitmap(32, 32);
            Color[] pal = new Color[16];
            for (int i = 0; i < 16; i++) pal[i] = mats[0].paldata[i].ToColor();
            for (int c = 0; c < 32 * 32; c++)
            {
                int x = c % 32;
                int y = c / 32;
                byte b = mats[TextureIDFromDirection(mats, direction, out bool flipX)].texdata[c / 2];
                int id = c % 2 == 1 ? b >> 4 : b & 0xf;
                if (!removeBG || id != 0)
                    img.SetPixel(x, y, pal[id]);
            }
            return img;
        }

        public static int TextureIDFromDirection(List<NSBMDMaterial> list, int direction, out bool flipX)
        {
            if (new List<int>() { 24, 16, 14, 15, 12, 22, 18, 19, 17 }.Contains(list.Count))
            {
                flipX = false;
                return direction * 3;
            }
            if (list.Count == 8)
            {
                flipX = false;
                return direction * 2;
            }
            if (new List<int>() { 7, 9, 13, 6 }.Contains(list.Count))
            {
                flipX = direction == 3;
                return flipX ? 4 : direction * 2;
            }

            flipX = false;
            return 0;
        }

        public override string ToString()
        {
            return nameID.ToString();
        }
    }
}
