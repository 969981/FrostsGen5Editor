using NewEditor.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;

namespace NewEditor.Data.NARCTypes
{
    public class PokemonSpritesNARC : NARC
    {
        public List<PokemonSpriteEntry> sprites;
        List<byte[]> remainingfiles;

        public override void ReadData()
        {
            base.ReadData();

            int pos = pointerStartAddress;
            int initialPosition = FileEntryStart;

            //Register data files
            sprites = new List<PokemonSpriteEntry>();

            //Populate data types
            int nameID = 0;
            int i = 0;
            for (i = 0; i <= numFileEntries - 20; i += 20)
            {
                PokemonSpriteEntry p = new PokemonSpriteEntry() { nameID = nameID };
                for (int j = 0; j < 20; j++)
                {
                    int start = HelperFunctions.ReadInt(byteData, pos);
                    int end = HelperFunctions.ReadInt(byteData, pos + 4);
                    byte[] bytes = new byte[end - start];

                    for (int k = 0; k < end - start; k++) bytes[k] = byteData[initialPosition + start + k];
                    p.files.Add(bytes);
                    pos += 8;
                }

                sprites.Add(p);

                p.ReadData();

                nameID++;
            }

            remainingfiles = new List<byte[]>();

            for (; i < numFileEntries; i++)
            {
                int start = HelperFunctions.ReadInt(byteData, pos);
                int end = HelperFunctions.ReadInt(byteData, pos + 4);
                byte[] bytes = new byte[end - start];

                for (int j = 0; j < end - start; j++) bytes[j] = byteData[initialPosition + start + j];

                remainingfiles.Add(bytes);

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
            foreach (PokemonSpriteEntry p in sprites)
            {
                for (int j = 0; j < 20; j++)
                {
                    newByteData.InsertRange(pPos, BitConverter.GetBytes(totalSize));
                    pPos += 4;
                    totalSize += p.files[j].Length;
                    newByteData.InsertRange(pPos, BitConverter.GetBytes(totalSize));
                    pPos += 4;
                }
            }

            foreach (byte[] b in remainingfiles)
            {
                newByteData.InsertRange(pPos, BitConverter.GetBytes(totalSize));
                pPos += 4;
                totalSize += b.Length;
                newByteData.InsertRange(pPos, BitConverter.GetBytes(totalSize));
                pPos += 4;
            }

            foreach (PokemonSpriteEntry p in sprites)
            {
                for (int j = 0; j < 20; j++)
                {
                    newByteData.AddRange(p.files[j]);
                }
            }
            foreach (byte[] b in remainingfiles)
            {
                newByteData.AddRange(b);
            }

            byteData = newByteData.ToArray();

            FixHeaders(sprites.Count * 20 + remainingfiles.Count);

            base.WriteData();
        }

        public override byte[] GetPatchBytes(NARC narc)
        {
            PokemonSpritesNARC other = narc as PokemonSpritesNARC;
            List<byte> bytes = new List<byte>();

            for (int i = 0; i < numFileEntries - 25; i++)
            {
                if (i / 20 > other.sprites.Count || !sprites[i / 20].files[i % 20].SequenceEqual(other.sprites[i / 20].files[i % 20]))
                {
                    bytes.AddRange(BitConverter.GetBytes(i));
                    bytes.AddRange(BitConverter.GetBytes(sprites[i / 20].files[i % 20].Length));
                    bytes.AddRange(sprites[i / 20].files[i % 20]);
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

                if (id / 20 > sprites.Count)
                {
                    //Don't accept extra files here
                }
                else sprites[id / 20].files[id % 20] = new List<byte>(bytes).GetRange(pos, size).ToArray();
                sprites[id / 20].ReadData();
                pos += size;
            }
        }
    }

    public class PokemonSpriteEntry
    {
        public List<byte[]> files;

        public byte[] FrontSpriteBytes => files[0];
        public byte[] FrontFemaleSpriteBytes => files[1];
        public byte[] FrontRigBytes => files[2];
        public byte[] FrontFemaleRigBytes => files[3];
        public byte[] FrontCellsBytes => files[4];
        public byte[] FrontAnimationBytes => files[5];
        public byte[] FrontRCMNBytes => files[6];
        public byte[] FrontRAMNBytes => files[7];
        public byte[] FrontTileBytes => files[8];
        public byte[] BackSpriteBytes => files[9];
        public byte[] BackFemaleSpriteBytes => files[10];
        public byte[] BackRigBytes => files[11];
        public byte[] BackFemaleRigBytes => files[12];
        public byte[] BackCellsBytes => files[13];
        public byte[] BackAnimationBytes => files[14];
        public byte[] BackRCMNBytes => files[15];
        public byte[] BackRAMNBytes => files[16];
        public byte[] BackTileBytes => files[17];
        public byte[] PaletteBytes => files[18];
        public byte[] ShinyPaletteBytes => files[19];

        public int nameID;

        public byte[] decompressedFrontSprite;
        public byte[] decompressedFrontRig;
        public byte[] decompressedFrontFemaleSprite;
        public byte[] decompressedFrontFemaleRig;
        public byte[] decompressedBackSprite;
        public byte[] decompressedBackRig;
        public byte[] decompressedBackFemaleSprite;
        public byte[] decompressedBackFemaleRig;

        public RNANFile frontRNAN;
        public RNANFile backRNAN;
        public RECNFile frontRECN;
        public RECNFile backRECN;
        public RCMNFile frontRCMN;
        public RCMNFile backRCMN;
        public RAMNFile frontRAMN;
        public RAMNFile backRAMN;
        public RigCellsFile frontRigCells;
        public RigCellsFile backRigCells;

        public Color[] palette;
        public Color[] shinyPalette;

        public PokemonSpriteEntry() { files = new List<byte[]>(); }

        public void ReadData()
        {
            palette = new Color[16];
            for (int i = 0; i < 16; i++) palette[i] = DsDecmp.Read16BitColor(HelperFunctions.ReadShort(PaletteBytes, 40 + i * 2));
            shinyPalette = new Color[16];
            for (int i = 0; i < 16; i++) shinyPalette[i] = DsDecmp.Read16BitColor(HelperFunctions.ReadShort(ShinyPaletteBytes, 40 + i * 2));
            decompressedFrontSprite = DsDecmp.Decompress(FrontSpriteBytes);
            decompressedFrontFemaleSprite = DsDecmp.Decompress(FrontFemaleSpriteBytes);
            decompressedFrontRig = DsDecmp.Decompress(FrontRigBytes);
            decompressedFrontFemaleRig = DsDecmp.Decompress(FrontFemaleRigBytes);
            decompressedBackSprite = DsDecmp.Decompress(BackSpriteBytes);
            decompressedBackFemaleSprite = DsDecmp.Decompress(BackFemaleSpriteBytes);
            decompressedBackRig = DsDecmp.Decompress(BackRigBytes);
            decompressedBackFemaleRig = DsDecmp.Decompress(BackFemaleRigBytes);

            RereadFiles();
        }

        public void ApplyData()
        {
            for (int i = 0; i < 16; i++) HelperFunctions.WriteShort(PaletteBytes, 40 + i * 2, DsDecmp.Write16BitColor(palette[i]));
            for (int i = 0; i < 16; i++) HelperFunctions.WriteShort(ShinyPaletteBytes, 40 + i * 2, DsDecmp.Write16BitColor(shinyPalette[i]));
        }

        public void RereadFiles()
        {
            frontRigCells = new RigCellsFile(FrontTileBytes);
            backRigCells = new RigCellsFile(BackTileBytes);

            if (FrontAnimationBytes[0] == 0x11)
                frontRNAN = new RNANFile(DsDecmp.Decompress(FrontAnimationBytes));

            if (BackAnimationBytes[0] == 0x11)
                backRNAN = new RNANFile(DsDecmp.Decompress(BackAnimationBytes));

            if (HelperFunctions.ReadInt(FrontCellsBytes, 0) == 0x4E434552)  //RECN
            {
                frontRECN = new RECNFile(FrontCellsBytes);
                backRECN = new RECNFile(BackCellsBytes);
            }

            if (HelperFunctions.ReadInt(FrontRCMNBytes, 0) == 0x4E4D4352)   //RCMN
            {
                frontRCMN = new RCMNFile(FrontRCMNBytes);
                backRCMN = new RCMNFile(BackRCMNBytes);
            }
            if (HelperFunctions.ReadInt(FrontRAMNBytes, 0) == 0x4E4D4152)   //RAMN
            {
                frontRAMN = new RAMNFile(FrontRAMNBytes);
                backRAMN = new RAMNFile(BackRAMNBytes);
            }
        }

        public void WriteRigCellsFile()
        {
            frontRigCells.WriteData();
            files[8] = frontRigCells.byteData;
            backRigCells.WriteData();
            files[17] = backRigCells.byteData;
        }

        public void WriteAnimationFiles()
        {
            if (FrontAnimationBytes[0] == 0x11)
            {
                frontRNAN.WriteData();
                files[5] = DsDecmp.Compress(frontRNAN.byteData);
            }

            if (BackAnimationBytes[0] == 0x11)
            {
                backRNAN.WriteData();
                files[14] = DsDecmp.Compress(backRNAN.byteData);
            }

            //This doesn't get edited?
            //if (HelperFunctions.ReadInt(FrontCellsBytes, 0) == 0x4E434552)  //RECN
            //{
            //    frontRECN.WriteData();
            //    files[4] = frontRECN.byteData;
            //    backRECN.WriteData();
            //    files[13] = backRECN.byteData;
            //}
            if (HelperFunctions.ReadInt(FrontRCMNBytes, 0) == 0x4E4D4352)   //RCMN
            {
                frontRCMN.WriteData();
                files[6] = frontRCMN.byteData;
                backRCMN.WriteData();
                files[15] = backRCMN.byteData;
            }
            if (HelperFunctions.ReadInt(FrontRAMNBytes, 0) == 0x4E4D4152)   //RAMN
            {
                frontRAMN.WriteData();
                files[7] = frontRAMN.byteData;
                backRAMN.WriteData();
                files[16] = backRAMN.byteData;
            }
        }

        public Bitmap GetSprite(bool shiny = false, bool back = false, bool female = false)
        {
            byte[] b = decompressedFrontSprite;
            if (back && female) b = decompressedBackFemaleSprite;
            else if (back) b = decompressedBackSprite;
            else if (female) b = decompressedFrontFemaleSprite;
            if (b == null || b.Length == 0) return null;
            Bitmap img = new Bitmap(64, 144);
            Color[] pal = shiny ? shinyPalette : palette;
            for (int tile = 0; tile < 144; tile++)
            {
                int tileX = tile % 8;
                int tileY = tile / 8;
                for (int yT = 0; yT < 8; yT++)
                {
                    for (int xT = 0; xT < 8; xT++)
                    {
                        int value = b[tile * 32 + yT * 4 + xT / 2 + 48];
                        value = (value >> (xT % 2) * 4) & ((1 << 4) - 1);
                        img.SetPixel(tileX * 8 + xT, tileY * 8 + yT, pal[value]);
                    }
                }
            }
            if (img == null) return img;

            //Unscrambling
            Bitmap finalImage = new Bitmap(96, 96, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            Graphics g = Graphics.FromImage(finalImage);
            g.DrawImage(img, new Rectangle(0, 0, 64, 64), new Rectangle(0, 0, 64, 64), GraphicsUnit.Pixel);
            g.DrawImage(img, new Rectangle(64, 0, 32, 8), new Rectangle(0, 64, 32, 8), GraphicsUnit.Pixel);
            g.DrawImage(img, new Rectangle(64, 8, 32, 8), new Rectangle(32, 64, 32, 8), GraphicsUnit.Pixel);
            g.DrawImage(img, new Rectangle(64, 16, 32, 8), new Rectangle(0, 72, 32, 8), GraphicsUnit.Pixel);
            g.DrawImage(img, new Rectangle(64, 24, 32, 8), new Rectangle(32, 72, 32, 8), GraphicsUnit.Pixel);
            g.DrawImage(img, new Rectangle(64, 32, 32, 8), new Rectangle(0, 80, 32, 8), GraphicsUnit.Pixel);
            g.DrawImage(img, new Rectangle(64, 40, 32, 8), new Rectangle(32, 80, 32, 8), GraphicsUnit.Pixel);
            g.DrawImage(img, new Rectangle(64, 48, 32, 8), new Rectangle(0, 88, 32, 8), GraphicsUnit.Pixel);
            g.DrawImage(img, new Rectangle(64, 56, 32, 8), new Rectangle(32, 88, 32, 8), GraphicsUnit.Pixel);
            g.DrawImage(img, new Rectangle(0, 64, 64, 32), new Rectangle(0, 96, 64, 32), GraphicsUnit.Pixel);
            g.DrawImage(img, new Rectangle(64, 64, 32, 8), new Rectangle(0, 128, 32, 8), GraphicsUnit.Pixel);
            g.DrawImage(img, new Rectangle(64, 72, 32, 8), new Rectangle(32, 128, 32, 8), GraphicsUnit.Pixel);
            g.DrawImage(img, new Rectangle(64, 80, 32, 8), new Rectangle(0, 136, 32, 8), GraphicsUnit.Pixel);
            g.DrawImage(img, new Rectangle(64, 88, 32, 8), new Rectangle(32, 136, 32, 8), GraphicsUnit.Pixel);
            return finalImage;
        }

        public Bitmap GetRig(bool shiny = false, bool back = false, bool female = false)
        {
            byte[] b = decompressedFrontRig;
            if (back && female) b = decompressedBackFemaleRig;
            else if (back) b = decompressedBackRig;
            else if (female) b = decompressedFrontFemaleRig;
            if (b == null || b.Length == 0) return null;
            Color[] pal = shiny ? shinyPalette : palette;
            Bitmap bmp = new Bitmap(256, 128);
            for (int i = 0; i < 256 * 128; i++)
            {
                int c = b[(i / 2) + 48];
                c = i % 2 == 0 ? c & 0b_1111 : (c & 0b_1111_0000) >> 4;
                bmp.SetPixel(i % 256, i / 256, pal[c]);
            }
            return bmp;
        }

        public void SetSprite(Bitmap image, bool shiny = false, bool back = false, bool female = false)
        {
            if (image.Width != 96 || image.Height != 96)
            {
                MessageBox.Show("Sprite size should be 96 x 96");
                return;
            }
            Bitmap scrambled = new Bitmap(64, 144, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            Graphics g = Graphics.FromImage(scrambled);
            g.DrawImage(image, new Rectangle(0, 0, 64, 64), new Rectangle(0, 0, 64, 64), GraphicsUnit.Pixel);
            g.DrawImage(image, new Rectangle(0, 64, 32, 8), new Rectangle(64, 0, 32, 8), GraphicsUnit.Pixel);
            g.DrawImage(image, new Rectangle(32, 64, 32, 8), new Rectangle(64, 8, 32, 8), GraphicsUnit.Pixel);
            g.DrawImage(image, new Rectangle(0, 72, 32, 8), new Rectangle(64, 16, 32, 8), GraphicsUnit.Pixel);
            g.DrawImage(image, new Rectangle(32, 72, 32, 8), new Rectangle(64, 24, 32, 8), GraphicsUnit.Pixel);
            g.DrawImage(image, new Rectangle(0, 80, 32, 8), new Rectangle(64, 32, 32, 8), GraphicsUnit.Pixel);
            g.DrawImage(image, new Rectangle(32, 80, 32, 8), new Rectangle(64, 40, 32, 8), GraphicsUnit.Pixel);
            g.DrawImage(image, new Rectangle(0, 88, 32, 8), new Rectangle(64, 48, 32, 8), GraphicsUnit.Pixel);
            g.DrawImage(image, new Rectangle(32, 88, 32, 8), new Rectangle(64, 56, 32, 8), GraphicsUnit.Pixel);
            g.DrawImage(image, new Rectangle(0, 96, 64, 32), new Rectangle(0, 64, 64, 32), GraphicsUnit.Pixel);
            g.DrawImage(image, new Rectangle(0, 128, 32, 8), new Rectangle(64, 64, 32, 8), GraphicsUnit.Pixel);
            g.DrawImage(image, new Rectangle(32, 128, 32, 8), new Rectangle(64, 72, 32, 8), GraphicsUnit.Pixel);
            g.DrawImage(image, new Rectangle(0, 136, 32, 8), new Rectangle(64, 80, 32, 8), GraphicsUnit.Pixel);
            g.DrawImage(image, new Rectangle(32, 136, 32, 8), new Rectangle(64, 88, 32, 8), GraphicsUnit.Pixel);

            byte[] b = decompressedFrontSprite;
            if (back && female) b = decompressedBackFemaleSprite;
            else if (back) b = decompressedBackSprite;
            else if (female) b = decompressedFrontFemaleSprite;
            Color[] pal = shiny ? shinyPalette : palette;

            for (int tile = 0; tile < 144; tile++)
            {
                int tileX = tile % 8;
                int tileY = tile / 8;
                for (int yT = 0; yT < 8; yT++)
                {
                    for (int xT = 0; xT < 8; xT++)
                    {
                        int value = b[tile * 32 + yT * 4 + xT / 2 + 48];
                        value = (value >> (xT % 2) * 4) & ((1 << 4) - 1);
                        Color c = scrambled.GetPixel(tileX * 8 + xT, tileY * 8 + yT);
                        int cid = 0;
                        for (int j = 1; j < 16; j++) if (c.R == pal[j].R && c.G == pal[j].G && c.B == pal[j].B) cid = j;

                        if (xT % 2 == 0) b[tile * 32 + yT * 4 + xT / 2 + 48] = (byte)cid;
                        else b[tile * 32 + yT * 4 + xT / 2 + 48] = (byte)((cid << 4) | b[tile * 32 + yT * 4 + xT / 2 + 48]);
                    }
                }
            }

            int n = 0;
            if (female) n++;
            if (back) n += 9;
            files[n] = DsDecmp.Compress(b);
        }

        public void SetRig(Bitmap image, bool shiny = false, bool back = false, bool female = false)
        {
            if (image.Width != 256 || image.Height != 128)
            {
                MessageBox.Show("Sprite size should be 256 x 128");
                return;
            }
            byte[] b = decompressedFrontRig;
            if (back && female) b = decompressedBackFemaleRig;
            else if (back) b = decompressedBackRig;
            else if (female) b = decompressedFrontFemaleRig;
            Color[] pal = shiny ? shinyPalette : palette;
            for (int i = 0; i < 256 * 128; i++)
            {
                Color c = image.GetPixel(i % 256, i / 256);
                int cid = 0;
                for (int j = 1; j < 16; j++) if (c.R == pal[j].R && c.G == pal[j].G && c.B == pal[j].B) cid = j;
                if (i % 2 == 0) b[(i / 2) + 48] = (byte)cid;
                else b[(i / 2) + 48] = (byte)((cid << 4) | b[(i / 2) + 48]);
            }

            int n = 2;
            if (female) n++;
            if (back) n += 9;
            files[n] = DsDecmp.Compress(b);
        }

        public override string ToString()
        {
            return nameID < MainEditor.textNarc.textFiles[VersionConstants.PokemonNameTextFileID].text.Count ? MainEditor.textNarc.textFiles[VersionConstants.PokemonNameTextFileID].text[nameID] + " - " + nameID : "Name not found";
        }
    }

    public class RNANFile
    {
        //file size at 0x8
        //address of name section at 0x14 (subtract 0x10)
        //sequence count at 0x18 (short)
        //total frame count at 0x1a (short)
        //address of frame pointers at 0x20 (add 0x18)
        //address of frame values at 0x24 (add 0x18)
        public byte[] byteData;
        public List<RNANSequence> sequences = new List<RNANSequence>();

        public RNANFile(byte[] byteData)
        {
            this.byteData = byteData;

            int seqCount = HelperFunctions.ReadShort(byteData, 0x18);
            int framesStart = HelperFunctions.ReadShort(byteData, 0x20) + 0x18;
            int frameValuesStart = HelperFunctions.ReadShort(byteData, 0x24) + 0x18;
            for (int i = 0; i < seqCount; i++)
            {
                int frameCount = HelperFunctions.ReadInt(byteData, 0x30 + 0x10 * i);
                int seqStart = HelperFunctions.ReadInt(byteData, 0x3C + 0x10 * i) + framesStart;
                int motionType = byteData[0x34 + 0x10 * i];
                int cellType = byteData[0x36 + 0x10 * i];
                int loopType = byteData[0x38 + 0x10 * i];
                byte[] seqBytes = new byte[16];
                for (int j = 0; j < 16; j++) seqBytes[j] = byteData[0x30 + 0x10 * i + j];
                List<RNANFrame> frames = new List<RNANFrame>();
                for (int j = 0; j < frameCount; j++)
                {
                    int valuesPointer = HelperFunctions.ReadInt(byteData, seqStart + 0x8 * j) + frameValuesStart;
                    int duration = HelperFunctions.ReadShort(byteData, seqStart + 0x4 + 0x8 * j);
                    RNANFrame frame = new RNANFrame() { duration = duration };
                    frame.cellID = HelperFunctions.ReadShort(byteData, valuesPointer);
                    if (motionType == 2)
                    {
                        frame.xPos = (short)HelperFunctions.ReadShort(byteData, valuesPointer + 4);
                        frame.yPos = (short)HelperFunctions.ReadShort(byteData, valuesPointer + 6);
                    }
                    if (motionType == 1)
                    {
                        frame.rot = HelperFunctions.ReadShort(byteData, valuesPointer + 0x2) * 360 / 65536f;
                        frame.xScale = HelperFunctions.ReadInt(byteData, valuesPointer + 0x4) / 4096f;
                        frame.yScale = HelperFunctions.ReadInt(byteData, valuesPointer + 0x8) / 4096f;
                        frame.xPos = (short)HelperFunctions.ReadShort(byteData, valuesPointer + 0xC);
                        frame.yPos = (short)HelperFunctions.ReadShort(byteData, valuesPointer + 0xE);
                    }
                    frames.Add(frame);
                }
                sequences.Add(new RNANSequence() { sequenceBytes = seqBytes, motionType = motionType, cellType = cellType, loopType = loopType, frames = frames });
            }
        }

        public void WriteData()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(byteData.ToList().GetRange(0, 16));

            bytes.AddRange(new byte[] { (byte)'K', (byte)'N', (byte)'B', (byte)'A', 0, 0, 0, 0 });
            bytes.AddRange(BitConverter.GetBytes((short)sequences.Count));
            int totalFrames = 0;
            foreach (var seq in sequences) totalFrames += seq.frames.Count;
            bytes.AddRange(BitConverter.GetBytes((short)totalFrames));
            bytes.AddRange(new byte[] { 0x18, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });

            foreach (var seq in sequences)
            {
                seq.motionType = 0;
                foreach (var frame in seq.frames)
                {
                    if (frame.xScale != 1 || frame.yScale != 1 || frame.rot != 0) seq.motionType = 1;
                    else if (seq.motionType == 0 && (frame.xPos != 0 || frame.yPos != 0)) seq.motionType = 2;
                }

                bytes.AddRange(BitConverter.GetBytes(seq.frames.Count));
                bytes.AddRange(BitConverter.GetBytes((short)seq.motionType));
                bytes.AddRange(BitConverter.GetBytes((short)seq.cellType));
                bytes.AddRange(BitConverter.GetBytes(seq.loopType));
                bytes.AddRange(new byte[] { 0, 0, 0, 0 });
            }

            int frameStart = bytes.Count;
            HelperFunctions.WriteInt(bytes, 0x20, frameStart - 0x18);

            Dictionary<byte[], List<int>> frameDataStock = new Dictionary<byte[], List<int>>();
            foreach (var seq in sequences)
            {
                HelperFunctions.WriteInt(bytes, 0x3C + 0x10 * sequences.IndexOf(seq), bytes.Count - frameStart);
                foreach (var frame in seq.frames)
                {
                    bytes.AddRange(new byte[] { 0, 0, 0, 0 });
                    bytes.AddRange(BitConverter.GetBytes((short)frame.duration));
                    bytes.AddRange(new byte[] { 0xEF, 0xBE });

                    byte[] frameData = new byte[seq.motionType == 0 ? 4 : seq.motionType == 1 ? 16 : 8];
                    if (seq.motionType == 0)
                    {
                        HelperFunctions.WriteShort(frameData, 0, frame.cellID);
                        HelperFunctions.WriteShort(frameData, 2, 0xCCCC);
                    }
                    if (seq.motionType == 1)
                    {
                        HelperFunctions.WriteShort(frameData, 0, frame.cellID);
                        HelperFunctions.WriteShort(frameData, 2, (short)(frame.rot * 65536f / 360f));
                        HelperFunctions.WriteInt(frameData, 4, (short)(frame.xScale * 4096f));
                        HelperFunctions.WriteInt(frameData, 8, (short)(frame.yScale * 4096f));
                        HelperFunctions.WriteShort(frameData, 12, frame.xPos);
                        HelperFunctions.WriteShort(frameData, 14, frame.yPos);
                    }
                    if (seq.motionType == 2)
                    {
                        HelperFunctions.WriteShort(frameData, 0, frame.cellID);
                        HelperFunctions.WriteShort(frameData, 2, 0xBEEF);
                        HelperFunctions.WriteShort(frameData, 4, frame.xPos);
                        HelperFunctions.WriteShort(frameData, 6, frame.yPos);
                    }

                    if (!frameDataStock.Keys.Any(b => b.SequenceEqual(frameData))) frameDataStock.Add(frameData, new List<int>());
                    else frameData = frameDataStock.Keys.First(b => b.SequenceEqual(frameData));

                    frameDataStock[frameData].Add(bytes.Count - 8);
                }
            }

            int frameDataStart = bytes.Count;
            HelperFunctions.WriteInt(bytes, 0x24, frameDataStart - 0x18);

            foreach (var frameData in frameDataStock)
            {
                foreach (var ptr in frameData.Value)
                    HelperFunctions.WriteInt(bytes, ptr, bytes.Count - frameDataStart);
                bytes.AddRange(frameData.Key);
            }
            HelperFunctions.WriteInt(bytes, 0x14, bytes.Count - 0x10);

            int lbalPos = HelperFunctions.ReadInt(byteData, 0x14) + 16;
            bytes.AddRange(byteData.ToList().GetRange(lbalPos, byteData.Length - lbalPos));
            HelperFunctions.WriteInt(bytes, 0x8, bytes.Count);

            byteData = bytes.ToArray();
        }
    }

    public class RNANSequence
    {
        //Frame count at 0x0
        //Motion type (trans, rot, scale) at 0x4 (short)
        //Pointer at 0xc
        public byte[] sequenceBytes;
        public int motionType = 0;
        public int cellType = 2;
        public int loopType = 0;
        public List<RNANFrame> frames = new List<RNANFrame>();

        public RNANFrame GetFrameAtTime(int globalFrame)
        {
            int frame = 0;
            while (globalFrame >= frames[frame].duration)
            {
                globalFrame -= frames[frame].duration;
                frame++;
                if (frame >= frames.Count) frame = 0;
                if (frame == 0 && frames.All(f => f.duration == 0)) break;
            }
            return frames[frame];
        }
    }

    public class RNANFrame
    {
        public int duration = 6;
        public int cellID;
        public short xPos;
        public short yPos;
        public float xScale = 1;
        public float yScale = 1;
        public float rot;
    }

    public class RECNFile
    {
        int[,] tableSizeInTiles =
        {
            { 1, 2, 4, 8 },
            { 2, 4, 4, 8 },
            { 1, 1, 2, 4 },
            { 1, 2, 4, 8 }
        };

        //file size at 0x8
        public byte[] byteData;

        public RECNFile(byte[] byteData)
        {
            this.byteData = byteData;
        }

        public CellObject[] GetTilesForCell(int cellID)
        {
            if (cellID >= byteData[0x18]) return new CellObject[0];

            int tileCount = byteData[0x30 + 8 * cellID];

            CellObject[] tiles = new CellObject[tileCount];
            for (int i = 0; i < tileCount; i++)
            {
                int pos = 0x30 + 8 * byteData[0x18] + 6 * i + HelperFunctions.ReadInt(byteData, 0x34 + 8 * cellID);
                int y = (sbyte)byteData[pos];
                int x = (sbyte)byteData[pos + 2];
                int tile = HelperFunctions.ReadShort(byteData, pos + 4) & 0b_11_11111111;
                int spriteX = (tile % 32) * 8;
                int spriteY = (tile / 32) * 8;
                int keyShape = byteData[pos + 1] >> 6;
                int keySize = byteData[pos + 3] >> 6;
                int width = tableSizeInTiles[keyShape, keySize] * 8;
                int height = tableSizeInTiles[3 - keyShape, keySize] * 8;
                tiles[i] = new CellObject(spriteX, spriteY, x, y, tile, width, height);
            }

            return tiles;
        }
    }

    public class CellObject
    {
        public int spriteX;
        public int spriteY;
        public int xOffset;
        public int yOffset;
        public int tileID;
        public int width;
        public int height;

        public CellObject(int spriteX, int spriteY, int xOffset, int yOffset, int tileID, int width, int height)
        {
            this.spriteX = spriteX;
            this.spriteY = spriteY;
            this.xOffset = xOffset;
            this.yOffset = yOffset;
            this.tileID = tileID;
            this.width = width;
            this.height = height;
        }
    }

    public class RCMNFile
    {
        public byte[] byteData;
        public List<RCMNGroup> groups = new List<RCMNGroup>();

        public RCMNFile(byte[] byteData)
        {
            this.byteData = byteData;

            groups = new List<RCMNGroup>();

            int cellGroupStart = HelperFunctions.ReadInt(byteData, 0x1c) + 0x18;
            int cellPropertiesStart = HelperFunctions.ReadInt(byteData, 0x20) + 0x18;
            for (int i = 0; i < byteData[0x18]; i++)
            {
                RCMNGroup group = new RCMNGroup();

                for (int j = 0; j < byteData[cellGroupStart + 8 * i]; j++)
                {
                    RCMNCell cell = new RCMNCell()
                    {
                        sequenceID = HelperFunctions.ReadShort(byteData, cellPropertiesStart),
                        xOffset = (short)HelperFunctions.ReadShort(byteData, cellPropertiesStart + 2),
                        yOffset = (short)HelperFunctions.ReadShort(byteData, cellPropertiesStart + 4),
                        frameMode = byteData[cellPropertiesStart + 6],
                        id = byteData[cellPropertiesStart + 7],
                    };
                    group.cells.Add(cell);
                    cellPropertiesStart += 8;
                }

                groups.Add(group);
            }
        }

        public void WriteData()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(byteData.ToList().GetRange(0, 16));

            bytes.AddRange(new byte[] { (byte)'K', (byte)'B', (byte)'C', (byte)'M', 0, 0, 0, 0 });
            bytes.AddRange(BitConverter.GetBytes((short)groups.Count));
            bytes.AddRange(new byte[] { 0xEF, 0xBE });
            bytes.AddRange(new byte[] { 0x14, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });

            foreach (var group in groups)
            {
                bytes.AddRange(BitConverter.GetBytes((short)group.cells.Count));
                bytes.AddRange(BitConverter.GetBytes((short)group.cells.Count));
                bytes.AddRange(new byte[] { 0, 0, 0, 0 });
            }

            int groupStart = bytes.Count;
            HelperFunctions.WriteInt(bytes, 0x20, groupStart - 0x18);

            foreach (var group in groups)
            {
                HelperFunctions.WriteInt(bytes, 0x30 + 0x8 * groups.IndexOf(group), bytes.Count - groupStart);
                foreach (var cell in group.cells)
                {
                    cell.id = group.cells.IndexOf(cell);
                    bytes.AddRange(BitConverter.GetBytes((short)cell.sequenceID));
                    bytes.AddRange(BitConverter.GetBytes((short)cell.xOffset));
                    bytes.AddRange(BitConverter.GetBytes((short)cell.yOffset));
                    bytes.Add((byte)cell.frameMode);
                    bytes.Add((byte)cell.id);
                }
            }
            HelperFunctions.WriteInt(bytes, 0x14, bytes.Count - 0x10);

            byteData = bytes.ToArray();
        }
    }

    public class RCMNGroup
    {
        public List<RCMNCell> cells = new List<RCMNCell>();
    }

    public class RCMNCell
    {
        public int id;
        public int sequenceID;
        public short xOffset;
        public short yOffset;
        public int frameMode;
    }

    public class RAMNFile
    {
        public byte[] byteData;
        public List<RAMNSequence> sequences = new List<RAMNSequence>();

        public RAMNFile(byte[] byteData)
        {
            this.byteData = byteData;

            sequences = new List<RAMNSequence>();

            int groupStart = HelperFunctions.ReadInt(byteData, 0x1c) + 0x18;
            int framesStart = HelperFunctions.ReadInt(byteData, 0x20) + 0x18;
            int framePropertiesStart = HelperFunctions.ReadInt(byteData, 0x24) + 0x18;
            for (int i = 0; i < byteData[0x18]; i++)
            {
                RAMNSequence seq = new RAMNSequence();
                int frameCount = HelperFunctions.ReadInt(byteData, groupStart + 0x10 * i);
                seq.motionType = byteData[groupStart + 0x10 * i + 4];
                seq.cellType = byteData[groupStart + 0x10 * i + 6];
                seq.loopMode = byteData[groupStart + 0x10 * i + 8];
                int offset = HelperFunctions.ReadInt(byteData, groupStart + 0x10 * i + 12);
                for (int j = 0; j < frameCount; j++)
                {
                    int pos = framesStart + offset;
                    int propPos = HelperFunctions.ReadInt(byteData, pos + 8 * j) + framePropertiesStart;

                    RAMNSubsequence sub = new RAMNSubsequence()
                    {
                        duration = HelperFunctions.ReadShort(byteData, pos + 4 + 8 * j),
                        rcmnGroupID = HelperFunctions.ReadShort(byteData, propPos),
                    };
                    if (seq.motionType == 2)
                    {
                        sub.xPos = (short)HelperFunctions.ReadShort(byteData, propPos + 4);
                        sub.yPos = (short)HelperFunctions.ReadShort(byteData, propPos + 6);
                    }
                    if (seq.motionType == 1)
                    {
                        sub.rot = HelperFunctions.ReadShort(byteData, propPos + 0x2) * 360 / 65536f;
                        sub.xScale = HelperFunctions.ReadInt(byteData, propPos + 0x4) / 4096f;
                        sub.yScale = HelperFunctions.ReadInt(byteData, propPos + 0x8) / 4096f;
                        sub.xPos = (short)HelperFunctions.ReadShort(byteData, propPos + 0xC);
                        sub.yPos = (short)HelperFunctions.ReadShort(byteData, propPos + 0xE);
                    }
                    seq.subSequences.Add(sub);
                }

                sequences.Add(seq);
            }
        }

        public void WriteData()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(byteData.ToList().GetRange(0, 16));

            bytes.AddRange(new byte[] { (byte)'K', (byte)'N', (byte)'B', (byte)'A', 0, 0, 0, 0 });
            bytes.AddRange(BitConverter.GetBytes((short)sequences.Count));
            int totalFrames = 0;
            foreach (var seq in sequences) totalFrames += seq.subSequences.Count;
            bytes.AddRange(BitConverter.GetBytes((short)totalFrames));
            bytes.AddRange(new byte[] { 0x18, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });

            foreach (var seq in sequences)
            {
                seq.motionType = 0;
                foreach (var frame in seq.subSequences)
                {
                    if (frame.xScale != 1 || frame.yScale != 1 || frame.rot != 0) seq.motionType = 1;
                    else if (seq.motionType == 0 && (frame.xPos != 0 || frame.yPos != 0)) seq.motionType = 2;
                }

                bytes.AddRange(BitConverter.GetBytes(seq.subSequences.Count));
                bytes.AddRange(BitConverter.GetBytes((short)seq.motionType));
                bytes.AddRange(BitConverter.GetBytes((short)seq.cellType));
                bytes.AddRange(BitConverter.GetBytes(seq.loopMode));
                bytes.AddRange(new byte[] { 0, 0, 0, 0 });
            }

            int frameStart = bytes.Count;
            HelperFunctions.WriteInt(bytes, 0x20, frameStart - 0x18);

            Dictionary<byte[], List<int>> frameDataStock = new Dictionary<byte[], List<int>>();
            foreach (var seq in sequences)
            {
                HelperFunctions.WriteInt(bytes, 0x3C + 0x10 * sequences.IndexOf(seq), bytes.Count - frameStart);
                foreach (var frame in seq.subSequences)
                {
                    bytes.AddRange(new byte[] { 0, 0, 0, 0 });
                    bytes.AddRange(BitConverter.GetBytes((short)frame.duration));
                    bytes.AddRange(new byte[] { 0xEF, 0xBE });

                    byte[] frameData = new byte[seq.motionType == 0 ? 2 : seq.motionType == 1 ? 16 : 8];
                    if (seq.motionType == 0)
                    {
                        HelperFunctions.WriteShort(frameData, 0, frame.rcmnGroupID);
                    }
                    if (seq.motionType == 1)
                    {
                        HelperFunctions.WriteShort(frameData, 0, frame.rcmnGroupID);
                        HelperFunctions.WriteShort(frameData, 2, (short)(frame.rot * 65536f / 360f));
                        HelperFunctions.WriteInt(frameData, 4, (short)(frame.xScale * 4096f));
                        HelperFunctions.WriteInt(frameData, 8, (short)(frame.yScale * 4096f));
                        HelperFunctions.WriteShort(frameData, 12, frame.xPos);
                        HelperFunctions.WriteShort(frameData, 14, frame.yPos);
                    }
                    if (seq.motionType == 2)
                    {
                        HelperFunctions.WriteShort(frameData, 0, frame.rcmnGroupID);
                        HelperFunctions.WriteShort(frameData, 2, 0xBEEF);
                        HelperFunctions.WriteShort(frameData, 4, frame.xPos);
                        HelperFunctions.WriteShort(frameData, 6, frame.yPos);
                    }

                    if (!frameDataStock.Keys.Any(b => b.SequenceEqual(frameData))) frameDataStock.Add(frameData, new List<int>());
                    else frameData = frameDataStock.Keys.First(b => b.SequenceEqual(frameData));

                    frameDataStock[frameData].Add(bytes.Count - 8);
                }
            }

            int frameDataStart = bytes.Count;
            HelperFunctions.WriteInt(bytes, 0x24, frameDataStart - 0x18);

            foreach (var frameData in frameDataStock)
            {
                foreach (var ptr in frameData.Value)
                    HelperFunctions.WriteInt(bytes, ptr, bytes.Count - frameDataStart);
                bytes.AddRange(frameData.Key);
            }
            HelperFunctions.WriteInt(bytes, 0x14, bytes.Count - 0x10);

            int lbalPos = HelperFunctions.ReadInt(byteData, 0x14) + 16;
            bytes.AddRange(byteData.ToList().GetRange(lbalPos, byteData.Length - lbalPos));
            HelperFunctions.WriteInt(bytes, 0x8, bytes.Count);

            byteData = bytes.ToArray();
        }
    }

