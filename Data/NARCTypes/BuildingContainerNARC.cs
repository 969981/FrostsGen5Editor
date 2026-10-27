using NewEditor.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace NewEditor.Data.NARCTypes
{
    public class BuildingContainerNARC : NARC
    {
        public List<BuildingContainerEntry> files;

        public override void ReadData()
        {
            base.ReadData();

            int pos = pointerStartAddress;
            int initialPosition = FileEntryStart;

            //Register data files
            files = new List<BuildingContainerEntry>();

            //Populate data types
            for (int i = 0; i < numFileEntries; i++)
            {
                int start = HelperFunctions.ReadInt(byteData, pos);
                int end = HelperFunctions.ReadInt(byteData, pos + 4);
                byte[] bytes = new byte[end - start];

                for (int j = 0; j < end - start; j++) bytes[j] = byteData[initialPosition + start + j];

                BuildingContainerEntry m = new BuildingContainerEntry(bytes) { nameID = i };
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
            foreach (BuildingContainerEntry m in files)
            {
                newByteData.InsertRange(pPos, BitConverter.GetBytes(totalSize));
                pPos += 4;
                totalSize += m.bytes.Length;
                newByteData.InsertRange(pPos, BitConverter.GetBytes(totalSize));
                pPos += 4;
            }
            foreach (BuildingContainerEntry m in files)
            {
                newByteData.AddRange(m.bytes);
            }
            byteData = newByteData.ToArray();

            FixHeaders(files.Count);
            base.WriteData();
        }
    }

    public class BuildingContainerEntry
    {
        public byte[] bytes;
        public int nameID;

        public List<List<byte>> models = new List<List<byte>>();
        public Dictionary<int, int> idLookupTable = new Dictionary<int, int>();
        public List<AreaBuildingEntry> areaBuildings = new List<AreaBuildingEntry>();

        public BuildingContainerEntry(byte[] bytes)
        {
            this.bytes = bytes;

            areaBuildings = new List<AreaBuildingEntry>();
            int numFiles = HelperFunctions.ReadShort(bytes, 2);

            List<int> ids = new List<int>();

            List<byte> byteList = new List<byte>(bytes);
            for (int i = 0; i < numFiles / 2; i++)
            {
                List<int> fileOffsets = new List<int>();
                int abEntrySection = HelperFunctions.ReadInt(bytes, 4 + 4 * i) + 0x14;
                int pos = HelperFunctions.ReadInt(bytes, 4 + 4 * i);
                AreaBuildingEntry ab = new AreaBuildingEntry()
                {
                    ID = (short)HelperFunctions.ReadShort(bytes, pos),
                    Type = (short)HelperFunctions.ReadShort(bytes, pos + 2),
                    DoorID = (short)HelperFunctions.ReadShort(bytes, pos + 4),
                    X = (short)HelperFunctions.ReadShort(bytes, pos + 6),
                    Y = (short)HelperFunctions.ReadShort(bytes, pos + 8),
                    Z = (short)HelperFunctions.ReadShort(bytes, pos + 10),
                    Unused = (short)HelperFunctions.ReadShort(bytes, pos + 12),
                    Unused2 = (short)HelperFunctions.ReadShort(bytes, pos + 14),
                    ControllerFunc = (short)HelperFunctions.ReadShort(bytes, pos + 16),
                    AnimCountPerAnimSet = bytes[pos + 18],
                    ItemCount = bytes[pos + 19],
                };
                ids.Add(ab.ID);

                pos += 20;
                while (bytes[pos] != 0xFF && fileOffsets.Count < 4)
                {
                    fileOffsets.Add(abEntrySection + HelperFunctions.ReadInt(bytes, pos));
                    pos += 4;
                }

                for (int j = 0; j < fileOffsets.Count; j++)
                {
                    pos = fileOffsets[j] + 4;
                    int size = HelperFunctions.ReadInt(bytes, pos);
                    pos = fileOffsets[j] - 4;
                    ab.animations.Add(byteList.GetRange(pos, size));
                }
                areaBuildings.Add(ab);
            }

            for (int i = numFiles / 2; i < numFiles; i++)
            {
                if (!idLookupTable.ContainsKey(ids[numFiles - i - 1]))
                {
                    idLookupTable.Add(ids[numFiles - i - 1], numFiles - i - 1);
                }
                int pos = HelperFunctions.ReadInt(bytes, 4 + 4 * i) + 0x8;
                int size = HelperFunctions.ReadInt(bytes, pos);
                pos -= 8;
                models.Add(byteList.GetRange(pos, size));
            }
        }

        public override string ToString()
        {
            return nameID.ToString();
        }
    }

    public class AreaBuildingEntry
    {
        public short ID;
        public short Type;
        public short DoorID;
        public short X;
        public short Y;
        public short Z;
        public short Unused;
        public short Unused2;
        public short ControllerFunc;
        public byte AnimCountPerAnimSet;
        public byte ItemCount;

        public List<List<byte>> animations = new List<List<byte>>();
    }
}
