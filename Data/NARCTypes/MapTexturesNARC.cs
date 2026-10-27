using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewEditor.Forms;

namespace NewEditor.Data.NARCTypes
{
    public class MapTexturesNARC : NARC
    {
        public List<MapTextureEntry> files;

        public override void ReadData()
        {
            base.ReadData();

            int pos = pointerStartAddress;
            int initialPosition = FileEntryStart;

            //Register data files
            files = new List<MapTextureEntry>();

            //Populate data types
            for (int i = 0; i < numFileEntries; i++)
            {
                int start = HelperFunctions.ReadInt(byteData, pos);
                int end = HelperFunctions.ReadInt(byteData, pos + 4);
                byte[] bytes = new byte[end - start];

                for (int j = 0; j < end - start; j++) bytes[j] = byteData[initialPosition + start + j];

                MapTextureEntry m = new MapTextureEntry(bytes) { nameID = i };
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
            foreach (MapTextureEntry m in files)
            {
                newByteData.InsertRange(pPos, BitConverter.GetBytes(totalSize));
                pPos += 4;
                totalSize += m.bytes.Length;
                newByteData.InsertRange(pPos, BitConverter.GetBytes(totalSize));
                pPos += 4;
            }
            foreach (MapTextureEntry m in files)
            {
                newByteData.AddRange(m.bytes);
            }
            byteData = newByteData.ToArray();

            FixHeaders(files.Count);
            base.WriteData();
        }
    }

    public class MapTextureEntry
    {
        public byte[] bytes;
        public int nameID;

        public MapTextureEntry(byte[] bytes)
        {
            this.bytes = bytes;
        }

        public override string ToString()
        {
            return nameID.ToString();
        }
    }
}