    public class RAMNSequence
    {
        public int motionType;
        public int cellType;
        public int loopMode;
        public List<RAMNSubsequence> subSequences = new List<RAMNSubsequence>();
    }

    public class RAMNSubsequence
    {
        public int duration;
        public int rcmnGroupID;
        public short xPos;
        public short yPos;
        public float xScale = 1;
        public float yScale = 1;
        public float rot;
    }

    public class RigCellsFile
    {
        public byte[] byteData;
        public List<RigCell> cells;
        public byte[] flags;

        public RigCellsFile(byte[] byteData)
        {
            this.byteData = byteData;
            cells = new List<RigCell>();

            for (int i = 0; i < byteData[0]; i++)
            {
                cells.Add(new RigCell()
                {
                    spriteX = HelperFunctions.ReadInt(byteData, 12 + i * 48) / 0x100,
                    spriteY = HelperFunctions.ReadInt(byteData, 16 + i * 48) / 0x100,
                    width = HelperFunctions.ReadInt(byteData, 20 + i * 48) / 0x1000,
                    height = HelperFunctions.ReadInt(byteData, 24 + i * 48) / 0x1000,
                    cellX = HelperFunctions.ReadInt(byteData, 28 + i * 48) / 0x1000,
                    cellY = HelperFunctions.ReadInt(byteData, 32 + i * 48) / 0x1000,
                    subCell = new RigCell()
                    {
                        spriteX = HelperFunctions.ReadInt(byteData, 36 + i * 48) / 0x100,
                        spriteY = HelperFunctions.ReadInt(byteData, 40 + i * 48) / 0x100,
                        width = HelperFunctions.ReadInt(byteData, 44 + i * 48) / 0x1000,
                        height = HelperFunctions.ReadInt(byteData, 48 + i * 48) / 0x1000,
                        cellX = HelperFunctions.ReadInt(byteData, 52 + i * 48) / 0x1000,
                        cellY = HelperFunctions.ReadInt(byteData, 56 + i * 48) / 0x1000,
                    }
                });
            }

            int flagPos = cells.Count * 48 + 12;
            if (byteData.Length > flagPos)
            {
                flags = new byte[byteData.Length - flagPos];
                for (int i = 0; i < flags.Length; i++)
                {
                    flags[i] = byteData[flagPos + i];
                }
            }
        }

