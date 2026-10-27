using DSDecmp;
using NewEditor.Data.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

//Adapted from DSPRE

namespace NewEditor.Data.Mapping
{
    public static class MappingUtils
    {
        public static void ModelToDAE(string modelName, byte[] modelData, byte[] textureData)
        {
            FolderBrowserDialog cofd = new FolderBrowserDialog();
            if (cofd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            string outDir = Path.Combine(cofd.SelectedPath, modelName);

            if (Directory.Exists(outDir))
            {
                if (Directory.GetFiles(outDir).Length > 0)
                {
                    DialogResult d = MessageBox.Show($"Directory \"{outDir}\" already exists and is not empty.\nIts contents will be lost.\n\nDo you want to proceed?", "Directory not empty", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (d.Equals(DialogResult.No))
                    {
                        return;
                    }
                    else
                    {
                        Directory.Delete(outDir, recursive: true);
                    }
                }
                else
                {
                    Directory.Delete(outDir, recursive: true);
                }
            }
            string tempNSBMDPath = outDir + "_temp.nsbmd";

            if (textureData != null && textureData.Length > 0)
            {
                modelData = BuildNSBMDwithTextures(modelData, textureData);
            }

            File.WriteAllBytes(tempNSBMDPath, modelData);

            /* Check correct creation of temp NSBMD file*/
            if (!File.Exists(tempNSBMDPath))
            {
                MessageBox.Show("NSBMD file corresponding to this map could not be found.\nAborting", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Process apicula = new Process();
            apicula.StartInfo.FileName = @"apicula.exe";
            apicula.StartInfo.Arguments = $" convert \"{tempNSBMDPath}\" --output \"{outDir}\"";
            apicula.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            apicula.StartInfo.CreateNoWindow = true;
            apicula.Start();
            apicula.WaitForExit();

            if (File.Exists(tempNSBMDPath))
            {
                File.Delete(tempNSBMDPath);

                if (File.Exists(tempNSBMDPath))
                {
                    MessageBox.Show("Temporary NSBMD file deletion failed.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("Temporary NSBMD file corresponding to this map disappeared.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (apicula.ExitCode == 0)
            {
                MessageBox.Show("NSBMD was exported and converted successfully!", "Operation successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("NSBMD to DAE conversion failed.", "Apicula error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static byte[] BuildNSBMDwithTextures(byte[] nsbmd, byte[] nsbtx)
        {
            byte[] wholeMDL0 = GetFirstBlock(nsbmd);
            byte[] wholeTEX0 = GetFirstBlock(nsbtx);

            MemoryStream ms = new MemoryStream();
            using (BinaryWriter msWriter = new BinaryWriter(ms))
            {
                msWriter.Write((uint)0x30444d42);
                msWriter.Write((ushort)0xfeff);
                msWriter.Write((ushort)0x2);

                ushort nBlocks = 2;
                uint modelLength = (uint)(wholeMDL0.Length + 16 + 4 * nBlocks);
                msWriter.Write((uint)(modelLength + wholeTEX0.Length));
                msWriter.Write((ushort)16); //Header size, always 16
                msWriter.Write(nBlocks); //Number of blocks, now it's 2 because we are inserting textures

                msWriter.Write((uint)(msWriter.BaseStream.Position + 4 * nBlocks)); //Absolute offset to model data. We are gonna have to write two offsets

                msWriter.Write(modelLength); //Copy offset to TEX0
                msWriter.Write(wholeMDL0);
                msWriter.Write(wholeTEX0);
            }
            return ms.ToArray();
        }
        private static byte[] GetFirstBlock(byte[] NSBFile)
        {
            int blockSize;
            uint offsetToMainBlock;
            using (BinaryReader reader = new BinaryReader(new MemoryStream(NSBFile)))
            {
                reader.BaseStream.Position = 16;
                offsetToMainBlock = reader.ReadUInt32();

                reader.BaseStream.Position = offsetToMainBlock + 4;
                blockSize = reader.ReadInt32();
            }
            byte[] blockData = new byte[blockSize];
            Buffer.BlockCopy(NSBFile, (int)offsetToMainBlock, blockData, 0, blockSize);

            return blockData;
        }

        public static byte[] GetModelWithoutTextures(byte[] modelFile)
        {
            byte[] nsbmdHeaderData;
            uint mdl0Size;
            byte[] mdl0Data;

            using (BinaryReader modelReader = new BinaryReader(new MemoryStream(modelFile)))
            {
                modelReader.BaseStream.Position = 0x0;
                nsbmdHeaderData = modelReader.ReadBytes(0x8);

                modelReader.BaseStream.Position = 0x1C;
                mdl0Size = modelReader.ReadUInt32(); // Read mdl0 file size

                modelReader.BaseStream.Position = 0x18;
                mdl0Data = modelReader.ReadBytes((int)mdl0Size);
            }

            MemoryStream output = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(output))
            {

                writer.Write(nsbmdHeaderData); // Write first header bytes, same for all NSBMD.
                writer.Write(mdl0Size + 0x14);
                writer.Write((short)0x10); // Writes BMD0 header size (always 16)
                writer.Write((short)0x1); // Write new number of sub-files, since embedded textures are removed
                writer.Write((uint)0x14); // Writes new start offset of MDL0

                writer.Write(mdl0Data); // Writes MDL0;
            }
            return output.ToArray();
        }
        public static string ReadNSBMDString(EndianBinaryReader reader)
        {
            var str = new String(reader.ReadChars(Encoding.ASCII, 16));
            str = str.Replace("\0", "");

            return str;
        }
    }
}
