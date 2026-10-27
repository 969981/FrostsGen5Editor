using NewEditor.Data;
using NewEditor.Data.NARCTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace NewEditor.Forms
{
    public partial class FileExplorer : Form
    {
        FileNameTable fnt = MainEditor.fileSystem.fnt;
        Dictionary<string, TreeNode> nodes = new Dictionary<string, TreeNode>();
        Dictionary<TreeNode, string> reverseNodes = new Dictionary<TreeNode, string>();

        public FileExplorer()
        {
            InitializeComponent();

            List<string> files = MainEditor.fileSystem.fnt.fileNames.Values.ToList();
            files.Sort();

            for (int i = 0; i < MainEditor.fileSystem.overlays.Count; i++)
            {
                AddNode("overlays/ov_" + i.ToString("D3"), true);
            }
            AddNode("ftc/header.bin", true);
            AddNode("ftc/arm9.bin", true);
            AddNode("ftc/y9.bin", true);
            AddNode("ftc/arm7.bin", true);
            AddNode("ftc/fnt.bin", true);
            AddNode("ftc/fat.bin", true);
            AddNode("ftc/banner.bin", true);
            foreach (string str in files)
            {
                AddNode(str, true);
            }
        }

        void AddNode(string path, bool leafNode)
        {
            if (path.Contains("ftc/"))
            {
                string sub = path.Substring(0, path.LastIndexOf('/'));
                if (!nodes.ContainsKey(sub)) AddNode(sub, false);

                TreeNode node = new TreeNode(path.Substring(path.LastIndexOf('/') + 1));
                node.ImageIndex = 2;
                node.SelectedImageIndex = node.ImageIndex;
                nodes.Add(path, node);
                reverseNodes.Add(node, path);
                nodes[sub].Nodes.Add(node);
            }
            else if (path.Contains('/'))
            {
                string sub = path.Substring(0, path.LastIndexOf('/'));
                if (!nodes.ContainsKey(sub)) AddNode(sub, false);

                TreeNode node = new TreeNode(path.Substring(path.LastIndexOf('/') + 1));
                node.ImageIndex = path.Contains("ov_") ? 2 : !leafNode ? 0 : MainEditor.fileSystem.files[fnt.reverseFileNames[path] - fnt.firstFile] is NARC narc && narc.byteData[0] == (byte)'N' ? 1 : 2;
                node.SelectedImageIndex = node.ImageIndex;
                nodes.Add(path, node);
                reverseNodes.Add(node, path);
                nodes[sub].Nodes.Add(node);
            }
            else
            {
                TreeNode node = new TreeNode(path);
                node.ImageIndex = !leafNode ? 0 : MainEditor.fileSystem.files[fnt.reverseFileNames[path] - fnt.firstFile] is NARC narc && narc.byteData[0] == (byte)'N' ? 1 : 2;
                node.SelectedImageIndex = node.ImageIndex;
                nodes.Add(path, node);
                reverseNodes.Add(node, path);
                fileTree.Nodes.Add(node);
            }
        }

        private void fileTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (fileTree.SelectedNode != null && fileTree.SelectedNode.Nodes.Count == 0)
            {
                if (fileTree.SelectedNode.Text.Contains("arm9.bin"))
                {
                    var obj = MainEditor.fileSystem.arm9;
                    string text = "Type: arm9";
                    text += "\nSize: 0x" + obj.Count.ToString("X");
                    text += "\nCompressed: " + (obj.Count < 600000).ToString();

                    fileText.Text = text;
                    compressOverlayButton.Visible = true;
                    detectCompressionCheckBox.Visible = false;
                    unpackNarcButton.Visible = false;
                    packNarcButton.Visible = false;
                    compressOverlayButton.Text = obj.Count < 600000 ? "Decompress" : "Compress";
                }
                else if (fileTree.SelectedNode.Text.Contains("y9.bin"))
                {
                    var obj = MainEditor.fileSystem.y9;
                    string text = "Type: y9";
                    text += "\nSize: 0x" + obj.bytes.Length.ToString("X");
                    text += "\nEntries: " + obj.entries.Count;

                    fileText.Text = text;
                    compressOverlayButton.Visible = false;
                    detectCompressionCheckBox.Visible = false;
                    unpackNarcButton.Visible = false;
                    packNarcButton.Visible = false;
                }
                else if (fileTree.SelectedNode.Text.Contains("arm7.bin"))
                {
                    var obj = MainEditor.fileSystem.arm7;
                    string text = "Type: arm7";
                    text += "\nSize: 0x" + obj.Count.ToString("X");

                    fileText.Text = text;
                    compressOverlayButton.Visible = false;
                    detectCompressionCheckBox.Visible = false;
                    unpackNarcButton.Visible = false;
                    packNarcButton.Visible = false;
                }
                else if (fileTree.SelectedNode.Text.Contains("fnt.bin"))
                {
                    var obj = MainEditor.fileSystem.fnt;
                    string text = "Type: File Name Table";
                    text += "\nSize: 0x" + obj.data.Length.ToString("X");

                    fileText.Text = text;
                    compressOverlayButton.Visible = false;
                    detectCompressionCheckBox.Visible = false;
                    unpackNarcButton.Visible = false;
                    packNarcButton.Visible = false;
                }
                else if (fileTree.SelectedNode.Text.Contains("fat.bin"))
                {
                    var obj = MainEditor.fileSystem.fat;
                    string text = "Type: File Association Table";
                    text += "\nSize: 0x" + obj.Count.ToString("X");

                    fileText.Text = text;
                    compressOverlayButton.Visible = false;
                    detectCompressionCheckBox.Visible = false;
                    unpackNarcButton.Visible = false;
                    packNarcButton.Visible = false;
                }
                else if (fileTree.SelectedNode.Text.Contains("header.bin"))
                {
                    var obj = MainEditor.fileSystem.romHeader;
                    string text = "Type: Rom Header";
                    text += "\nSize: 0x" + obj.Count.ToString("X");

                    fileText.Text = text;
                    compressOverlayButton.Visible = false;
                    detectCompressionCheckBox.Visible = false;
                    unpackNarcButton.Visible = false;
                    packNarcButton.Visible = false;
                }
                else if (fileTree.SelectedNode.Text.Contains("banner.bin"))
                {
                    var obj = MainEditor.fileSystem.banner;
                    string text = "Type: Banner";
                    text += "\nSize: 0x" + obj.Count.ToString("X");

                    fileText.Text = text;
                    compressOverlayButton.Visible = false;
                    detectCompressionCheckBox.Visible = false;
                    unpackNarcButton.Visible = false;
                    packNarcButton.Visible = false;
                }
                else if (fileTree.SelectedNode.Text.Contains("ov_"))
                {
                    int id = int.Parse(fileTree.SelectedNode.Text.Substring(3));
                    var y9 = MainEditor.fileSystem.y9.entries[id];
                    var obj = MainEditor.fileSystem.overlays[id];
                    string text = "Type: Overlay\nID: " + id.ToString("D3");

                    text += "\n\nRAM Address: 0x" + y9.mountAddress.ToString("X");
                    text += "\nRAM Size: 0x" + y9.mountSize.ToString("X");
                    text += "\nCompressed Size: 0x" + y9.compressedSize.ToString("X");
                    text += "\nCompressed: " + y9.compressed;

                    text += "\n\nFile Size: 0x" + obj.Count.ToString("X");

                    fileText.Text = text;
                    compressOverlayButton.Visible = true;
                    detectCompressionCheckBox.Visible = true;
                    unpackNarcButton.Visible = false;
                    packNarcButton.Visible = false;
                    compressOverlayButton.Text = y9.compressed ? "Decompress" : "Compress";
                }
                else
                {
                    var obj = MainEditor.fileSystem.files[fnt.reverseFileNames[reverseNodes[fileTree.SelectedNode]] - fnt.firstFile];

                    if (obj is NARC narc && narc.byteData[0] == (byte)'N')
                    {
                        string text = "Type: NARC\nPath: " + reverseNodes[fileTree.SelectedNode];

                        text += "\n\nSize: 0x" + narc.byteData.Length.ToString("X");
                        text += "\nFile Count: " + narc.numFileEntries;

                        fileText.Text = text;

                        unpackNarcButton.Visible = true;
                        packNarcButton.Visible = true;
                    }
                    else
                    {
                        fileText.Text = "Type: File\nPath: " + reverseNodes[fileTree.SelectedNode];
                        if (obj is byte[] b) fileText.Text += "\nSize: 0x" + b.Length.ToString("X");
                        unpackNarcButton.Visible = false;
                        packNarcButton.Visible = false;
                    }
                    compressOverlayButton.Visible = false;
                    detectCompressionCheckBox.Visible = false;
                }
                importButton.Enabled = true;
                extractButton.Enabled = true;
            }
            else if (fileTree.SelectedNode != null)
            {
                fileText.Text = "Type: Folder\nPath: " + reverseNodes[fileTree.SelectedNode];
                importButton.Enabled = false;
                extractButton.Enabled = false;
                compressOverlayButton.Visible = false;
                unpackNarcButton.Visible = false;
                packNarcButton.Visible = false;
            }
        }

        private void extractButton_Click(object sender, EventArgs e)
        {
            if (fileTree.SelectedNode != null && fileTree.SelectedNode.Nodes.Count == 0)
            {
                if (fileTree.SelectedNode.Text.Contains("arm9.bin"))
                {
                    SaveFileDialog save = new SaveFileDialog();
                    save.FileName = "arm9.bin";

                    if (save.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllBytes(save.FileName, MainEditor.fileSystem.arm9.ToArray());
                        statusText.Text = "Extracted arm9 - " + DateTime.Now.StatusText();
                    }
                }
                else if (fileTree.SelectedNode.Text.Contains("y9.bin"))
                {
                    SaveFileDialog save = new SaveFileDialog();
                    save.FileName = "y9.bin";

                    if (save.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllBytes(save.FileName, MainEditor.fileSystem.y9.bytes.ToArray());
                        statusText.Text = "Extracted y9 - " + DateTime.Now.StatusText();
                    }
                }
                else if (fileTree.SelectedNode.Text.Contains("arm7.bin"))
                {
                    SaveFileDialog save = new SaveFileDialog();
                    save.FileName = "arm7.bin";

                    if (save.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllBytes(save.FileName, MainEditor.fileSystem.arm7.ToArray());
                        statusText.Text = "Extracted arm7 - " + DateTime.Now.StatusText();
                    }
                }
                else if (fileTree.SelectedNode.Text.Contains("fnt.bin"))
                {
                    SaveFileDialog save = new SaveFileDialog();
                    save.FileName = "fnt.bin";

                    if (save.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllBytes(save.FileName, MainEditor.fileSystem.fnt.data.ToArray());
                        statusText.Text = "Extracted fnt - " + DateTime.Now.StatusText();
                    }
                }
                else if (fileTree.SelectedNode.Text.Contains("fat.bin"))
                {
                    SaveFileDialog save = new SaveFileDialog();
                    save.FileName = "fat.bin";

                    if (save.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllBytes(save.FileName, MainEditor.fileSystem.fat.ToArray());
                        statusText.Text = "Extracted fat - " + DateTime.Now.StatusText();
                    }
                }
                else if (fileTree.SelectedNode.Text.Contains("header.bin"))
                {
                    SaveFileDialog save = new SaveFileDialog();
                    save.FileName = "header.bin";

                    if (save.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllBytes(save.FileName, MainEditor.fileSystem.romHeader.ToArray());
                        statusText.Text = "Extracted header - " + DateTime.Now.StatusText();
                    }
                }
                else if (fileTree.SelectedNode.Text.Contains("banner.bin"))
                {
                    SaveFileDialog save = new SaveFileDialog();
                    save.FileName = "banner.bin";

                    if (save.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllBytes(save.FileName, MainEditor.fileSystem.banner.ToArray());
                        statusText.Text = "Extracted banner - " + DateTime.Now.StatusText();
                    }
                }
                else if (fileTree.SelectedNode.Text.Contains("ov_"))
                {
                    int id = int.Parse(fileTree.SelectedNode.Text.Substring(3));

                    SaveFileDialog save = new SaveFileDialog();
                    save.FileName = "ov_" + id.ToString("D3") + ".bin";

                    if (save.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllBytes(save.FileName, MainEditor.fileSystem.overlays[id].ToArray());
                        statusText.Text = "Extracted Overlay " + id + " - " + DateTime.Now.StatusText();
                    }
                }
                else
                {
                    var obj = MainEditor.fileSystem.files[fnt.reverseFileNames[reverseNodes[fileTree.SelectedNode]] - fnt.firstFile];

                    if (obj is NARC narc)
                    {
                        SaveFileDialog save = new SaveFileDialog();
                        save.FileName = fileTree.SelectedNode.Text;
                        if (save.ShowDialog() == DialogResult.OK)
                        {
                            narc.WriteData();
                            File.WriteAllBytes(save.FileName, narc.byteData);

                            statusText.Text = "Extracted narc " + reverseNodes[fileTree.SelectedNode] + " - " + DateTime.Now.StatusText();
                        }
                    }
                    else if (obj is byte[] b)
                    {
                        SaveFileDialog save = new SaveFileDialog();
                        save.FileName = fileTree.SelectedNode.Text;
                        if (save.ShowDialog() == DialogResult.OK)
                        {
                            File.WriteAllBytes(save.FileName, b);
                            statusText.Text = "Extracted file " + fileTree.SelectedNode.Text + " - " + DateTime.Now.StatusText();
                        }
                    }
                }
            }
        }

        private void compressOverlayButton_Click(object sender, EventArgs e)
        {
            if (fileTree.SelectedNode != null && fileTree.SelectedNode.Nodes.Count == 0 && fileTree.SelectedNode.Text.Contains("arm9.bin"))
            {
                var arm = MainEditor.fileSystem.arm9;
                bool compressed = arm.Count < 600000;

                if (compressed)
                {
                    MainEditor.fileSystem.arm9 = new List<byte>(BLZDecoder.BLZ_DecodePub(arm.ToArray()));
                    statusText.Text = "Decompressed arm9 - " + DateTime.Now.StatusText();
                }
                else
                {
                    MainEditor.fileSystem.arm9 = new List<byte>(BLZDecoder.BLZ_EncodePub(arm.ToArray(), true));
                    statusText.Text = "Compressed arm9 - " + DateTime.Now.StatusText();
                }

                fileTree_AfterSelect(null, null);
            }
            else if (fileTree.SelectedNode != null && fileTree.SelectedNode.Nodes.Count == 0 && fileTree.SelectedNode.Text.Contains("ov_"))
            {
                int id = int.Parse(fileTree.SelectedNode.Text.Substring(3));
                var y9 = MainEditor.fileSystem.y9.entries[id];
                var obj = MainEditor.fileSystem.overlays[id];

                if (y9.compressed)
                {
                    MainEditor.fileSystem.overlays[id] = new List<byte>(BLZDecoder.BLZ_DecodePub(obj.ToArray()));
                    y9.compressed = false;
                    y9.compressedSize = 0;
                    y9.Apply();
                    statusText.Text = "Decompressed Overlay " + id + " - " + DateTime.Now.StatusText();
                }
                else
                {
                    MainEditor.fileSystem.overlays[id] = new List<byte>(BLZDecoder.BLZ_EncodePub(obj.ToArray(), true));
                    y9.compressed = true;
                    y9.compressedSize = MainEditor.fileSystem.overlays[id].Count;
                    y9.Apply();
                    statusText.Text = "Compressed Overlay " + id + " - " + DateTime.Now.StatusText();
                }

                fileTree_AfterSelect(null, null);
            }
        }

        private void importButton_Click(object sender, EventArgs e)
        {
            if (fileTree.SelectedNode != null && fileTree.SelectedNode.Nodes.Count == 0)
            {
                if (fileTree.SelectedNode.Text.Contains("arm9.bin"))
                {
                    OpenFileDialog open = new OpenFileDialog();
                    open.FileName = "arm9.bin";
                    if (open.ShowDialog() == DialogResult.OK)
                    {
                        byte[] b = File.ReadAllBytes(open.FileName);
                        if (b.Length != MainEditor.fileSystem.arm9.Count && MessageBox.Show("The selected file's length does not match the length of the rom's file.\nIs this intended?", "File size mismatch!", MessageBoxButtons.YesNo) == DialogResult.No)
                            return;
                        MainEditor.fileSystem.arm9 = b.ToList();
                        statusText.Text = "Imported arm9 - " + DateTime.Now.StatusText();
                    }
                }
                else if (fileTree.SelectedNode.Text.Contains("y9.bin"))
                {
                    OpenFileDialog open = new OpenFileDialog();
                    open.FileName = "y9.bin";
                    if (open.ShowDialog() == DialogResult.OK)
                    {
                        byte[] b = File.ReadAllBytes(open.FileName);
                        if (b.Length != MainEditor.fileSystem.y9.bytes.Length && MessageBox.Show("The selected file's length does not match the length of the rom's file.\nIs this intended?", "File size mismatch!", MessageBoxButtons.YesNo) == DialogResult.No)
                            return;
                        MainEditor.fileSystem.y9 = new Y9Table(b);
                        statusText.Text = "Imported y9 - " + DateTime.Now.StatusText();
                    }
                }
                else if (fileTree.SelectedNode.Text.Contains("arm7.bin"))
                {
                    OpenFileDialog open = new OpenFileDialog();
                    open.FileName = "arm7.bin";
                    if (open.ShowDialog() == DialogResult.OK)
                    {
                        byte[] b = File.ReadAllBytes(open.FileName);
                        if (b.Length != MainEditor.fileSystem.arm7.Count && MessageBox.Show("The selected file's length does not match the length of the rom's file.\nIs this intended?", "File size mismatch!", MessageBoxButtons.YesNo) == DialogResult.No)
                            return;
                        MainEditor.fileSystem.arm7 = b.ToList();
                        statusText.Text = "Imported arm7 - " + DateTime.Now.StatusText();
                    }
                }
                else if (fileTree.SelectedNode.Text.Contains("fnt.bin"))
                {
                    OpenFileDialog open = new OpenFileDialog();
                    open.FileName = "fnt.bin";
                    if (open.ShowDialog() == DialogResult.OK)
                    {
                        byte[] b = File.ReadAllBytes(open.FileName);
                        if (b.Length != MainEditor.fileSystem.fnt.data.Length && MessageBox.Show("The selected file's length does not match the length of the rom's file.\nIs this intended?", "File size mismatch!", MessageBoxButtons.YesNo) == DialogResult.No)
                            return;
                        MainEditor.fileSystem.fnt = new FileNameTable(b);
                        statusText.Text = "Imported fnt - " + DateTime.Now.StatusText();
                    }
                }
                else if (fileTree.SelectedNode.Text.Contains("fat.bin"))
                {
                    OpenFileDialog open = new OpenFileDialog();
                    open.FileName = "fat.bin";
                    if (open.ShowDialog() == DialogResult.OK)
                    {
                        byte[] b = File.ReadAllBytes(open.FileName);
                        if (b.Length != MainEditor.fileSystem.fat.Count && MessageBox.Show("The selected file's length does not match the length of the rom's file.\nIs this intended?", "File size mismatch!", MessageBoxButtons.YesNo) == DialogResult.No)
                            return;
                        MainEditor.fileSystem.fat = b.ToList();
                        statusText.Text = "Imported fat - " + DateTime.Now.StatusText();
                    }
                }
                else if (fileTree.SelectedNode.Text.Contains("header.bin"))
                {
                    OpenFileDialog open = new OpenFileDialog();
                    open.FileName = "header.bin";
                    if (open.ShowDialog() == DialogResult.OK)
                    {
                        byte[] b = File.ReadAllBytes(open.FileName);
                        if (b.Length != MainEditor.fileSystem.romHeader.Count && MessageBox.Show("The selected file's length does not match the length of the rom's file.\nIs this intended?", "File size mismatch!", MessageBoxButtons.YesNo) == DialogResult.No)
                            return;
                        MainEditor.fileSystem.romHeader = b.ToList();
                        statusText.Text = "Imported header - " + DateTime.Now.StatusText();
                    }
                }
                else if (fileTree.SelectedNode.Text.Contains("banner.bin"))
                {
                    OpenFileDialog open = new OpenFileDialog();
                    open.FileName = "banner.bin";
                    if (open.ShowDialog() == DialogResult.OK)
                    {
                        byte[] b = File.ReadAllBytes(open.FileName);
                        if (b.Length != MainEditor.fileSystem.banner.Count && MessageBox.Show("The selected file's length does not match the length of the rom's file.\nIs this intended?", "File size mismatch!", MessageBoxButtons.YesNo) == DialogResult.No)
                            return;
                        MainEditor.fileSystem.banner = b.ToList();
                        statusText.Text = "Imported banner - " + DateTime.Now.StatusText();
                    }
                }
                else if (fileTree.SelectedNode.Text.Contains("ov_"))
                {
                    int id = int.Parse(fileTree.SelectedNode.Text.Substring(3));
                    var y9 = MainEditor.fileSystem.y9.entries[id];

                    OpenFileDialog open = new OpenFileDialog();
                    open.FileName = "ov_" + id.ToString("D3") + ".bin";

                    if (open.ShowDialog() == DialogResult.OK)
                    {
                        byte[] b = File.ReadAllBytes(open.FileName);

                        if (b.Length != MainEditor.fileSystem.overlays[id].Count && MessageBox.Show("The selected file's length does not match the length of the rom's file.\nIs this intended?", "File size mismatch!", MessageBoxButtons.YesNo) == DialogResult.No)
                            return;

                        bool shouldCompress = false;
                        if (!detectCompressionCheckBox.Checked) shouldCompress = y9.compressed;
                        else shouldCompress = HelperFunctions.ReadInt(b, b.Length - 4) > 0;
                        if (shouldCompress)
                        {
                            y9.compressedSize = b.Length;
                            y9.compressed = true;
                        }
                        else
                        {
                            y9.mountSize = b.Length;
                            y9.compressed = false;
                            y9.compressedSize = 0;
                        }
                        y9.Apply();

                        MainEditor.fileSystem.overlays[id] = b.ToList();
                        statusText.Text = "Imported Overlay " + id + " - " + DateTime.Now.StatusText();
                    }
                }
                else
                {
                    var obj = MainEditor.fileSystem.files[fnt.reverseFileNames[reverseNodes[fileTree.SelectedNode]] - fnt.firstFile];

                    if (obj is NARC narc)
                    {
                        OpenFileDialog open = new OpenFileDialog();
                        open.FileName = fileTree.SelectedNode.Text;
                        if (open.ShowDialog() == DialogResult.OK)
                        {
                            byte[] b = File.ReadAllBytes(open.FileName);
                            narc.byteData = b;
                            narc.ReadData();

                            statusText.Text = "Imported narc " + reverseNodes[fileTree.SelectedNode] + " - " + DateTime.Now.StatusText();
                        }
                    }
                    else if (obj is byte[] b)
                    {
                        OpenFileDialog open = new OpenFileDialog();
                        open.FileName = fileTree.SelectedNode.Text;
                        if (open.ShowDialog() == DialogResult.OK)
                        {
                            MainEditor.fileSystem.files[fnt.reverseFileNames[reverseNodes[fileTree.SelectedNode]] - fnt.firstFile] = File.ReadAllBytes(open.FileName);
                            statusText.Text = "Imported file " + fileTree.SelectedNode.Text + " - " + DateTime.Now.StatusText();
                        }
                    }
                }
            }

            fileTree_AfterSelect(null, null);
        }

        private void unpackNarcButton_Click(object sender, EventArgs e)
        {
            if (fileTree.SelectedNode != null && fileTree.SelectedNode.Nodes.Count == 0 &&
                MainEditor.fileSystem.files[fnt.reverseFileNames[reverseNodes[fileTree.SelectedNode]] - fnt.firstFile] is NARC narc && narc.byteData[0] == (byte)'N')
            {
                FolderBrowserDialog prompt = new FolderBrowserDialog();
                if (prompt.ShowDialog() == DialogResult.OK)
                {
                    if (!Directory.Exists(prompt.SelectedPath + "/" + fileTree.SelectedNode.Text))
                        Directory.CreateDirectory(prompt.SelectedPath + "/" + fileTree.SelectedNode.Text);

                    if (narc is ScriptNARC sn)
                    {
                        if (MessageBox.Show("Would you like to extract the decompiled script files instead of binary files?", "Extract Scripts", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            int commandNames = 0;
                            Form prompt2 = new Form();
                            prompt2.Font = Font;
                            prompt2.Width = 320;
                            prompt2.Height = 200;
                            prompt2.Text = "Script Export";
                            FlowLayoutPanel panel = new FlowLayoutPanel();
                            panel.Width = 320;
                            panel.Height = 200;
                            panel.Controls.Add(new Label() { Text = "Select which command dictionary to export with.", Width = 360 });
                            RadioButton[] options = new RadioButton[]
                            {
                                new RadioButton() { Text = "Frost's", Checked = MainEditor.RomType == RomType.BW2, Enabled = MainEditor.RomType == RomType.BW2 },
                                new RadioButton() { Text = "BeaterScript", Checked = MainEditor.RomType != RomType.BW2 },
                                new RadioButton() { Text = "Custom", Checked = false },
                            };
                            Button ok = new Button() { Text = "Confirm", Size = new Size(80, 28) };
                            ok.Click += (s, ev) =>
                            {
                                commandNames = options[0].Checked ? 0 : options[1].Checked ? 1 : 2;
                                prompt2.Close();
                            };
                            Button no = new Button() { Text = "Cancel", Size = new Size(80, 28) };
                            no.Click += (s, ev) => { commandNames = -1; prompt2.Close(); };
                            foreach (RadioButton r in options)
                            {
                                r.Width = 180;
                                panel.Controls.Add(r);
                                panel.SetFlowBreak(r, true);
                            }
                            panel.Controls.Add(ok);
                            panel.Controls.Add(no);
                            prompt2.Controls.Add(panel);
                            prompt2.StartPosition = FormStartPosition.CenterParent;
                            prompt2.ShowDialog();

                            if (commandNames == -1) return;

                            Dictionary<int, Data.NARCTypes.CommandType> baseCommands = new Dictionary<int, Data.NARCTypes.CommandType>(MainEditor.RomType == RomType.BW1 ? CommandReference.bw1CommandList : commandNames == 0 ? CommandReference.bw2CommandList : commandNames == 1 ? CommandReference.bw2BeaterScriptCommandList : CommandReference.customCommandList);

                            if (commandNames == 2)
                            {
                                OpenFileDialog open = new OpenFileDialog();
                                open.FileName = "ScriptCommands.json";

                                if (open.ShowDialog() == DialogResult.OK)
                                {
                                    Dictionary<int, Data.NARCTypes.CommandType> comDefs = new Dictionary<int, Data.NARCTypes.CommandType>();

                                    try
                                    {
                                        JsonDocument json = JsonDocument.Parse(File.ReadAllText(open.FileName));
                                        foreach (JsonProperty com in json.RootElement.EnumerateObject())
                                        {
                                            if (int.TryParse(com.Name.Substring(com.Name.IndexOf("x") + 1), System.Globalization.NumberStyles.HexNumber, null, out int id) &&
                                                com.Value.TryGetProperty("name", out JsonElement name) && com.Value.TryGetProperty("parameters", out JsonElement pars))
                                            {
                                                int[] paramInts = pars.EnumerateArray().Count() == 0 ? new int[0] : JsonSerializer.Deserialize<int[]>(pars);
                                                comDefs.Add(id, new Data.NARCTypes.CommandType(name.GetString(), paramInts.Length, paramInts));
                                            }
                                            else
                                            {
                                                MessageBox.Show("Error reading definition for command: \"" + com.Name + "\"");
                                                return;
                                            }
                                        }

                                        baseCommands = comDefs;
                                    }
                                    catch
                                    {
                                        MessageBox.Show("Unable to retrieve custom command dictionary.");
                                        return;
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("Unable to retrieve custom command dictionary.");
                                    return;
                                }
                            }

                            for (int i = 0; i < sn.scriptFiles.Count; i++)
                            {
                                if (!sn.scriptFiles[i].valid) continue;
                                List<string> headers = new List<string>()
                                {
                                    MainEditor.RomType == RomType.BW1 ? "ScriptHeaders/ScriptCommandsBW1.h" : "ScriptHeaders/FrostScriptCommandsBW2.h",
                                    "ScriptHeaders/MovementCommands.h"
                                };
                                int ow = MainEditor.zoneDataNarc.zones.FindIndex(z => z.scriptFile == i);

                                CommandReference.commandList = new Dictionary<int, Data.NARCTypes.CommandType>(baseCommands);
                                if (OverworldEditor.overlayZones.ContainsKey(ow))
                                {
                                    headers.Add("ScriptHeaders/CommandOverlay" + OverworldEditor.overlayZones[ow] + ".h");
                                    foreach (var cmd in CommandReference.bw2OverlayCommands[OverworldEditor.overlayZones[ow]])
                                        CommandReference.commandList.Add(cmd.Key, cmd.Value);
                                }

                                FileStream fs = File.OpenWrite(prompt.SelectedPath + "/" + fileTree.SelectedNode.Text + "/" + i + ".c");
                                fs.SetLength(0);
                                try
                                {
                                    sn.scriptFiles[i].Export(fs, headers.ToArray());
                                }
                                catch
                                {

                                }
                                fs.Close();
                            }

                            if (Directory.Exists(Directory.GetCurrentDirectory() + "\\ScriptHeaders"))
                            {
                                if (!Directory.Exists(prompt.SelectedPath + "/" + fileTree.SelectedNode.Text + "\\ScriptHeaders")) Directory.CreateDirectory(prompt.SelectedPath + "/" + fileTree.SelectedNode.Text + "\\ScriptHeaders");
                                foreach (string file in Directory.GetFiles(Directory.GetCurrentDirectory() + "\\ScriptHeaders"))
                                {
                                    File.Copy(file, prompt.SelectedPath + "/" + fileTree.SelectedNode.Text + "\\ScriptHeaders\\" + Path.GetFileName(file), true);
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < narc.numFileEntries; i++)
                            {
                                byte[] b = narc.GetFileEntry(i).ToArray();
                                File.WriteAllBytes(prompt.SelectedPath + "/" + fileTree.SelectedNode.Text + "/" + i + ".bin", b);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < narc.numFileEntries; i++)
                        {
                            byte[] b = narc.GetFileEntry(i).ToArray();
                            File.WriteAllBytes(prompt.SelectedPath + "/" + fileTree.SelectedNode.Text + "/" + i + ".bin", b);
                        }
                    }
                    statusText.Text = "Unpacked narc to " + prompt.SelectedPath + "\\" + fileTree.SelectedNode.Text + " - " + DateTime.Now.StatusText();
                    MessageBox.Show("Unpacked narc to" + prompt.SelectedPath + "\\" + fileTree.SelectedNode.Text);
                }
            }
        }

        private void packNarcButton_Click(object sender, EventArgs e)
        {
            if (fileTree.SelectedNode != null && fileTree.SelectedNode.Nodes.Count == 0 &&
                MainEditor.fileSystem.files[fnt.reverseFileNames[reverseNodes[fileTree.SelectedNode]] - fnt.firstFile] is NARC narc && narc.byteData[0] == (byte)'N')
            {
                FolderBrowserDialog prompt = new FolderBrowserDialog();
                if (prompt.ShowDialog() == DialogResult.OK)
                {
                    if (!Directory.Exists(prompt.SelectedPath))
                        Directory.CreateDirectory(prompt.SelectedPath);
                    List<string> fileNames = Directory.GetFiles(prompt.SelectedPath).ToList();
                    foreach (string s in fileNames)
                    {
                        if (!int.TryParse(s.Substring(s.LastIndexOf("\\") + 1, s.LastIndexOf(".") - s.LastIndexOf("\\") - 1), out int i1))
                        {
                            MessageBox.Show("Unable to process files in the provided folder.\nFiles must be named numerically.");
                            return;
                        }
                    }
                    fileNames.Sort((s1, s2) =>
                    {
                        if (int.TryParse(s1.Substring(s1.LastIndexOf("\\") + 1, s1.LastIndexOf(".") - s1.LastIndexOf("\\") - 1), out int i1) &&
                            int.TryParse(s2.Substring(s2.LastIndexOf("\\") + 1, s2.LastIndexOf(".") - s2.LastIndexOf("\\") - 1), out int i2))
                        {
                            return i1 - i2;
                        }
                        else
                        {
                            return 0;
                        }
                    });
                    for (int i = 0; i < fileNames.Count; i++)
                    {
                        narc.AddFileEntry(i, File.ReadAllBytes(fileNames[i]).ToList());
                    }
                    narc.ReadData();
                    statusText.Text = "Imported narc " + reverseNodes[fileTree.SelectedNode] + " - " + DateTime.Now.StatusText();
                    MessageBox.Show("Successfully packed Narc file");
                }
            }
        }

        string bulkImportLocation = "";
        private void bulkImportButton_Click(object sender, EventArgs e)
        {
            if (bulkImportLocation == "")
            {
                FolderBrowserDialog prompt = new FolderBrowserDialog();
                if (prompt.ShowDialog() == DialogResult.OK)
                {
                    bulkImportLocation = prompt.SelectedPath;
                }
                else return;
                if (!Directory.Exists(prompt.SelectedPath))
                    Directory.CreateDirectory(prompt.SelectedPath);
            }

            int importCount = 0;
            bool importedArm = false;
            for (int i = 0; i < MainEditor.fileSystem.overlays.Count; i++)
            {
                if (File.Exists(bulkImportLocation + "/ov_" + i.ToString("D3") + ".bin"))
                {
                    byte[] b = File.ReadAllBytes(bulkImportLocation + "/ov_" + i.ToString("D3") + ".bin");
                    if (b.Length == MainEditor.fileSystem.overlays[i].Count)
                    {
                        MainEditor.fileSystem.overlays[i] = new List<byte>(b);
                        importCount++;
                    }
                    else
                    {
                        MessageBox.Show("Failed to import overlay " + i + " due to file size mismatch");
                    }
                }
            }
            if (File.Exists(bulkImportLocation + "/arm9.bin"))
            {
                byte[] b = File.ReadAllBytes(bulkImportLocation + "/arm9.bin");
                if (b.Length == MainEditor.fileSystem.arm9.Count)
                {
                    MainEditor.fileSystem.arm9 = new List<byte>(b);
                    importedArm = true;
                }
                else
                {
                    MessageBox.Show("Failed to import arm9 due to file size mismatch");
                }
            }
            if (importCount == 0 && !importedArm)
                MessageBox.Show("No overlays found to import");
            else if (importCount > 0)
            {
                MessageBox.Show("Successfully imported " + importCount + " overlays" + (importedArm ? " and arm9" : ""));
                statusText.Text = "Imported " + importCount + " overlays" + (importedArm ? " and arm9" : "") + " - " + DateTime.Now.StatusText();
            }
            else
            {
                MessageBox.Show("Successfully imported arm9");
                statusText.Text = "Imported arm9" + " - " + DateTime.Now.StatusText();
            }
        }
    }
}
