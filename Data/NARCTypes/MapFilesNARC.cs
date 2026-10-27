using DSDecmp;
using LibNDSFormats.NSBMD;
using NewEditor.Data.Mapping;
using NewEditor.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NewEditor.Data.NARCTypes
{
    public class MapFilesNARC : NARC
    {
        public static int fid;
        public List<MapFileEntry> files;

        public override void ReadData()
        {
            base.ReadData();

            int pos = pointerStartAddress;
            int initialPosition = FileEntryStart;

            //Register data files
            files = new List<MapFileEntry>();
            fid = 0;
            //Populate data types
            for (int i = 0; i < numFileEntries; i++)
            {
                int start = HelperFunctions.ReadInt(byteData, pos);
                int end = HelperFunctions.ReadInt(byteData, pos + 4);
                byte[] bytes = new byte[end - start];

                for (int j = 0; j < end - start; j++) bytes[j] = byteData[initialPosition + start + j];

                MapFileEntry m = new MapFileEntry(bytes) { nameID = i };
                files.Add(m);
                fid++;

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
            foreach (MapFileEntry m in files)
            {
                newByteData.InsertRange(pPos, BitConverter.GetBytes(totalSize));
                pPos += 4;
                totalSize += m.bytes.Length;
                newByteData.InsertRange(pPos, BitConverter.GetBytes(totalSize));
                pPos += 4;
            }
            foreach (MapFileEntry m in files)
            {
                newByteData.AddRange(m.bytes);
            }
            byteData = newByteData.ToArray();

            FixHeaders(files.Count);
            base.WriteData();
        }
    }

    public class MapFileEntry
    {
        public byte[] bytes;
        public int nameID;

        public byte[] header;
        public byte[] bdmo;
        public byte[] collisions;
        public byte[] collisions2;
        public byte[] buildingData;
        public List<Building> buildings;
        public byte[] mapModelData;
        public NSBMD mapModel;

        public MapFileEntry(byte[] bytes)
        {
            this.bytes = bytes;

            ReadData();
        }
        public void ImportBuildings(byte[] newData)
        {
            buildings = new List<Building>();
            if (newData.Length == 0) return;
            using (BinaryReader reader = new BinaryReader(new MemoryStream(newData)))
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    buildings.Add(new Building(new MemoryStream(reader.ReadBytes(16))));
                }
            }
        }

        public void ReadData()
        {
            bool twoCollisions = HelperFunctions.ReadInt(bytes, 0x0) == 0x44347;

            if (twoCollisions)
            {
                int bdmoStart = HelperFunctions.ReadInt(bytes, 0x4);
                int bdmoLength = HelperFunctions.ReadInt(bytes, bdmoStart + 0x8);
                int collisionStart = HelperFunctions.ReadInt(bytes, 0x8);
                int collision2Start = HelperFunctions.ReadInt(bytes, 0x8);
                int buildingStart = HelperFunctions.ReadInt(bytes, 0x10);

                List<byte> b = new List<byte>(bytes);
                header = b.GetRange(0, bdmoStart).ToArray();
                bdmo = b.GetRange(bdmoStart, bdmoLength).ToArray();
                collisions = b.GetRange(collisionStart, collision2Start - collisionStart).ToArray();
                collisions2 = b.GetRange(collision2Start, buildingStart - collision2Start).ToArray();
                buildingData = b.GetRange(buildingStart, bytes.Length - buildingStart).ToArray();
            }
            else
            {
                int bdmoStart = HelperFunctions.ReadInt(bytes, 0x4);
                int bdmoLength = HelperFunctions.ReadInt(bytes, bdmoStart + 0x8);
                int collisionStart = HelperFunctions.ReadInt(bytes, 0x8);
                int buildingStart = HelperFunctions.ReadInt(bytes, 0xc);

                List<byte> b = new List<byte>(bytes);
                header = b.GetRange(0, bdmoStart).ToArray();
                bdmo = b.GetRange(bdmoStart, bdmoLength).ToArray();
                collisions = b.GetRange(collisionStart, buildingStart - collisionStart).ToArray();
                buildingData = b.GetRange(buildingStart, bytes.Length - buildingStart).ToArray();
                collisions2 = new byte[0];
            }

            using (BinaryReader modelReader = new BinaryReader(new MemoryStream(bytes)))
            {
                modelReader.BaseStream.Position = 0x22;
                if (modelReader.ReadInt16() > 1)
                {
                    // If NSBMD contains more than one segment, it means there are embedded textures we must remove
                    mapModelData = MappingUtils.GetModelWithoutTextures(bytes);
                }
                else
                {
                    modelReader.BaseStream.Position = 0x14;
                    mapModelData = modelReader.ReadBytes((int)modelReader.BaseStream.Length);
                }

                /* Read buildings data */
                ImportBuildings(buildingData);

                mapModel = NSBMDLoader.LoadNSBMD(new MemoryStream(bdmo));
            }
        }

        public void ApplyData()
        {
            if (collisions2.Length > 0)
            {
                header = new byte[0x18];
                HelperFunctions.WriteInt(header, 0x0, 0x44347);
                HelperFunctions.WriteInt(header, 0x4, header.Length);
                HelperFunctions.WriteInt(header, 0x8, header.Length + bdmo.Length);
                HelperFunctions.WriteInt(header, 0xc, header.Length + bdmo.Length + collisions.Length);
                HelperFunctions.WriteInt(header, 0x10, header.Length + bdmo.Length + collisions.Length + collisions2.Length);
                HelperFunctions.WriteInt(header, 0x14, header.Length + bdmo.Length + collisions.Length + collisions2.Length + buildingData.Length);
            }
            else
            {
                header = new byte[0x14];
                HelperFunctions.WriteInt(header, 0x0, 0x34257);
                HelperFunctions.WriteInt(header, 0x4, header.Length);
                HelperFunctions.WriteInt(header, 0x8, header.Length + bdmo.Length);
                HelperFunctions.WriteInt(header, 0xc, header.Length + bdmo.Length + collisions.Length);
                HelperFunctions.WriteInt(header, 0x10, header.Length + bdmo.Length + collisions.Length + buildingData.Length);
            }
            HelperFunctions.WriteInt(bdmo, 0x8, bdmo.Length);

            List<byte> b = new List<byte>();
            b.AddRange(header);
            b.AddRange(bdmo);
            b.AddRange(collisions);
            b.AddRange(collisions2);
            b.AddRange(buildingData);
            bytes = b.ToArray();
        }

        public override string ToString()
        {
            return nameID.ToString();
        }
    }

    public class Building
    {
        #region Fields (11)
        public NSBMD NSBMDFile;
        public int modelID { get; set; }
        public float xPosition;
        public float yPosition;
        public float zPosition;
        public float rotation;
        #endregion Fields

        #region Constructors (2)
        public Building(Stream data)
        {
            using (BinaryReader reader = new BinaryReader(data))
            {
                xPosition = reader.ReadInt32() / 4096f;
                yPosition = reader.ReadInt32() / 4096f;
                zPosition = reader.ReadInt32() / 4096f;

                rotation = reader.ReadUInt16();
                modelID = (reader.ReadByte() << 8) + reader.ReadByte();
            }
        }
        #endregion Constructors
        public static ushort DegToU16(float deg)
        {
            return (ushort)(deg * 65536 / 360);
        }
        public static float U16ToDeg(ushort u16)
        {
            return (float)u16 * 360 / 65536;
        }
        public void LoadModelDataFromID(BuildingContainerEntry buildingContainer)
        {
            try
            {
                if (!buildingContainer.idLookupTable.ContainsKey((int)modelID)) return;
                using (MemoryStream ms = new MemoryStream(buildingContainer.models[buildingContainer.idLookupTable[(int)modelID]].ToArray()))
                {
                    NSBMDFile = NSBMDLoader.LoadNSBMD(ms);
                }
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("Building " + modelID + " could not be found\n", "Building not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