        public void WriteData()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(new List<byte>(byteData).GetRange(0, 12));
            bytes[0] = (byte)cells.Count;
            for (int i = 0; i < cells.Count; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(cells[i].spriteX * 0x100));
                bytes.AddRange(BitConverter.GetBytes(cells[i].spriteY * 0x100));
                bytes.AddRange(BitConverter.GetBytes(cells[i].width * 0x1000));
                bytes.AddRange(BitConverter.GetBytes(cells[i].height * 0x1000));
                bytes.AddRange(BitConverter.GetBytes(cells[i].cellX * 0x1000));
                bytes.AddRange(BitConverter.GetBytes(cells[i].cellY * 0x1000));
                bytes.AddRange(BitConverter.GetBytes(cells[i].subCell.spriteX * 0x100));
                bytes.AddRange(BitConverter.GetBytes(cells[i].subCell.spriteY * 0x100));
                bytes.AddRange(BitConverter.GetBytes(cells[i].subCell.width * 0x1000));
                bytes.AddRange(BitConverter.GetBytes(cells[i].subCell.height * 0x1000));
                bytes.AddRange(BitConverter.GetBytes(cells[i].subCell.cellX * 0x1000));
                bytes.AddRange(BitConverter.GetBytes(cells[i].subCell.cellY * 0x1000));
            }
            bytes.AddRange(flags);
            byteData = bytes.ToArray();
        }
    }

    public class RigCell
    {
        public int cellX;
        public int cellY;
        public int width;
        public int height;
        public int spriteX;
        public int spriteY;
        public RigCell subCell;

        public Rectangle ToRectangle(int scale)
        {
            return new Rectangle(cellX * scale, cellY * scale, width * scale, height * scale);
        }
    }
}
