using DarkModeForms;
using NewEditor.Data;
using NewEditor.Data.NARCTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms;

namespace NewEditor.Forms
{
    public partial class PaletteEditor : Form
    {
        PokemonSpriteEntry sprite;
        AnimationTimelineControl atc;
        AnimationTimelinePreview atp;
        public int RAMNStopSequence = 0;
        public RAMNSubsequence selectedRAMN = null;
        public RCMNCell selectedRCMN = null;
        public int selectedRCMNGroup = 0;
        public RNANSequence selectedRNANSeq = null;
        public RNANFrame selectedRNAN = null;

        PictureBox[] colorBoxes;
        NumericUpDown[,] colorValueNumberBoxes;
        int spriteID;

        int paletteArrayOffset = 0x8C54C;

        public PaletteEditor(int spriteID)
        {
            this.spriteID = spriteID;
            sprite = MainEditor.pokemonSpritesNarc.sprites[spriteID];
            InitializeComponent();

            if (MainEditor.fileSystem.romHeader[8] == 0x57)
            {
                paletteArrayOffset = 0x8C578;
            }

            List<byte> arm = MainEditor.fileSystem.arm9;

            bool valid = true;
            if (arm.Count < paletteArrayOffset)
            {
                valid = false;
            }
            else
            {
                for (int i = 0; i < 16; i++)
                {
                    if (arm[paletteArrayOffset + i] != 0 && arm[paletteArrayOffset + i] != 17 && arm[paletteArrayOffset + i] != 34)
                    {
                        valid = false;
                        break;
                    }
                }
            }

            if (!valid || spriteID >= 754) paletteArrayOffset = 0;
            else
            {
                int value = arm[paletteArrayOffset + spriteID];
                paletteIDNumberBox.Value = value & 0b_1111;
            }

            if (MainEditor.pokemonSpritesNarc.sprites[spriteID].FrontFemaleSpriteBytes.Length == 0) imageTypeDropdown.Items.AddRange(new string[]
            {
                "Front Sprite",
                "Front Rig",
                "Back Sprite",
                "Back Rig",
            });
            else imageTypeDropdown.Items.AddRange(new string[]
            {
                "Front Male Sprite",
                "Front Female Sprite",
                "Front Male Rig",
                "Front Female Rig",
                "Back Male Sprite",
                "Back Female Sprite",
                "Back Male Rig",
                "Back Female Rig",
            });
            imageTypeDropdown.SelectedIndex = 0;
            iconTypeDropdown.SelectedIndex = 0;
            fileIDDropdown.SelectedIndex = 0;

            animRCMNPanel.Location = new Point(20, 20);
            animRNANPanel.Location = new Point(20, 20);
            animRAMNPanel.Location = new Point(20, 20);

            sprite.RereadFiles();
            RenderRigCells();
            RenderAnimation();

            mainTabs.TabPages[3].Controls.Add(atp = new AnimationTimelinePreview(sprite));
            mainTabs.TabPages[3].Controls.Add(atc = new AnimationTimelineControl(sprite));
            atp.timeline = atc;
            atc.paletteEditor = this;
            ClickTimelineRAMN(sprite.frontRAMN.sequences[0].subSequences[0], 0);

            selectedCell = sprite.frontRigCells.cells[0];
            if (selectedCell.width == 0) selectedCell = sprite.frontRigCells.cells[1];
            LoadRigCell(selectedCell, "0");
            tlStopSequenceDropdown.SelectedIndex = 0;

            string text = "";
            rigFloatingCheckBox.Checked = sprite.frontRigCells.flags[1] != 0;
            useStopSequenceCheckBox.Checked = sprite.frontRigCells.flags[0] != 0xFF;
            if (sprite.frontRigCells.flags[0] != 0xFF)
            {
                for (int i = 0; i < Math.Min(sprite.frontRigCells.flags.Length - 2, sprite.frontRigCells.flags[0]); i++)
                {
                    text += sprite.frontRigCells.flags[i + 2] + " ";
                }
            }
            rigFlagsTextBox.Text = text.Substring(0, text.Length == 0 ? 0 : text.Length - 1);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            atp.timeline = null;
            mainTabs.TabPages[3].Controls.Remove(atc);
            mainTabs.TabPages[3].Controls.Remove(atp);
            atc.Dispose();
            atp.Dispose();
            base.OnFormClosing(e);
        }

        public void Setup(int spriteID)
        {
            sprite = MainEditor.pokemonSpritesNarc.sprites[spriteID];

            string type = (string)imageTypeDropdown.SelectedItem;
            if (type == "Front Sprite" || type == "Front Male Sprite") pokemonSpriteBox.Image = sprite.GetSprite(shinyCheckBox.Checked);
            else if (type == "Front Female Sprite") pokemonSpriteBox.Image = sprite.GetSprite(shinyCheckBox.Checked, false, true);
            else if (type == "Front Rig" || type == "Front Male Rig") pokemonSpriteBox.Image = sprite.GetRig(shinyCheckBox.Checked);
            else if (type == "Front Female Rig") pokemonSpriteBox.Image = sprite.GetRig(shinyCheckBox.Checked, false, true);
            else if (type == "Back Sprite" || type == "Back Male Sprite") pokemonSpriteBox.Image = sprite.GetSprite(shinyCheckBox.Checked, true);
            else if (type == "Back Female Sprite") pokemonSpriteBox.Image = sprite.GetSprite(shinyCheckBox.Checked, true, true);
            else if (type == "Back Rig" || type == "Back Male Rig") pokemonSpriteBox.Image = sprite.GetRig(shinyCheckBox.Checked, true);
            else if (type == "Back Female Rig") pokemonSpriteBox.Image = sprite.GetRig(shinyCheckBox.Checked, true, true);

            Color[] pal = shinyCheckBox.Checked ? sprite.shinyPalette : sprite.palette;

            if (colorBoxes == null)
            {
                colorBoxes = new PictureBox[16];
                colorValueNumberBoxes = new NumericUpDown[16, 3];
                for (int i = 0; i < 16; i++)
                {
                    int iPos = i == 0 ? 0 : i + 2;
                    colorBoxes[i] = new PictureBox()
                    {
                        Location = new Point(20 + 312 * (iPos % 3), 380 + 50 * (iPos / 3)),
                        Size = new Size(32, 32),
                        BackColor = pal[i]
                    };
                    mainTabs.TabPages[0].Controls.Add(colorBoxes[i]);

                    for (int j = 0; j < 3; j++)
                    {
                        int num = i;

                        colorValueNumberBoxes[i, j] = new NumericUpDown()
                        {
                            Location = new Point(68 + 312 * (iPos % 3) + 80 * j, 384 + 50 * (iPos / 3)),
                            Size = new Size(60, 24),
                            Maximum = 255,
                            Value = j == 0 ? pal[i].R : j == 1 ? pal[i].G : pal[i].B,
                        };
                        colorValueNumberBoxes[i, j].ValueChanged += (o, e) =>
                        {
                            colorBoxes[num].BackColor = Color.FromArgb((byte)colorValueNumberBoxes[num, 0].Value, (byte)colorValueNumberBoxes[num, 1].Value, (byte)colorValueNumberBoxes[num, 2].Value);
                        };
                        mainTabs.TabPages[0].Controls.Add(colorValueNumberBoxes[i, j]);
                    }
                }
            }
            else
            {
                for (int i = 0; i < 16; i++)
                {
                    colorBoxes[i].BackColor = pal[i];

                    for (int j = 0; j < 3; j++)
                    {
                        int num = i;

                        colorValueNumberBoxes[i, j].Value = j == 0 ? pal[i].R : j == 1 ? pal[i].G : pal[i].B;
                    }
                }
            }
        }

        private void applyPaletteButton_Click(object sender, EventArgs e)
        {
            Color[] pal = shinyCheckBox.Checked ? sprite.shinyPalette : sprite.palette;
            for (int i = 0; i < 16; i++)
            {
                pal[i] = colorBoxes[i].BackColor;
            }
            sprite.ApplyData();
            string type = (string)imageTypeDropdown.SelectedItem;
            if (type == "Front Sprite" || type == "Front Male Sprite") pokemonSpriteBox.Image = sprite.GetSprite(shinyCheckBox.Checked);
            else if (type == "Front Female Sprite") pokemonSpriteBox.Image = sprite.GetSprite(shinyCheckBox.Checked, false, true);
            else if (type == "Front Rig" || type == "Front Male Rig") pokemonSpriteBox.Image = sprite.GetRig(shinyCheckBox.Checked);
            else if (type == "Front Female Rig") pokemonSpriteBox.Image = sprite.GetRig(shinyCheckBox.Checked, false, true);
            else if (type == "Back Sprite" || type == "Back Male Sprite") pokemonSpriteBox.Image = sprite.GetSprite(shinyCheckBox.Checked, true);
            else if (type == "Back Female Sprite") pokemonSpriteBox.Image = sprite.GetSprite(shinyCheckBox.Checked, true, true);
            else if (type == "Back Rig" || type == "Back Male Rig") pokemonSpriteBox.Image = sprite.GetRig(shinyCheckBox.Checked, true);
            else if (type == "Back Female Rig") pokemonSpriteBox.Image = sprite.GetRig(shinyCheckBox.Checked, true, true);

            for (int i = 0; i < 4; i++)
            {
                rigImages[i] = (Bitmap)sprite.GetRig(false, i > 1, i % 2 == 1 && sprite.FrontFemaleSpriteBytes.Length > 0).Clone();
                for (int x = 0; x < 256; x++)
                {
                    for (int y = 0; y < 128; y++)
                    {
                        if (rigImages[i].GetPixel(x, y).Equals(sprite.palette[0])) rigImages[i].SetPixel(x, y, Color.Transparent);
                    }
                }
            }
        }

        private void shinyCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Setup(spriteID);
        }

        private void imageTypeDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            Setup(spriteID);
        }

        private void saveImageButton_Click(object sender, EventArgs e)
        {
            if (imageTypeDropdown.SelectedIndex == -1) return;
            SaveFileDialog save = new SaveFileDialog();
            save.FileName = imageTypeDropdown.Text.Replace(" ", "") + (shinyCheckBox.Checked ? "_Shiny" : "");
            save.Filter = "png file|*.png";
            if (save.ShowDialog() == DialogResult.OK)
            {
                pokemonSpriteBox.Image.Save(save.FileName);
            }
        }

        private void importImageButton_Click(object sender, EventArgs e)
        {
            if (imageTypeDropdown.SelectedIndex == -1) return;
            OpenFileDialog open = new OpenFileDialog();
            open.FileName = imageTypeDropdown.Text.Replace(" ", "") + (shinyCheckBox.Checked ? "_Shiny" : "");
            open.Filter = "png file|*.png";
            if (open.ShowDialog() == DialogResult.OK)
            {
                FileStream fs = File.OpenRead(open.FileName);
                Bitmap img = (Bitmap)Image.FromStream(fs);
                fs.Close();
                string type = (string)imageTypeDropdown.SelectedItem;
                if (type == "Front Sprite" || type == "Front Male Sprite") sprite.SetSprite(img, shinyCheckBox.Checked);
                else if (type == "Front Female Sprite") sprite.SetSprite(img, shinyCheckBox.Checked, false, true);
                else if (type == "Front Rig" || type == "Front Male Rig") sprite.SetRig(img, shinyCheckBox.Checked);
                else if (type == "Front Female Rig") sprite.SetRig(img, shinyCheckBox.Checked, false, true);
                else if (type == "Back Sprite" || type == "Back Male Sprite") sprite.SetSprite(img, shinyCheckBox.Checked, true);
                else if (type == "Back Female Sprite") sprite.SetSprite(img, shinyCheckBox.Checked, true, true);
                else if (type == "Back Rig" || type == "Back Male Rig") sprite.SetRig(img, shinyCheckBox.Checked, true);
                else if (type == "Back Female Rig") sprite.SetRig(img, shinyCheckBox.Checked, true, true);

                if (type == "Front Sprite" || type == "Front Male Sprite") pokemonSpriteBox.Image = sprite.GetSprite(shinyCheckBox.Checked);
                else if (type == "Front Female Sprite") pokemonSpriteBox.Image = sprite.GetSprite(shinyCheckBox.Checked, false, true);
                else if (type == "Front Rig" || type == "Front Male Rig") pokemonSpriteBox.Image = sprite.GetRig(shinyCheckBox.Checked);
                else if (type == "Front Female Rig") pokemonSpriteBox.Image = sprite.GetRig(shinyCheckBox.Checked, false, true);
                else if (type == "Back Sprite" || type == "Back Male Sprite") pokemonSpriteBox.Image = sprite.GetSprite(shinyCheckBox.Checked, true);
                else if (type == "Back Female Sprite") pokemonSpriteBox.Image = sprite.GetSprite(shinyCheckBox.Checked, true, true);
                else if (type == "Back Rig" || type == "Back Male Rig") pokemonSpriteBox.Image = sprite.GetRig(shinyCheckBox.Checked, true);
                else if (type == "Back Female Rig") pokemonSpriteBox.Image = sprite.GetRig(shinyCheckBox.Checked, true, true);

                for (int i = 0; i < 4; i++)
                {
                    rigImages[i] = (Bitmap)sprite.GetRig(false, i > 1, i % 2 == 1 && sprite.FrontFemaleSpriteBytes.Length > 0).Clone();
                    for (int x = 0; x < 256; x++)
                    {
                        for (int y = 0; y < 128; y++)
                        {
                            if (rigImages[i].GetPixel(x, y).Equals(sprite.palette[0])) rigImages[i].SetPixel(x, y, Color.Transparent);
                        }
                    }
                }
            }
            atp.SetRig(tlBackRigCheckbox.Checked, tlFemaleRigCheckbox.Checked);
        }

        private void extractFileButton_Click(object sender, EventArgs e)
        {
            if (fileIDDropdown.SelectedIndex == -1) return;
            SaveFileDialog save = new SaveFileDialog();
            save.FileName = fileIDDropdown.SelectedItem.ToString().Replace(" ", "");
            save.Filter = "bin file|*.bin";
            if (save.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllBytes(save.FileName, sprite.files[fileIDDropdown.SelectedIndex]);
            }
        }

        private void replaceFileButton_Click(object sender, EventArgs e)
        {
            if (fileIDDropdown.SelectedIndex == -1) return;
            OpenFileDialog open = new OpenFileDialog();
            open.FileName = fileIDDropdown.SelectedItem.ToString().Replace(" ", "");
            open.Filter = "bin file|*.bin";
            if (open.ShowDialog() == DialogResult.OK)
            {
                sprite.files[fileIDDropdown.SelectedIndex] = File.ReadAllBytes(open.FileName);
                sprite.ReadData();
                Setup(spriteID);
            }
        }

        private void savePaletteButton_Click(object sender, EventArgs e)
        {
            Color[] pal = shinyCheckBox.Checked ? sprite.shinyPalette : sprite.palette;
            SaveFileDialog save = new SaveFileDialog();
            save.FileName = shinyCheckBox.Checked ? "ShinyPalette" : "Palette";
            save.Filter = "png file|*.png";
            if (save.ShowDialog() == DialogResult.OK)
            {
                Bitmap bmp = new Bitmap(16, 1);
                for (int i = 0; i < 16; i++) bmp.SetPixel(i, 0, pal[i]);
                bmp.Save(save.FileName);
            }
        }

        private void importPaletteButton_Click(object sender, EventArgs e)
        {
            Color[] pal = shinyCheckBox.Checked ? sprite.shinyPalette : sprite.palette;
            OpenFileDialog open = new OpenFileDialog();
            open.FileName = shinyCheckBox.Checked ? "ShinyPalette" : "Palette";
            open.Filter = "png file|*.png";
            if (open.ShowDialog() == DialogResult.OK)
            {
                FileStream fs = File.OpenRead(open.FileName);
                Bitmap img = (Bitmap)Image.FromStream(fs);
                fs.Close();
                for (int i = 0; i < Math.Min(img.Width, 16); i++) pal[i] = img.GetPixel(i, 0);
                Setup(spriteID);
                applyPaletteButton_Click(sender, e);
            }
        }

        private void paletteIDNumberBox_ValueChanged(object sender, EventArgs e)
        {
            pokemonIconBox.Image = MainEditor.pokemonIconNarc.GetIcon(spriteID, iconTypeDropdown.SelectedIndex == 1, (int)paletteIDNumberBox.Value);
        }

        private void iconTypeDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            pokemonIconBox.Image = MainEditor.pokemonIconNarc.GetIcon(spriteID, iconTypeDropdown.SelectedIndex == 1, (int)paletteIDNumberBox.Value);
            if (spriteID >= 754) return;
            if (paletteArrayOffset != 0)
            {
                List<byte> arm = MainEditor.fileSystem.arm9;
                int value = arm[paletteArrayOffset + spriteID + (spriteID > 680 ? 2 : 0)];
                if (iconTypeDropdown.SelectedIndex == 1) paletteIDNumberBox.Value = (value & 0b_11110000) >> 4;
                else paletteIDNumberBox.Value = value & 0b_1111;
            }
        }

        private void setIconPaletteButton_Click(object sender, EventArgs e)
        {
            if (paletteArrayOffset == 0)
            {
                MessageBox.Show("Palette array not found");
                return;
            }
            List<byte> arm = MainEditor.fileSystem.arm9;
            int value = arm[paletteArrayOffset + spriteID + (spriteID > 680 ? 2 : 0)];
            if (iconTypeDropdown.SelectedIndex == 1) value = (value & 0b1111) | ((int)paletteIDNumberBox.Value << 4);
            else value = (value & 0b11110000) | (int)paletteIDNumberBox.Value;
            arm[paletteArrayOffset + spriteID + (spriteID > 680 ? 2 : 0)] = (byte)value;
        }

        private void saveIconButton_Click(object sender, EventArgs e)
        {
            if (iconTypeDropdown.SelectedIndex == -1) return;
            SaveFileDialog save = new SaveFileDialog();
            save.FileName = iconTypeDropdown.SelectedIndex == 1 ? "Icon_Female" : "Icon";
            save.Filter = "png file|*.png";
            if (save.ShowDialog() == DialogResult.OK)
            {
                pokemonIconBox.Image.Save(save.FileName);
            }
        }

        private void importIconButton_Click(object sender, EventArgs e)
        {
            if (iconTypeDropdown.SelectedIndex == -1) return;
            OpenFileDialog open = new OpenFileDialog();
            open.FileName = iconTypeDropdown.SelectedIndex == 1 ? "Icon_Female" : "Icon";
            open.Filter = "png file|*.png";
            if (open.ShowDialog() == DialogResult.OK)
            {
                FileStream fs = File.OpenRead(open.FileName);
                Bitmap img = (Bitmap)Image.FromStream(fs);
                fs.Close();
                MainEditor.pokemonIconNarc.SetIcon(spriteID, img, iconTypeDropdown.SelectedIndex == 1, (int)paletteIDNumberBox.Value);
            }
            pokemonIconBox.Image = MainEditor.pokemonIconNarc.GetIcon(spriteID, iconTypeDropdown.SelectedIndex == 1, (int)paletteIDNumberBox.Value);
        }

        private void saveIconPaletteButton_Click(object sender, EventArgs e)
        {
            Color[] pal = MainEditor.pokemonIconNarc.palettes[(int)paletteIDNumberBox.Value];
            SaveFileDialog save = new SaveFileDialog();
            save.FileName = "IconPalette" + (int)paletteIDNumberBox.Value;
            save.Filter = "png file|*.png";
            if (save.ShowDialog() == DialogResult.OK)
            {
                Bitmap bmp = new Bitmap(16, 1);
                for (int i = 0; i < 16; i++) bmp.SetPixel(i, 0, pal[i]);
                bmp.Save(save.FileName);
            }
        }

        private void dumpFilesButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog prompt = new FolderBrowserDialog();
            if (prompt.ShowDialog() == DialogResult.OK)
            {
                int start = (int)dumpFilesPIDNumberBox.Value * 20;
                for (int i = 0; i < 20; i++)
                {
                    string location = prompt.SelectedPath + "\\004_" + start.ToString("D8") + ".bin";
                    File.WriteAllBytes(location, sprite.files[i]);
                    start++;
                }
            }
        }

        private void RenderRigCells()
        {
            Bitmap image = new Bitmap(768, 384);
            Bitmap image2 = new Bitmap(512, 256);
            Graphics g = Graphics.FromImage(image);
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            Graphics g2 = Graphics.FromImage(image2);
            g2.InterpolationMode = InterpolationMode.NearestNeighbor;
            rigCellsRender.Image = image;
            cellPreview.Image = image2;

            for (int i = 0; i < 4; i++)
            {
                rigImages[i] = (Bitmap)sprite.GetRig(false, i > 1, i % 2 == 1 && sprite.FrontFemaleSpriteBytes.Length > 0).Clone();
                for (int x = 0; x < 256; x++)
                {
                    for (int y = 0; y < 128; y++)
                    {
                        if (rigImages[i].GetPixel(x, y).Equals(sprite.palette[0])) rigImages[i].SetPixel(x, y, Color.Transparent);
                    }
                }
            }

            cellRenderer = Task.Run(() =>
            {
                Thread.Sleep(200);
                while (!IsDisposed)
                {
                    if (tabToRender != 1)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    //Main
                    Pen pen = new Pen(Color.Black, 1);
                    g.Clear(Color.FromArgb(180, 200, 210));
                    Bitmap img = rigImages[(frontRigCheckBox.Checked ? 0 : 2) + (maleRigCheckBox.Checked ? 0 : 1)];
                    lock (img)
                    {
                        g.DrawImage(img, new Rectangle(0, 0, 768, 384));
                    }
                    for (int i = 0; i < 64; i++)
                    {
                        g.DrawLines(pen, new Point[]
                        {
                            new Point(i * 24, 0),
                            new Point(i * 24, 384),
                        });
                    }
                    for (int i = 0; i < 32; i++)
                    {
                        g.DrawLines(pen, new Point[]
                        {
                            new Point(0, i * 24),
                            new Point(768, i * 24),
                        });
                    }
                    pen = new Pen(Color.White, 2);
                    Pen pen2 = new Pen(Color.Red, 2);
                    List<RigCell> cells = new List<RigCell>(frontRigCheckBox.Checked ? sprite.frontRigCells.cells : sprite.backRigCells.cells);
                    foreach (RigCell cell in cells)
                    {
                        g.FillRectangle(new SolidBrush(Color.FromArgb(120, cell == selectedCell ? Color.White : Color.LawnGreen)), cell.cellX * 3, cell.cellY * 3, cell.width * 3, cell.height * 3);
                        g.DrawRectangle(pen, cell.cellX * 3, cell.cellY * 3, cell.width * 3, cell.height * 3);
                        g.DrawString(cells.IndexOf(cell).ToString(), new Font(Font.FontFamily, 16), new SolidBrush(Color.Black), cell.cellX * 3, cell.cellY * 3);
                        if (cell.subCell.width > 0)
                        {
                            g.FillRectangle(new SolidBrush(Color.FromArgb(120, cell.subCell == selectedCell ? Color.White : Color.Gold)), cell.subCell.cellX * 3, cell.subCell.cellY * 3, cell.subCell.width * 3, cell.subCell.height * 3);
                            g.DrawRectangle(pen, cell.subCell.cellX * 3, cell.subCell.cellY * 3, cell.subCell.width * 3, cell.subCell.height * 3);
                            g.DrawString(cells.IndexOf(cell).ToString(), new Font(Font.FontFamily, 16), new SolidBrush(Color.Black), cell.subCell.cellX * 3, cell.subCell.cellY * 3);
                            g.FillRectangle(new SolidBrush(Color.FromArgb(120, cell.subCell == selectedCell ? Color.White : Color.Gold)), cell.subCell.cellX * 3, cell.subCell.cellY * 3 + cell.subCell.height * 3, cell.subCell.width * 3, cell.subCell.height * 3);
                            g.DrawRectangle(pen, cell.subCell.cellX * 3, cell.subCell.cellY * 3 + cell.subCell.height * 3, cell.subCell.width * 3, cell.subCell.height * 3);
                        }
                    }

                    //Preview
                    pen = new Pen(Color.FromArgb(80, 80, 80), 1);
                    g2.Clear(Color.FromArgb(120, 160, 160));
                    RigCell parentCell = cells.FirstOrDefault(c => c.subCell == selectedCell);
                    if (parentCell != null)
                    {
                        g2.DrawImage(img,
                            new Rectangle(256 + parentCell.spriteX * 2, 128 - parentCell.spriteY * 2, parentCell.width * 2, parentCell.height * 2),
                            new Rectangle(parentCell.cellX, parentCell.cellY, parentCell.width, parentCell.height), GraphicsUnit.Pixel);
                    }
                    if (selectedCell != null)
                    {
                        int y = selectedCell.cellY;
                        if (parentCell != null && DateTime.Now.Second % 2 == 1) y += selectedCell.height;
                        g2.DrawImage(img,
                            new Rectangle(256 + selectedCell.spriteX * 2, 128 - selectedCell.spriteY * 2, selectedCell.width * 2, selectedCell.height * 2),
                            new Rectangle(selectedCell.cellX, y, selectedCell.width, selectedCell.height), GraphicsUnit.Pixel);
                    }
                    for (int i = 0; i < 64; i++)
                    {
                        g2.DrawLines(pen, new Point[]
                        {
                            new Point(i * 16, 0),
                            new Point(i * 16, 256),
                        });
                    }
                    for (int i = 0; i < 32; i++)
                    {
                        g2.DrawLines(pen, new Point[]
                        {
                            new Point(0, i * 16),
                            new Point(512, i * 16),
                        });
                    }
                    pen = new Pen(Color.LawnGreen, 2);
                    g2.DrawLine(pen, new Point(256, 0), new Point(256, 256));
                    pen = new Pen(Color.Red, 2);
                    g2.DrawLine(pen, new Point(0, 128), new Point(512, 128));

                    if (endRender) break;
                    try
                    {
                        rigCellsRender.Invoke((MethodInvoker)delegate { if (!endRender) rigCellsRender.Refresh(); });
                        cellPreview.Invoke((MethodInvoker)delegate { if (!endRender) cellPreview.Refresh(); });
                    }
                    catch { return; }
                    Thread.Sleep(5);
                }
            });
        }


        RigCell draggingCell = null;
        RigCell selectedCell = null;
        Bitmap[] rigImages = new Bitmap[4];
        int dragType;
        Point grabAnchor;
        Point mouse = new Point(0, 0);
        Task cellRenderer;
        bool endRender = false;
        int tabToRender = 0;

        private void rigCellsRender_MouseDown(object sender, MouseEventArgs e)
        {
            List<RigCell> cells = new List<RigCell>(frontRigCheckBox.Checked ? sprite.frontRigCells.cells : sprite.backRigCells.cells);
            grabAnchor = rigCellsRender.PointToClient(MousePosition);
            grabAnchor.X /= 3;
            grabAnchor.Y /= 3;
            dragType = 0;
            foreach (RigCell cell in cells)
            {
                if (new Rectangle(cell.cellX, cell.cellY, cell.width, cell.height).Contains(grabAnchor))
                {
                    if (cell != draggingCell) LoadRigCell(cell, cells.IndexOf(cell).ToString());
                    draggingCell = cell;
                    break;
                }
                if (new Rectangle(cell.subCell.cellX, cell.subCell.cellY, cell.subCell.width, cell.subCell.height * 2).Contains(grabAnchor))
                {
                    if (cell != draggingCell) LoadRigCell(cell.subCell, cells.IndexOf(cell).ToString() + "b");
                    draggingCell = cell.subCell;
                    break;
                }
            }
        }

        private void rigCellsRender_MouseMove(object sender, MouseEventArgs e)
        {
            if (draggingCell == null) return;
            mouse = rigCellsRender.PointToClient(MousePosition);
            mouse.X /= 3;
            mouse.Y /= 3;

            while (mouse.X > grabAnchor.X + 4 && draggingCell.cellX + draggingCell.width < 256)
            {
                grabAnchor.X += 8;
                draggingCell.cellX += 8;
                LoadRigCell(draggingCell);
            }
            while (mouse.X < grabAnchor.X - 4 && draggingCell.cellX > 0)
            {
                grabAnchor.X -= 8;
                draggingCell.cellX -= 8;
                LoadRigCell(draggingCell);
            }

            while (mouse.Y > grabAnchor.Y + 4 && draggingCell.cellY + draggingCell.height < 128)
            {
                grabAnchor.Y += 8;
                draggingCell.cellY += 8;
                LoadRigCell(draggingCell);
            }
            while (mouse.Y < grabAnchor.Y - 4 && draggingCell.cellY > 0)
            {
                grabAnchor.Y -= 8;
                draggingCell.cellY -= 8;
                LoadRigCell(draggingCell);
            }
        }

        private void rigCellsRender_MouseUp(object sender, MouseEventArgs e)
        {
            draggingCell = null;
        }

        private void PaletteEditor_FormClosing(object sender, FormClosedEventArgs e)
        {
            endRender = true;
        }

        private void LoadRigCell(RigCell cell, string cellID = "")
        {
            selectedCell = cell;
            cellXNumberBox.Value = cell.cellX;
            cellYNumberBox.Value = cell.cellY;
            cellWidthNumberBox.Value = cell.width;
            cellHeightNumberBox.Value = cell.height;
            cellSpriteXNumberBox.Value = cell.spriteX;
            cellSpriteYNumberBox.Value = cell.spriteY;

            if (cellID.Length > 0)
                selectedCellText.Text = "Cell " + cellID + ":";

            if (cell.subCell != null && cell.subCell.width == 0) addSubCellButton.Text = "Add Sub Cell";
            else addSubCellButton.Text = "Remove Sub Cell";
        }

        private void cellXNumberBox_ValueChanged(object sender, EventArgs e)
        {
            if (selectedCell == null) return;
            if (cellXNumberBox.Value % 8 != 0)
            {
                cellXNumberBox.Value -= cellXNumberBox.Value % 8;
            }
            else if (cellXNumberBox.Value + selectedCell.width > 256)
            {
                cellXNumberBox.Value = 256 - selectedCell.width;
            }
            selectedCell.cellX = (int)cellXNumberBox.Value;
        }

        private void cellYNumberBox_ValueChanged(object sender, EventArgs e)
        {
            if (selectedCell == null) return;
            if (cellYNumberBox.Value % 8 != 0)
            {
                cellYNumberBox.Value -= cellYNumberBox.Value % 8;
            }
            else if (cellYNumberBox.Value + selectedCell.height > 128)
            {
                cellYNumberBox.Value = 128 - selectedCell.height;
            }
            selectedCell.cellY = (int)cellYNumberBox.Value;
        }

        private void cellWidthNumberBox_ValueChanged(object sender, EventArgs e)
        {
            if (selectedCell == null) return;
            if (cellWidthNumberBox.Value % 8 != 0)
            {
                cellWidthNumberBox.Value -= cellWidthNumberBox.Value % 8;
            }
            else if (cellWidthNumberBox.Value + selectedCell.cellX > 256)
            {
                cellWidthNumberBox.Value = 256 - selectedCell.cellX;
            }
            selectedCell.width = (int)cellWidthNumberBox.Value;
        }

        private void cellHeightNumberBox_ValueChanged(object sender, EventArgs e)
        {
            if (selectedCell == null) return;
            if (cellHeightNumberBox.Value % 8 != 0)
            {
                cellHeightNumberBox.Value -= cellHeightNumberBox.Value % 8;
            }
            else if (cellHeightNumberBox.Value + selectedCell.cellY > 128)
            {
                cellHeightNumberBox.Value = 128 - selectedCell.cellY;
            }
            selectedCell.height = (int)cellHeightNumberBox.Value;
        }

        private void cellSpriteXNumberBox_ValueChanged(object sender, EventArgs e)
        {
            if (selectedCell == null) return;
            selectedCell.spriteX = (int)cellSpriteXNumberBox.Value;
        }

        private void cellSpriteYNumberBox_ValueChanged(object sender, EventArgs e)
        {
            if (selectedCell == null) return;
            selectedCell.spriteY = (int)cellSpriteYNumberBox.Value;
        }

        private void frontrigCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            LoadRigCell(frontRigCheckBox.Checked ? sprite.frontRigCells.cells[0] : sprite.backRigCells.cells[0], "0");

            string text = "";
            byte[] flags = backRigCheckbox.Checked ? sprite.backRigCells.flags : sprite.frontRigCells.flags;
            rigFloatingCheckBox.Checked = flags[1] != 0;
            useStopSequenceCheckBox.Checked = flags[0] != 0xFF;
            if (sprite.frontRigCells.flags[0] != 0xFF)
            {
                for (int i = 0; i < Math.Min(flags.Length - 2, flags[0]); i++)
                {
                    text += flags[i + 2] + " ";
                }
            }
            rigFlagsTextBox.Text = text.Substring(0, text.Length == 0 ? 0 : text.Length - 1);
        }

        private void addRigCellButton_Click(object sender, EventArgs e)
        {
            RigCell cell = new RigCell()
            {
                width = 16,
                height = 16,
                cellX = 0,
                cellY = 112,
                subCell = new RigCell()
            };
            if (frontRigCheckBox.Checked) sprite.frontRigCells.cells.Add(cell);
            else sprite.backRigCells.cells.Add(cell);
            LoadRigCell(cell);
        }

        private void removeRigCellButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Removing a cell will shift the IDs of all cells after it, requiring you update the animation data accordingly.\nAre you sure you want to do this?", "Remove Rig Cell", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

            if (frontRigCheckBox.Checked) sprite.frontRigCells.cells.Remove(selectedCell);
            else sprite.backRigCells.cells.Remove(selectedCell);
            LoadRigCell(frontRigCheckBox.Checked ? sprite.frontRigCells.cells[0] : sprite.backRigCells.cells[0], "0");
        }

        private void applyRigCellButton_Click(object sender, EventArgs e)
        {
            RigCellsFile rc = frontRigCheckBox.Checked ? sprite.frontRigCells : sprite.backRigCells;
            List<byte> b = new List<byte>();
            string[] ints = rigFlagsTextBox.Text.Split(new char[] {' ', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (string i in ints)
            {
                if (byte.TryParse(i, out byte num)) b.Add(num);
                else
                {
                    MessageBox.Show("Failed to parse numbers for stop sequence cells.");
                    return;
                }
            }
            b.Insert(0, (byte)b.Count);
            b.Insert(1, (byte)(rigFloatingCheckBox.Checked ? 1 : 0));
            b.Add(0);
            while (b.Count % 4 != 0) b.Add(0);
            rc.flags = b.ToArray();

            sprite.WriteRigCellsFile();
        }

        private void subCellHelpButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Sub cells are animated cells that follow the motion of their parent cell. They are primarily used to make eyes open and close when the pokemon blinks or is asleep. You can only have one sub cell per cell.");
        }

        private void addSubCellButton_Click(object sender, EventArgs e)
        {
            if (selectedCell.subCell != null && selectedCell.subCell.width == 0)
            {
                //Add
                selectedCell.subCell.width = 8;
                selectedCell.subCell.height = 8;
                selectedCell.subCell.cellX = 0;
                selectedCell.subCell.cellY = 0;
                selectedCell.subCell.spriteX = 0;
                selectedCell.subCell.spriteY = 0;
                List<RigCell> cells = new List<RigCell>(frontRigCheckBox.Checked ? sprite.frontRigCells.cells : sprite.backRigCells.cells);
                RigCell cell = selectedCell.subCell == null ? cells.FirstOrDefault(cl => cl.subCell == selectedCell) : selectedCell;
                LoadRigCell(selectedCell.subCell, cells.IndexOf(cell).ToString() + "b");
            }
            else
            {
                //Remove
                RigCell c = selectedCell.subCell == null ? selectedCell : selectedCell.subCell;
                c.width = 0;
                c.height = 0;
                c.cellX = 0;
                c.cellY = 0;
                c.spriteX = 0;
                c.spriteY = 0;
                if (selectedCell.subCell == null)
                {
                    List<RigCell> cells = new List<RigCell>(frontRigCheckBox.Checked ? sprite.frontRigCells.cells : sprite.backRigCells.cells);
                    RigCell cell = cells.FirstOrDefault(cl => cl.subCell == selectedCell);
                    LoadRigCell(cell, cells.IndexOf(cell).ToString());
                }
            }
        }

        int animSequence;
        int animFrame;
        float animTimer = 0;
        DateTime animTime;
        bool playAnim = false;
        Task animRenderer;
        int selectedAnimCell = 0;

        private void RenderAnimation()
        {
            Bitmap image = new Bitmap(320, 320);
            Graphics g = Graphics.FromImage(image);
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            animationRender.Image = image;

            animSequenceNumberBox.Maximum = sprite.frontRAMN.sequences.Count - 1;
            UpdateRCMNList();
            if (frontAnimationCheckBox.Checked)
            {
                for (int i = 0; i < sprite.frontRAMN.sequences[animSequence].subSequences.Count; i++)
                {
                    ramnSeqListBox.Items.Add("Sequence " + sprite.frontRAMN.sequences[animSequence].subSequences[i].rcmnGroupID);
                }
            }
            else
            {
                for (int i = 0; i < sprite.backRAMN.sequences[animSequence].subSequences.Count; i++)
                {
                    ramnSeqListBox.Items.Add("Sequence " + sprite.backRAMN.sequences[animSequence].subSequences[i].rcmnGroupID);
                }
            }

            animRenderer = Task.Run(() =>
            {
                Thread.Sleep(200);
                UpdateRNANData();
                while (!IsDisposed)
                {
                    if (tabToRender != 2)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    //Main
                    Pen pen = new Pen(Color.SlateGray, 1);
                    g.Clear(Color.FromArgb(180, 200, 210));
                    for (int i = 0; i < 24; i++)
                    {
                        g.DrawLines(pen, new Point[]
                        {
                            new Point(i * 16, 0),
                            new Point(i * 16, 320),
                        });
                    }
                    for (int i = 0; i < 24; i++)
                    {
                        g.DrawLines(pen, new Point[]
                        {
                            new Point(0, i * 16),
                            new Point(320, i * 16),
                        });
                    }
                    int imgid = frontAnimationCheckBox.Checked ? (animMaleCheckBox.Checked ? 0 : 1) : (animMaleCheckBox.Checked ? 2 : 3);
                    Bitmap img = rigImages[imgid];
                    lock (img)
                    {
                        List<RCMNCell> rcmnCells = GetRCMNGroupAtTime(frontAnimationCheckBox.Checked ? sprite.frontRAMN : sprite.backRAMN, frontAnimationCheckBox.Checked ? sprite.frontRCMN : sprite.backRCMN, animFrame, animSequence, out animFrame, out int seqnenceFrame, out RAMNSubsequence ramnSeq).cells.ToList();
                        rcmnCells.Reverse();
                        foreach (RCMNCell rcmn in rcmnCells)
                        {
                            RNANFrame frame = frontAnimationCheckBox.Checked ? sprite.frontRNAN.sequences[rcmn.sequenceID].GetFrameAtTime(seqnenceFrame) :
                                sprite.backRNAN.sequences[rcmn.sequenceID].GetFrameAtTime(seqnenceFrame);
                            if (frame.xScale == 0 || frame.yScale == 0) continue;
                            int cell = frame.cellID;
                            if (cell >= (frontAnimationCheckBox.Checked ? sprite.frontRigCells.cells.Count : sprite.backRigCells.cells.Count)) continue;
                            RigCell rc = frontAnimationCheckBox.Checked ? sprite.frontRigCells.cells[cell] : sprite.backRigCells.cells[cell];
                            g.TranslateTransform(rc.spriteX, -rc.spriteY, MatrixOrder.Append);
                            g.ScaleTransform(frame.xScale, frame.yScale, MatrixOrder.Append);
                            g.RotateTransform(frame.rot, MatrixOrder.Append);
                            g.TranslateTransform(rcmn.xOffset, rcmn.yOffset, MatrixOrder.Append);
                            g.TranslateTransform((short)frame.xPos, (short)frame.yPos, MatrixOrder.Append);
                            g.ScaleTransform(ramnSeq.xScale, ramnSeq.yScale, MatrixOrder.Append);
                            g.RotateTransform(ramnSeq.rot, MatrixOrder.Append);
                            g.TranslateTransform((short)ramnSeq.xPos, (short)ramnSeq.yPos, MatrixOrder.Append);
                            g.TranslateTransform(64, 112, MatrixOrder.Append);
                            g.ScaleTransform(2, 2, MatrixOrder.Append);
                            g.DrawImage(img, new Rectangle(0, 0, rc.width, rc.height),
                                    new Rectangle(rc.cellX, rc.cellY, rc.width, rc.height), GraphicsUnit.Pixel);

                            if (rcmnCells.IndexOf(rcmn) == selectedAnimCell)
                            {
                                pen = new Pen(Color.Yellow, 1);
                                g.DrawLines(pen, new Point[]
                                {
                                    new Point(0, 0),
                                    new Point(rc.width, 0),
                                    new Point(rc.width, rc.height),
                                    new Point(0, rc.height),
                                    new Point(0, 0),
                                });
                            }
                            g.ResetTransform();

                            rc = rc.subCell;
                            if (rc.width > 0)
                            {
                                g.TranslateTransform(rc.spriteX, -rc.spriteY, MatrixOrder.Append);
                                g.ScaleTransform(frame.xScale, frame.yScale, MatrixOrder.Append);
                                g.RotateTransform(frame.rot, MatrixOrder.Append);
                                g.TranslateTransform(rcmn.xOffset, rcmn.yOffset, MatrixOrder.Append);
                                g.TranslateTransform((short)frame.xPos, (short)frame.yPos, MatrixOrder.Append);
                                g.ScaleTransform(ramnSeq.xScale, ramnSeq.yScale, MatrixOrder.Append);
                                g.RotateTransform(ramnSeq.rot, MatrixOrder.Append);
                                g.TranslateTransform((short)ramnSeq.xPos, (short)ramnSeq.yPos, MatrixOrder.Append);
                                g.TranslateTransform(64, 112, MatrixOrder.Append);
                                g.ScaleTransform(2, 2, MatrixOrder.Append);
                                g.DrawImage(img, new Rectangle(0, 0, rc.width, rc.height),
                                        new Rectangle(rc.cellX, rc.cellY, rc.width, rc.height), GraphicsUnit.Pixel);

                                pen = new Pen(Color.Yellow, 1);
                                if (rcmnCells.IndexOf(rcmn) == selectedAnimCell)
                                {
                                    pen = new Pen(Color.Yellow, 1);
                                    g.DrawLines(pen, new Point[]
                                    {
                                        new Point(0, 0),
                                        new Point(rc.width, 0),
                                        new Point(rc.width, rc.height),
                                        new Point(0, rc.height),
                                        new Point(0, 0),
                                    });
                                }
                            }
                            g.ResetTransform();
                        }
                    }
                    pen.Dispose();

                    if (playAnim)
                    {
                        UpdateRNANData();
                        animTimer += (float)((DateTime.Now - animTime).TotalSeconds * 60);
                        animTime = DateTime.Now;
                        if (animTimer > 1)
                        {
                            animFrame++;
                            animTimer = animTimer % 1;
                        }
                    }
                    try
                    {
                        animationFrameNumberBox.Invoke((MethodInvoker)delegate { animationFrameNumberBox.Value = animFrame; });
                    }
                    catch { }

                    if (endRender) break;
                    try
                    {
                        animationRender.Invoke((MethodInvoker)delegate { if (!endRender) animationRender.Refresh(); });
                    }
                    catch { return; }
                    Thread.Sleep(5);
                }
            });
        }

        private void mainTabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            tabToRender = mainTabs.SelectedIndex;
        }

        private void animationFrameNumberBox_ValueChanged(object sender, EventArgs e)
        {
            animFrame = (int)animationFrameNumberBox.Value;
        }

        private void playAnimationButton_Click(object sender, EventArgs e)
        {
            playAnimationButton.ImageIndex = (playAnimationButton.ImageIndex + 1) % 2;
            playAnim = !playAnim;
            ramnSeqListBox.Enabled = !playAnim;

            if (!playAnim) UpdateRNANData();
        }

        private void prevAnimFrameCutton_Click(object sender, EventArgs e)
        {
            int cell = animCellsListBox.SelectedIndex;
            if (cell < 0) cell = 0;
            List<RCMNCell> rcmnCells = GetRCMNGroupAtTime(frontAnimationCheckBox.Checked ? sprite.frontRAMN : sprite.backRAMN, frontAnimationCheckBox.Checked ? sprite.frontRCMN : sprite.backRCMN, animFrame, animSequence, out int animFrame2, out int seqnenceFrame, out RAMNSubsequence ramnSeq).cells.ToList();
            rcmnCells.Reverse();
            RNANFrame frame = frontAnimationCheckBox.Checked ? sprite.frontRNAN.sequences[rcmnCells[Math.Min(cell, rcmnCells.Count - 1)].sequenceID].GetFrameAtTime(seqnenceFrame) :
                sprite.backRNAN.sequences[rcmnCells[Math.Min(cell, rcmnCells.Count - 1)].sequenceID].GetFrameAtTime(seqnenceFrame);

            playAnimationButton.ImageIndex = 0;
            playAnim = false;
            animationFrameNumberBox.Value = Math.Max(0, animationFrameNumberBox.Value - frame.duration);
            ramnSeqListBox.Enabled = !playAnim;

            UpdateRNANData();
        }

        private void nextAnimFrameButton_Click(object sender, EventArgs e)
        {
            int cell = animCellsListBox.SelectedIndex;
            if (cell < 0) cell = 0;
            List<RCMNCell> rcmnCells = GetRCMNGroupAtTime(frontAnimationCheckBox.Checked ? sprite.frontRAMN : sprite.backRAMN, frontAnimationCheckBox.Checked ? sprite.frontRCMN : sprite.backRCMN, animFrame, animSequence, out int animFrame2, out int seqnenceFrame, out RAMNSubsequence ramnSeq).cells.ToList();
            rcmnCells.Reverse();
            RNANFrame frame = frontAnimationCheckBox.Checked ? sprite.frontRNAN.sequences[rcmnCells[Math.Min(cell, rcmnCells.Count - 1)].sequenceID].GetFrameAtTime(seqnenceFrame) :
                sprite.backRNAN.sequences[rcmnCells[Math.Min(cell, rcmnCells.Count - 1)].sequenceID].GetFrameAtTime(seqnenceFrame);

            playAnimationButton.ImageIndex = 0;
            playAnim = false;
            animationFrameNumberBox.Value = Math.Min(65535, animationFrameNumberBox.Value + frame.duration);
            ramnSeqListBox.Enabled = !playAnim;

            UpdateRNANData();
        }

        private void animCellsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedAnimCell = animCellsListBox.SelectedIndex;

            if (!playAnim) UpdateRNANData();
        }

        private void animSequenceNumberBox_ValueChanged(object sender, EventArgs e)
        {
            animSequence = (int)animSequenceNumberBox.Value;
            animFrame = 0;

            UpdateRCMNList();
            ramnSeqListBox.Items.Clear();
            if (frontAnimationCheckBox.Checked)
            {
                for (int i = 0; i < sprite.frontRAMN.sequences[animSequence].subSequences.Count; i++)
                {
                    ramnSeqListBox.Items.Add("Sequence " + sprite.frontRAMN.sequences[animSequence].subSequences[i].rcmnGroupID);
                }
            }
            else
            {
                for (int i = 0; i < sprite.backRAMN.sequences[animSequence].subSequences.Count; i++)
                {
                    ramnSeqListBox.Items.Add("Sequence " + sprite.backRAMN.sequences[animSequence].subSequences[i].rcmnGroupID);
                }
            }
            UpdateRNANData();
        }

        private RCMNGroup GetRCMNGroupAtTime(RAMNFile ramn, RCMNFile rcmn, int globalFrame, int ramnSeq, out int correctedFrame, out int sequenceFrame, out RAMNSubsequence subSeq)
        {
            int seq = 0;
            correctedFrame = globalFrame;
            sequenceFrame = globalFrame;
            while (globalFrame >= ramn.sequences[ramnSeq].subSequences[seq].duration)
            {
                globalFrame -= ramn.sequences[ramnSeq].subSequences[seq].duration;
                int old = ramn.sequences[ramnSeq].subSequences[seq].rcmnGroupID;
                seq++;
                if (seq >= ramn.sequences[ramnSeq].subSequences.Count)
                {
                    seq -= ramn.sequences[ramnSeq].subSequences.Count;
                    correctedFrame = globalFrame;
                }
                if (old != ramn.sequences[ramnSeq].subSequences[seq].rcmnGroupID) sequenceFrame = globalFrame;
            }
            if (playAnim)
            {
                try
                {
                    ramnSeqListBox.Invoke((MethodInvoker)delegate { ramnSeqListBox.SelectedItems.Clear(); ramnSeqListBox.SelectedIndex = seq; });
                }
                catch { }
            }
            subSeq = ramn.sequences[ramnSeq].subSequences[seq];
            return rcmn.groups[subSeq.rcmnGroupID];
        }

        private void UpdateRNANData()
        {
            try
            {
                Invoke((MethodInvoker)delegate
                {
                    RNANFile rnan = frontAnimationCheckBox.Checked ? sprite.frontRNAN : sprite.backRNAN;
                    RCMNFile rcmnFile = frontAnimationCheckBox.Checked ? sprite.frontRCMN : sprite.backRCMN;
                    RAMNFile ramnFile = frontAnimationCheckBox.Checked ? sprite.frontRAMN : sprite.backRAMN;
                    manualRNANChange = false;
                    int cell = animCellsListBox.SelectedIndex;
                    List<RCMNCell> rcmnCells = GetRCMNGroupAtTime(frontAnimationCheckBox.Checked ? sprite.frontRAMN : sprite.backRAMN, frontAnimationCheckBox.Checked ? sprite.frontRCMN : sprite.backRCMN, animFrame, animSequence, out int animFrame2, out int seqnenceFrame, out RAMNSubsequence ramnSeq).cells.ToList();
                    if (cell < 0) cell = 0;
                    if (cell >= rcmnCells.Count) cell = rcmnCells.Count - 1;
                    rcmnCells.Reverse();
                    RNANFrame frame = rnan.sequences[rcmnCells[cell].sequenceID].GetFrameAtTime(seqnenceFrame);

                    rnanSequencesListBox.SelectedIndex = rcmnCells[cell].sequenceID;

                    ramnModeDropdown.SelectedIndex = ramnFile.sequences[(int)animSequenceNumberBox.Value].loopMode;

                    ramnRCMNGroupNumberBox.Maximum = rcmnFile.groups.Count - 1;
                    rcmnGroupNumberBox.Maximum = rcmnFile.groups.Count - 1;
                    rcmnGroupCountLabel.Text = "/ " + (rcmnFile.groups.Count - 1);
                    rcmnGroupCountLabel2.Text = "/ " + (rcmnFile.groups.Count - 1);
                    ramnRCMNGroupNumberBox.Value = ramnSeq.rcmnGroupID;
                    ramnDurationNumberBox.Value = ramnSeq.duration;
                    ramnXPosNumberBox.Value = (short)ramnSeq.xPos;
                    ramnYPosNumberBox.Value = (short)ramnSeq.yPos;
                    ramnXScaleNumberBox.Value = (decimal)ramnSeq.xScale;
                    ramnYScaleNumberBox.Value = (decimal)ramnSeq.yScale;
                    ramnRotationNumberBox.Value = (decimal)ramnSeq.rot;

                    rcmnSequenceNumberBox.Value = rcmnCells[cell].sequenceID;
                    rcmnSequenceNumberBox.Maximum = frontAnimationCheckBox.Checked ? (sprite.frontRNAN.sequences.Count - 1) : (sprite.backRNAN.sequences.Count - 1);
                    rcmnSequenceLabel.Text = "/ " + rcmnSequenceNumberBox.Maximum.ToString();
                    rcmnXPosNumberBox.Value = rcmnCells[cell].xOffset;
                    rcmnYPosNumberBox.Value = rcmnCells[cell].yOffset;
                    
                    rnanLoopModeDropdown.SelectedIndex = rnan.sequences[rcmnCells[cell].sequenceID].loopType;
                    rnanRigCellIDNumberBox.Value = frame.cellID;
                    rnanDurationNumberBox.Value = frame.duration;
                    rnanXPosNumberBox.Value = (short)frame.xPos;
                    rnanYPosNumberBox.Value = (short)frame.yPos;
                    rnanRotationNumberBox.Value = (decimal)frame.rot;
                    rnanXScaleNumberBox.Value = (decimal)frame.xScale;
                    rnanYScaleNumberBox.Value = (decimal)frame.yScale;

                    rnanKeyframeNumberBox.Maximum = rnan.sequences[rcmnCells[cell].sequenceID].frames.Count - 1;
                    rnanKeyframeNumberBox.Value = rnan.sequences[rcmnCells[cell].sequenceID].frames.IndexOf(frame);
                    rnanKeyframesLabel.Text = "/ " + (rnan.sequences[rcmnCells[cell].sequenceID].frames.Count - 1);
                    manualRNANChange = true;

                    List<int> usedCells = new List<int>();
                    if (frontAnimationCheckBox.Checked)
                    {
                        foreach (var v in sprite.frontRNAN.sequences[rcmnCells[cell].sequenceID].frames) if (!usedCells.Contains(v.cellID)) usedCells.Add(v.cellID);
                    }
                    else
                    {
                        foreach (var v in sprite.backRNAN.sequences[rcmnCells[cell].sequenceID].frames) if (!usedCells.Contains(v.cellID)) usedCells.Add(v.cellID);
                    }
                    string text = "Sequence: " + rcmnCells[cell].sequenceID + "\nRig Cells: " + usedCells[0];
                    for (int i = 1; i < usedCells.Count; i++) text += ", " + usedCells[i];
                    text += "\nKeyframe: " + (frontAnimationCheckBox.Checked ?
                        sprite.frontRNAN.sequences[rcmnCells[cell].sequenceID].frames.IndexOf(frame) :
                        sprite.backRNAN.sequences[rcmnCells[cell].sequenceID].frames.IndexOf(frame)) + " / " +
                        (frontAnimationCheckBox.Checked ?
                        sprite.frontRNAN.sequences[rcmnCells[cell].sequenceID].frames.Count - 1 :
                        sprite.backRNAN.sequences[rcmnCells[cell].sequenceID].frames.Count - 1);
                    rnanDescriptionText.Text = text;
                });
            }
            catch { };
        }

        private void rnanSequencesListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!manualRNANChange) return;

            manualRNANChange = false;
            RNANFile rnan = frontAnimationCheckBox.Checked ? sprite.frontRNAN : sprite.backRNAN;

            GetRCMNGroupAtTime(frontAnimationCheckBox.Checked ? sprite.frontRAMN : sprite.backRAMN, frontAnimationCheckBox.Checked ? sprite.frontRCMN : sprite.backRCMN, animFrame, animSequence, out int animFrame2, out int seqnenceFrame, out RAMNSubsequence ramnSeq).cells.ToList();
            if (rnanSequencesListBox.SelectedIndex < 0) return;
            RNANFrame frame = rnan.sequences[rnanSequencesListBox.SelectedIndex].GetFrameAtTime(seqnenceFrame);

            rnanLoopModeDropdown.SelectedIndex = rnan.sequences[rnanSequencesListBox.SelectedIndex].loopType;
            rnanRigCellIDNumberBox.Value = frame.cellID;
            rnanDurationNumberBox.Value = frame.duration;
            rnanXPosNumberBox.Value = (short)frame.xPos;
            rnanYPosNumberBox.Value = (short)frame.yPos;
            rnanRotationNumberBox.Value = (decimal)frame.rot;
            rnanXScaleNumberBox.Value = (decimal)frame.xScale;
            rnanYScaleNumberBox.Value = (decimal)frame.yScale;

            rnanKeyframeNumberBox.Maximum = rnan.sequences[rnanSequencesListBox.SelectedIndex].frames.Count - 1;
            rnanKeyframeNumberBox.Value = rnan.sequences[rnanSequencesListBox.SelectedIndex].frames.IndexOf(frame);
            rnanKeyframesLabel.Text = "/ " + (rnan.sequences[rnanSequencesListBox.SelectedIndex].frames.Count - 1);
            manualRNANChange = true;
        }

        private void rnanKeyframeNumberBox_ValueChanged(object sender, EventArgs e)
        {
            if (!manualRNANChange) return;
            manualRNANChange = false;
            RNANFile rnan = frontAnimationCheckBox.Checked ? sprite.frontRNAN : sprite.backRNAN;
            RNANFrame frame = rnan.sequences[rnanSequencesListBox.SelectedIndex].frames[(int)rnanKeyframeNumberBox.Value];

            rnanLoopModeDropdown.SelectedIndex = rnan.sequences[rnanSequencesListBox.SelectedIndex].loopType;
            rnanRigCellIDNumberBox.Value = frame.cellID;
            rnanDurationNumberBox.Value = frame.duration;
            rnanXPosNumberBox.Value = (short)frame.xPos;
            rnanYPosNumberBox.Value = (short)frame.yPos;
            rnanRotationNumberBox.Value = (decimal)frame.rot;
            rnanXScaleNumberBox.Value = (decimal)frame.xScale;
            rnanYScaleNumberBox.Value = (decimal)frame.yScale;
            manualRNANChange = true;
        }

        private void EditRCMNData(object sender, EventArgs e)
        {
            if (manualRNANChange)
            {
                int cell = animCellsListBox.SelectedIndex;
                if (cell < 0) cell = 0;
                List<RCMNCell> rcmnCells = (frontAnimationCheckBox.Checked ? sprite.frontRCMN : sprite.backRCMN).groups[(int)rcmnGroupNumberBox.Value].cells.ToList();
                rcmnCells.Reverse();

                rcmnCells[cell].sequenceID = (int)rcmnSequenceNumberBox.Value;
                rcmnCells[cell].xOffset = (short)rcmnXPosNumberBox.Value;
                rcmnCells[cell].yOffset = (short)rcmnYPosNumberBox.Value;
            }
        }

        private void EditRAMNData(object sender, EventArgs e)
        {
            if (manualRNANChange)
            {
                RAMNFile ramnFile = frontAnimationCheckBox.Checked ? sprite.frontRAMN : sprite.backRAMN;

                foreach (int i in ramnSeqListBox.SelectedIndices)
                {
                    RAMNSubsequence ramnSeq = ramnFile.sequences[(int)animSequenceNumberBox.Value].subSequences[i];
                    if (sender == ramnModeDropdown) ramnFile.sequences[(int)animSequenceNumberBox.Value].loopMode = ramnModeDropdown.SelectedIndex;
                    if (sender == ramnRCMNGroupNumberBox) ramnSeq.rcmnGroupID = (int)ramnRCMNGroupNumberBox.Value;
                    if (sender == ramnDurationNumberBox) ramnSeq.duration = (int)ramnDurationNumberBox.Value;
                    if (sender == ramnXPosNumberBox) ramnSeq.xPos = (short)ramnXPosNumberBox.Value;
                    if (sender == ramnYPosNumberBox) ramnSeq.yPos = (short)ramnYPosNumberBox.Value;
                    if (sender == ramnXScaleNumberBox) ramnSeq.xScale = (float)ramnXScaleNumberBox.Value;
                    if (sender == ramnYScaleNumberBox) ramnSeq.yScale = (float)ramnYScaleNumberBox.Value;
                    if (sender == ramnRotationNumberBox) ramnSeq.rot = (float)ramnRotationNumberBox.Value;
                }
                foreach (var seq in ramnFile.sequences[(int)animSequenceNumberBox.Value].subSequences)
                {
                    ramnSeqListBox.Items[ramnFile.sequences[(int)animSequenceNumberBox.Value].subSequences.IndexOf(seq)] = "Sequence " + seq.rcmnGroupID;
                }
            }
        }

        bool manualRNANChange = true;
        private void EditRNANData(object sender, EventArgs e)
        {
            if (manualRNANChange)
            {
                int cell = animCellsListBox.SelectedIndex;
                if (cell < 0) cell = 0;
                List<RCMNCell> rcmnCells = GetRCMNGroupAtTime(frontAnimationCheckBox.Checked ? sprite.frontRAMN : sprite.backRAMN, frontAnimationCheckBox.Checked ? sprite.frontRCMN : sprite.backRCMN, animFrame, animSequence, out int animFrame2, out int seqnenceFrame, out RAMNSubsequence ramnSeq).cells.ToList();
                rcmnCells.Reverse();
                RNANFile rnan = frontAnimationCheckBox.Checked ? sprite.frontRNAN : sprite.backRNAN;
                RNANFrame frame = rnan.sequences[rnanSequencesListBox.SelectedIndex].frames[(int)rnanKeyframeNumberBox.Value];

                rnan.sequences[rnanSequencesListBox.SelectedIndex].loopType = rnanLoopModeDropdown.SelectedIndex;
                frame.cellID = (int)rnanRigCellIDNumberBox.Value;
                frame.duration = (int)rnanDurationNumberBox.Value;
                frame.xPos = (short)rnanXPosNumberBox.Value;
                frame.yPos = (short)rnanYPosNumberBox.Value;
                frame.rot = (float)rnanRotationNumberBox.Value;
                while (frame.rot < 0) frame.rot += 360;
                frame.xScale = (float)rnanXScaleNumberBox.Value;
                frame.yScale = (float)rnanYScaleNumberBox.Value;
            }
        }

        private void ramnSeqListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            RAMNFile ramnFile = frontAnimationCheckBox.Checked ? sprite.frontRAMN : sprite.backRAMN;
            if (ramnSeqListBox.SelectedIndex < 0) return;
            RAMNSubsequence ramnSeq = ramnFile.sequences[(int)animSequenceNumberBox.Value].subSequences[ramnSeqListBox.SelectedIndex];
            if (ramnSeqListBox.Enabled)
            {
                int frame = 0;
                for (int i = 0; i < ramnSeqListBox.SelectedIndex; i++)
                {
                    frame += sprite.frontRAMN.sequences[animSequence].subSequences[i].duration;
                }
                animationFrameNumberBox.Value = frame;
                UpdateRNANData();
            
                manualRNANChange = false;
                rcmnGroupNumberBox.Value = ramnSeq.rcmnGroupID;
                manualRNANChange = true;
                UpdateRCMNList();
            }
            rcmnGroupNumberBox.Value = ramnSeq.rcmnGroupID;
        }

        private void frontAnimationCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            manualRNANChange = false;
            animSequenceNumberBox.Value = 0;
            animSequence = 0;
            animFrame = 0;
            rcmnGroupNumberBox.Value = 0;
            manualRNANChange = true;

            UpdateRCMNList();
            ramnSeqListBox.Items.Clear();
            if (frontAnimationCheckBox.Checked)
            {
                for (int i = 0; i < sprite.frontRAMN.sequences[animSequence].subSequences.Count; i++)
                {
                    ramnSeqListBox.Items.Add("Sequence " + sprite.frontRAMN.sequences[animSequence].subSequences[i].rcmnGroupID);
                }
            }
            else
            {
                for (int i = 0; i < sprite.backRAMN.sequences[animSequence].subSequences.Count; i++)
                {
                    ramnSeqListBox.Items.Add("Sequence " + sprite.backRAMN.sequences[animSequence].subSequences[i].rcmnGroupID);
                }
            }
        }

        private void UpdateRCMNList()
        {
            int indexBefore = animCellsListBox.SelectedIndex;
            animCellsListBox.Items.Clear();
            if (frontAnimationCheckBox.Checked)
            {
                for (int i = 0; i < sprite.frontRCMN.groups[(int)rcmnGroupNumberBox.Value].cells.Count; i++)
                {
                    List<int> usedCells = new List<int>();
                    foreach (var v in sprite.frontRNAN.sequences[sprite.frontRCMN.groups[(int)rcmnGroupNumberBox.Value].cells[i].sequenceID].frames) if (!usedCells.Contains(v.cellID) && v.duration != 0) usedCells.Add(v.cellID);
                    string s = usedCells.Count == 0 ? "???" : usedCells[0].ToString();
                    if (usedCells.Count > 1) s += ", " + usedCells[1];
                    if (usedCells.Count > 2) s += ", " + usedCells[2];
                    if (usedCells.Count > 3) s += "...";
                    animCellsListBox.Items.Insert(0, s);
                }
            }
            else
            {
                for (int i = 0; i < sprite.backRCMN.groups[(int)rcmnGroupNumberBox.Value].cells.Count; i++)
                {
                    List<int> usedCells = new List<int>();
                    foreach (var v in sprite.backRNAN.sequences[sprite.backRCMN.groups[(int)rcmnGroupNumberBox.Value].cells[i].sequenceID].frames) if (!usedCells.Contains(v.cellID) && v.duration != 0) usedCells.Add(v.cellID);
                    string s = usedCells.Count == 0 ? "???" : usedCells[0].ToString();
                    if (usedCells.Count > 1) s += ", " + usedCells[1];
                    if (usedCells.Count > 2) s += ", " + usedCells[2];
                    if (usedCells.Count > 3) s += "...";
                    animCellsListBox.Items.Insert(0, s);
                }
            }

            List<RNANSequence> rnans = frontAnimationCheckBox.Checked ? sprite.frontRNAN.sequences : sprite.backRNAN.sequences;
            rnanSequencesListBox.Items.Clear();
            for (int i = 0; i < rnans.Count; i++)
            {
                rnanSequencesListBox.Items.Add(i);
            }

            if (animCellsListBox.Items.Count > indexBefore && indexBefore >= 0)
                animCellsListBox.SelectedIndex = indexBefore;
            else
                animCellsListBox.SelectedIndex = 0;
        }

        private void addRCMNButton_Click(object sender, EventArgs e)
        {
            int id = 0;
            if (frontAnimationCheckBox.Checked)
            {
                while (sprite.frontRCMN.groups[(int)rcmnGroupNumberBox.Value].cells.Any(c => c.id == id)) id++;
                sprite.frontRCMN.groups[(int)rcmnGroupNumberBox.Value].cells.Insert(0, new RCMNCell() { id = id });
            }
            else
            {
                while (sprite.backRCMN.groups[(int)rcmnGroupNumberBox.Value].cells.Any(c => c.id == id)) id++;
                sprite.backRCMN.groups[(int)rcmnGroupNumberBox.Value].cells.Insert(0, new RCMNCell() { id = id });
            }
            UpdateRCMNList();
            animCellsListBox.SelectedIndex = animCellsListBox.Items.Count - 1;
        }

        private void removeRCMNButton_Click(object sender, EventArgs e)
        {
            int cell = animCellsListBox.SelectedIndex;
            if (cell < 0) cell = 0;
            if (frontAnimationCheckBox.Checked) sprite.frontRCMN.groups[(int)rcmnGroupNumberBox.Value].cells.RemoveAt(animCellsListBox.Items.Count - 1 - cell);
            else sprite.backRCMN.groups[(int)rcmnGroupNumberBox.Value].cells.RemoveAt(animCellsListBox.Items.Count - 1 - cell);
            UpdateRCMNList();
            animCellsListBox.SelectedIndex = Math.Min(cell, animCellsListBox.Items.Count - 1);
        }

        private void moveRCMNUpButton_Click(object sender, EventArgs e)
        {
            if (animCellsListBox.SelectedIndex > 0)
            {
                int i = animCellsListBox.Items.Count - 1 - animCellsListBox.SelectedIndex;
                if (frontAnimationCheckBox.Checked)
                {
                    var cell = sprite.frontRCMN.groups[(int)rcmnGroupNumberBox.Value].cells[animCellsListBox.Items.Count - 1 - animCellsListBox.SelectedIndex];
                    sprite.frontRCMN.groups[(int)rcmnGroupNumberBox.Value].cells.Remove(cell);
                    sprite.frontRCMN.groups[(int)rcmnGroupNumberBox.Value].cells.Insert(i + 1, cell);
                }
                else
                {
                    var cell = sprite.backRCMN.groups[(int)rcmnGroupNumberBox.Value].cells[animCellsListBox.Items.Count - 1 - animCellsListBox.SelectedIndex];
                    sprite.backRCMN.groups[(int)rcmnGroupNumberBox.Value].cells.Remove(cell);
                    sprite.backRCMN.groups[(int)rcmnGroupNumberBox.Value].cells.Insert(i + 1, cell);
                }
                UpdateRCMNList();
                animCellsListBox.SelectedIndex = animCellsListBox.Items.Count - i - 2;
            }
        }

        private void moveRCMNDownButton_Click(object sender, EventArgs e)
        {
            if (animCellsListBox.SelectedIndex >= 0 && animCellsListBox.SelectedIndex < animCellsListBox.Items.Count - 1)
            {
                int i = animCellsListBox.Items.Count - 1 - animCellsListBox.SelectedIndex;
                if (frontAnimationCheckBox.Checked)
                {
                    var cell = sprite.frontRCMN.groups[(int)rcmnGroupNumberBox.Value].cells[animCellsListBox.Items.Count - 1 - animCellsListBox.SelectedIndex];
                    sprite.frontRCMN.groups[(int)rcmnGroupNumberBox.Value].cells.Remove(cell);
                    sprite.frontRCMN.groups[(int)rcmnGroupNumberBox.Value].cells.Insert(i - 1, cell);
                }
                else
                {
                    var cell = sprite.backRCMN.groups[(int)rcmnGroupNumberBox.Value].cells[animCellsListBox.Items.Count - 1 - animCellsListBox.SelectedIndex];
                    sprite.backRCMN.groups[(int)rcmnGroupNumberBox.Value].cells.Remove(cell);
                    sprite.backRCMN.groups[(int)rcmnGroupNumberBox.Value].cells.Insert(i - 1, cell);
                }
                UpdateRCMNList();
                animCellsListBox.SelectedIndex = animCellsListBox.Items.Count - i;
            }
        }

        private void addRNANButton_Click(object sender, EventArgs e)
        {
            List<RNANSequence> rnans = frontAnimationCheckBox.Checked ? sprite.frontRNAN.sequences : sprite.backRNAN.sequences;
            rnans.Add(new RNANSequence() { loopType = 2, cellType = 2 });
            rnans[rnans.Count - 1].frames.Add(new RNANFrame());
            rnanSequencesListBox.Items.Clear();
            for (int i = 0; i < rnans.Count; i++)
            {
                rnanSequencesListBox.Items.Add(i);
            }
        }

        private void removeRNANButton_Click(object sender, EventArgs e)
        {
            List<RNANSequence> rnans = frontAnimationCheckBox.Checked ? sprite.frontRNAN.sequences : sprite.backRNAN.sequences;
            if (rnanSequencesListBox.SelectedIndex >= 0)
                rnans.RemoveAt(rnanSequencesListBox.SelectedIndex);
            rnanSequencesListBox.Items.Clear();
            for (int i = 0; i < rnans.Count; i++)
            {
                rnanSequencesListBox.Items.Add(i);
            }
        }

        private void addRnanFrameButton_Click(object sender, EventArgs e)
        {
            List<RNANSequence> rnans = frontAnimationCheckBox.Checked ? sprite.frontRNAN.sequences : sprite.backRNAN.sequences;
            if (rnanSequencesListBox.SelectedIndex >= 0)
            {
                RNANFrame last = rnans[rnanSequencesListBox.SelectedIndex].frames[rnans[rnanSequencesListBox.SelectedIndex].frames.Count - 1];
                rnans[rnanSequencesListBox.SelectedIndex].frames.Add(new RNANFrame()
                {
                    cellID = last.cellID,
                    duration = last.duration,
                    rot = last.rot,
                    xPos = last.xPos,
                    yPos = last.yPos,
                    xScale = last.xScale,
                    yScale = last.yScale
                });
                rnanKeyframeNumberBox.Maximum = rnans[rnanSequencesListBox.SelectedIndex].frames.Count - 1;
                rnanKeyframeNumberBox.Value = rnanKeyframeNumberBox.Maximum;
                rnanKeyframesLabel.Text = "/ " + (rnans[rnanSequencesListBox.SelectedIndex].frames.Count - 1);
            }
        }

        private void removeRnanFrameButton_Click(object sender, EventArgs e)
        {
            List<RNANSequence> rnans = frontAnimationCheckBox.Checked ? sprite.frontRNAN.sequences : sprite.backRNAN.sequences;
            if (rnanSequencesListBox.SelectedIndex >= 0 && rnans[rnanSequencesListBox.SelectedIndex].frames.Count > 1)
            {
                rnans[rnanSequencesListBox.SelectedIndex].frames.RemoveAt((int)rnanKeyframeNumberBox.Value);
                rnanKeyframeNumberBox.Maximum = rnans[rnanSequencesListBox.SelectedIndex].frames.Count - 1;
                rnanKeyframesLabel.Text = "/ " + (rnans[rnanSequencesListBox.SelectedIndex].frames.Count - 1);
            }
        }

        private void addRamnSequenceButton_Click(object sender, EventArgs e)
        {
            RAMNFile ramn = frontAnimationCheckBox.Checked ? sprite.frontRAMN : sprite.backRAMN;
            if (ramnSeqListBox.SelectedIndices.Count > 0)
            {
                int last = ramnSeqListBox.SelectedIndices[0];
                List<RAMNSubsequence> toCopy = new List<RAMNSubsequence>();
                foreach (int i in ramnSeqListBox.SelectedIndices)
                {
                    toCopy.Add(ramn.sequences[(int)animSequenceNumberBox.Value].subSequences[i]);
                    if (i > last) last = i;
                }
                foreach (var selected in toCopy)
                {
                    last++;
                    ramn.sequences[(int)animSequenceNumberBox.Value].subSequences.Insert(last, new RAMNSubsequence() { rcmnGroupID = selected.rcmnGroupID, duration = selected.duration, rot = selected.rot, xPos = selected.xPos, yPos = selected.yPos, xScale = selected.xScale, yScale = selected.yScale });
                    ramnSeqListBox.Items.Insert(last, "Sequence " + selected.rcmnGroupID);
                }
                ramnSeqListBox.SelectedItems.Clear();
                ramnSeqListBox.SelectedIndex = last;
            }
            else
            {
                ramn.sequences[(int)animSequenceNumberBox.Value].subSequences.Add(new RAMNSubsequence() { duration = 6 });
                ramnSeqListBox.Items.Add("Sequence 0");
                ramnSeqListBox.SelectedItems.Clear();
                ramnSeqListBox.SelectedIndex = ramnSeqListBox.Items.Count - 1;
            }
        }

        private void removeRamnSequenceButton_Click(object sender, EventArgs e)
        {
            RAMNFile ramn = frontAnimationCheckBox.Checked ? sprite.frontRAMN : sprite.backRAMN;
            if (ramnSeqListBox.SelectedIndices.Count > 0)
            {
                List<int> toRemove = new List<int>();
                foreach (int i in ramnSeqListBox.SelectedIndices) toRemove.Add(i);
                toRemove.Sort((i1, i2) => i2.CompareTo(i1));
                foreach (int i in toRemove)
                {
                    ramn.sequences[(int)animSequenceNumberBox.Value].subSequences.RemoveAt(i);
                    ramnSeqListBox.Items.RemoveAt(i);
                }
            }
        }

        private void addRCMNGroupButton_Click(object sender, EventArgs e)
        {
            ramnRCMNGroupNumberBox.Maximum += 1;
            rcmnGroupNumberBox.Maximum += 1;
            rcmnGroupCountLabel.Text = "/ " + ramnRCMNGroupNumberBox.Maximum;
            rcmnGroupCountLabel2.Text = "/ " + ramnRCMNGroupNumberBox.Maximum;
            RAMNFile ramn = frontAnimationCheckBox.Checked ? sprite.frontRAMN : sprite.backRAMN;
            RCMNFile rcmn = frontAnimationCheckBox.Checked ? sprite.frontRCMN : sprite.backRCMN;
            RCMNGroup copy = rcmn.groups[(int)rcmnGroupNumberBox.Value];

            rcmn.groups.Add(new RCMNGroup());
            foreach (RCMNCell cell in copy.cells)
            {
                rcmn.groups[rcmn.groups.Count - 1].cells.Add(new RCMNCell()
                {
                    frameMode = cell.frameMode,
                    id = cell.id,
                    sequenceID = cell.sequenceID,
                    xOffset = cell.xOffset,
                    yOffset = cell.yOffset
                });
            }
        }

        private void removeRCMNGroupButton_Click(object sender, EventArgs e)
        {
            RAMNFile ramn = frontAnimationCheckBox.Checked ? sprite.frontRAMN : sprite.backRAMN;
            RCMNFile rcmn = frontAnimationCheckBox.Checked ? sprite.frontRCMN : sprite.backRCMN;
            RCMNGroup group = rcmn.groups[(int)rcmnGroupNumberBox.Value];

            for (int i = 0; i < ramn.sequences.Count; i++)
            {
                if (ramn.sequences[i].subSequences.Exists(s => s.rcmnGroupID == rcmn.groups.IndexOf(group)))
                {
                    MessageBox.Show("Cannot remove rcmn group while it is in use by a RAMN sequence");
                    return;
                }
            }

            if (rcmn.groups.Count > 1)
            {
                foreach (RAMNSequence rseq in ramn.sequences) foreach (RAMNSubsequence rsub in rseq.subSequences)
                    {
                        if (rsub.rcmnGroupID > rcmn.groups.IndexOf(group)) rsub.rcmnGroupID--;
                    }
                rcmn.groups.Remove(group);
                ramnRCMNGroupNumberBox.Maximum -= 1;
                rcmnGroupNumberBox.Maximum -= 1;
                rcmnGroupCountLabel.Text = "/ " + ramnRCMNGroupNumberBox.Maximum;
                rcmnGroupCountLabel2.Text = "/ " + ramnRCMNGroupNumberBox.Maximum;
            }
        }

        private void rcmnGroupNumberBox_ValueChanged(object sender, EventArgs e)
        {
            UpdateRCMNList();
        }

        private void saveAnimationDataButton_Click(object sender, EventArgs e)
        {
            sprite.WriteAnimationFiles();
        }

        public void ClickTimelineRAMN(RAMNSubsequence ramn, int id)
        {
            animRCMNPanel.Visible = false;
            animRNANPanel.Visible = false;
            animRAMNPanel.Visible = true;

            selectedRAMN = ramn;
            selectedRCMN = null;
            selectedRNAN = null;
            ramnIDLabel.Text = "RAMN: Sequence " + id + ", RCMN Group " + ramn.rcmnGroupID;
            tlRAMNLoopModeDropdown.SelectedIndex = tlBackRigCheckbox.Checked ? sprite.backRAMN.sequences[RAMNStopSequence].loopMode : sprite.frontRAMN.sequences[RAMNStopSequence].loopMode;

            tlRAMNCellGroupNumberBox.Maximum = tlBackRigCheckbox.Checked ? sprite.backRCMN.groups.Count - 1 : sprite.frontRCMN.groups.Count - 1;
            tlRAMNCellGroupCountLabel.Text = "/ " + tlRAMNCellGroupNumberBox.Maximum;
            tlRAMNCellGroupNumberBox.Value = ramn.rcmnGroupID;
            tlRAMNDurationNumberBox.Value = ramn.duration;
            tlRAMNXPosNumberBox.Value = (short)ramn.xPos;
            tlRAMNYPosNumberBox.Value = (short)ramn.yPos;
            tlRAMNXScaleNumberBox.Value = (decimal)ramn.xScale;
            tlRAMNYScaleNumberBox.Value = (decimal)ramn.yScale;
            tlRAMNRotationNumberBox.Value = (decimal)ramn.rot;
        }

        public void ClickTimelineRCMN(RCMNCell rcmn, int groupID, int cellID)
        {
            animRNANPanel.Visible = false;
            animRAMNPanel.Visible = false;
            animRCMNPanel.Visible = true;

            selectedRAMN = null;
            selectedRCMN = rcmn;
            selectedRCMNGroup = groupID;
            selectedRNAN = null;
            rcmnIDLabel.Text = "RCMN: Group " + groupID + ", Cell " + cellID;

            tlRCMNSequenceNumberBox.Maximum = tlBackRigCheckbox.Checked ? sprite.backRNAN.sequences.Count - 1 : sprite.frontRNAN.sequences.Count - 1;
            tlRCMNSequenceCountLabel.Text = "/ " + tlRCMNSequenceNumberBox.Maximum;
            tlRCMNSequenceNumberBox.Value = rcmn.sequenceID;

            tlRCMNXPosNumberBox.Value = rcmn.xOffset;
            tlRCMNYPosNumberBox.Value = rcmn.yOffset;
        }

        public void ClickTimelineRNAN(RNANFrame rnan, RNANSequence sequence)
        {
            animRAMNPanel.Visible = false;
            animRCMNPanel.Visible = false;
            animRNANPanel.Visible = true;

            selectedRAMN = null;
            selectedRCMN = null;
            selectedRNAN = rnan;
            selectedRNANSeq = sequence;
            rnanFrameIDLabel.Text = "RNAN: Sequence " + (tlBackRigCheckbox.Checked ? sprite.backRNAN.sequences.IndexOf(sequence) : sprite.frontRNAN.sequences.IndexOf(sequence)) + ", Frame " + sequence.frames.IndexOf(rnan);
            tlRNANLoopModeDropdown.SelectedIndex = sequence.loopType;

            tlKeyframeRigCellNumberBox.Value = rnan.cellID;
            tlKeyframeDurationNumberBox.Value = rnan.duration;
            tlKeyframeXPosNumberBox.Value = rnan.xPos;
            tlKeyframeYPosNumberBox.Value = rnan.yPos;
            tlKeyframeXScaleNumberBox.Value = (decimal)rnan.xScale;
            tlKeyframeYScaleNumberBox.Value = (decimal)rnan.yScale;
            tlKeyframeRotationNumberBox.Value = (decimal)rnan.rot;
        }

        public void PlayTimeline(object sender, EventArgs e)
        {
            atc.playing = !atc.playing;
            tlPlayButton.ImageIndex = atc.playing ? 1 : 0;
        }

        private void TimelineNextFrame(object sender, EventArgs e)
        {
            atc.NextFrame();
        }

        private void TimelinePrevFrame(object sender, EventArgs e)
        {
            atc.PreviousFrame();
        }

        private void SetTimelineRig(object sender, EventArgs e)
        {
            tlStopSequenceDropdown.SelectedIndex = 0;
            if (atc.playing)
            {
                PlayTimeline(null, null);
            }
            atc.animTime = 0;
            atp.SetRig(tlBackRigCheckbox.Checked, tlFemaleRigCheckbox.Checked);
            atc.back = tlBackRigCheckbox.Checked;
            atp.back = tlBackRigCheckbox.Checked;
            ClickTimelineRAMN(atc.back ? sprite.backRAMN.sequences[RAMNStopSequence].subSequences[0] : sprite.frontRAMN.sequences[RAMNStopSequence].subSequences[0], 0);
        }

        private void tlRAMNLoopModeDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            RAMNFile ramn = tlBackRigCheckbox.Checked ? sprite.backRAMN : sprite.frontRAMN;
            if (tlRAMNLoopModeDropdown.SelectedIndex >= 0) ramn.sequences[RAMNStopSequence].loopMode = tlRAMNLoopModeDropdown.SelectedIndex;
        }
        private void tlRAMNCellGroupNumberBox_ValueChanged(object sender, EventArgs e) => selectedRAMN.rcmnGroupID = (int)tlRAMNCellGroupNumberBox.Value;
        private void tlRAMNDurationNumberBox_ValueChanged(object sender, EventArgs e)
        {
            if (tlRAMNDurationNumberBox.Value <= 0) tlRAMNDurationNumberBox.Value = 1;
            else selectedRAMN.duration = (int)tlRAMNDurationNumberBox.Value;
        }
        private void tlRAMNXPosNumberBox_ValueChanged(object sender, EventArgs e) => selectedRAMN.xPos = (short)tlRAMNXPosNumberBox.Value;
        private void tlRAMNYPosNumberBox_ValueChanged(object sender, EventArgs e) => selectedRAMN.yPos = (short)tlRAMNYPosNumberBox.Value;
        private void tlRAMNXScaleNumberBox_ValueChanged(object sender, EventArgs e) => selectedRAMN.xScale = (float)tlRAMNXScaleNumberBox.Value;
        private void tlRAMNYScaleNumberBox_ValueChanged(object sender, EventArgs e) => selectedRAMN.yScale = (float)tlRAMNYScaleNumberBox.Value;
        private void tlRAMNRotationNumberBox_ValueChanged(object sender, EventArgs e) => selectedRAMN.rot = (float)tlRAMNRotationNumberBox.Value;

        private void tlRAMNAddSequenceButton_Click(object sender, EventArgs e)
        {
            if (selectedRAMN == null) return;
            RAMNFile ramn = tlBackRigCheckbox.Checked ? sprite.backRAMN : sprite.frontRAMN;
            int last = ramn.sequences[RAMNStopSequence].subSequences.IndexOf(selectedRAMN) + 1;
            ramn.sequences[(int)animSequenceNumberBox.Value].subSequences.Insert(last, new RAMNSubsequence() { rcmnGroupID = selectedRAMN.rcmnGroupID, duration = selectedRAMN.duration, rot = selectedRAMN.rot, xPos = selectedRAMN.xPos, yPos = selectedRAMN.yPos, xScale = selectedRAMN.xScale, yScale = selectedRAMN.yScale });
        }

        private void tlRAMNRemoveSequenecButton_Click(object sender, EventArgs e)
        {
            if (selectedRAMN == null) return;
            RAMNFile ramn = tlBackRigCheckbox.Checked ? sprite.backRAMN : sprite.frontRAMN;
            ramn.sequences[RAMNStopSequence].subSequences.Remove(selectedRAMN);
            selectedRAMN = null;
        }

        private void tlRCMNSequenceNumberBox_ValueChanged(object sender, EventArgs e) => selectedRCMN.sequenceID = (short)tlRCMNSequenceNumberBox.Value;

        private void tlRCMNXPosNumberBox_ValueChanged(object sender, EventArgs e) => selectedRCMN.xOffset = (short)tlRCMNXPosNumberBox.Value;
        private void tlRCMNYPosNumberBox_ValueChanged(object sender, EventArgs e) => selectedRCMN.yOffset = (short)tlRCMNYPosNumberBox.Value;

        private void tlAddRCMNCellButton_Click(object sender, EventArgs e)
        {
            if (selectedRCMN == null) return;
            RCMNCell newCell;
            if (tlFrontRigCheckbox.Checked)
            {
                sprite.frontRCMN.groups[selectedRCMNGroup].cells.Add(newCell = new RCMNCell() { id = sprite.frontRCMN.groups[selectedRCMNGroup].cells.Count });
            }
            else
            {
                sprite.backRCMN.groups[selectedRCMNGroup].cells.Add(newCell = new RCMNCell() { id = sprite.backRCMN.groups[selectedRCMNGroup].cells.Count });
            }
            ClickTimelineRCMN(newCell, selectedRCMNGroup, newCell.id);
        }

        private void tlRemoveRCMNCellButton_Click(object sender, EventArgs e)
        {
            if (selectedRCMN == null) return;
            var list = tlBackRigCheckbox.Checked ? sprite.backRCMN.groups[selectedRCMNGroup].cells : sprite.frontRCMN.groups[selectedRCMNGroup].cells;
            if (list.Count == 1) return;
            int id = Math.Max(list.IndexOf(selectedRCMN) - 1, 0);
            list.Remove(selectedRCMN);
            for (int i = 0; i < list.Count; i++) list[i].id = i;
            ClickTimelineRCMN(list[id], selectedRCMNGroup, id);
        }

        private void tlRAMNAddCellGroupButton_Click(object sender, EventArgs e)
        {
            if (selectedRAMN == null) return;
            RAMNFile ramn = tlFrontRigCheckbox.Checked ? sprite.frontRAMN : sprite.backRAMN;
            RCMNFile rcmn = tlFrontRigCheckbox.Checked ? sprite.frontRCMN : sprite.backRCMN;
            RCMNGroup copy = rcmn.groups[selectedRAMN.rcmnGroupID];

            rcmn.groups.Add(new RCMNGroup());
            foreach (RCMNCell cell in copy.cells)
            {
                rcmn.groups[rcmn.groups.Count - 1].cells.Add(new RCMNCell()
                {
                    frameMode = cell.frameMode,
                    id = cell.id,
                    sequenceID = cell.sequenceID,
                    xOffset = cell.xOffset,
                    yOffset = cell.yOffset
                });
            }
            ClickTimelineRAMN(selectedRAMN, ramn.sequences[RAMNStopSequence].subSequences.IndexOf(selectedRAMN));
        }

        private void tlRAMNRemoveCellGroupButton_Click(object sender, EventArgs e)
        {
            if (selectedRAMN == null) return;
            RAMNFile ramn = tlFrontRigCheckbox.Checked ? sprite.frontRAMN : sprite.backRAMN;
            RCMNFile rcmn = tlFrontRigCheckbox.Checked ? sprite.frontRCMN : sprite.backRCMN;
            RCMNGroup group = rcmn.groups[selectedRAMN.rcmnGroupID];

            if (rcmn.groups.Count > 1)
            {
                foreach (RAMNSequence rseq in ramn.sequences) foreach (RAMNSubsequence rsub in rseq.subSequences)
                    {
                        if (rsub.rcmnGroupID > selectedRAMN.rcmnGroupID) rsub.rcmnGroupID--;
                        if (rsub.rcmnGroupID == selectedRAMN.rcmnGroupID) rsub.rcmnGroupID = 0;
                    }
                rcmn.groups.Remove(group);
            }
            ClickTimelineRAMN(selectedRAMN, ramn.sequences[RAMNStopSequence].subSequences.IndexOf(selectedRAMN));
        }

        private void tlAddRNANSequenceButton_Click(object sender, EventArgs e)
        {
            if (selectedRCMN == null) return;
            RNANFile rnan = tlFrontRigCheckbox.Checked ? sprite.frontRNAN : sprite.backRNAN;
            rnan.sequences.Add(new RNANSequence()
            {
                frames = new List<RNANFrame>()
                {
                    new RNANFrame() { duration = 6 }
                }, cellType = 2, loopType = 2
            });
            ClickTimelineRCMN(selectedRCMN, selectedRCMNGroup, selectedRCMN.id);
        }

        private void tlRemoveRNANSequenceButton_Click(object sender, EventArgs e)
        {
            if (selectedRCMN == null) return;
            RNANFile rnan = tlFrontRigCheckbox.Checked ? sprite.frontRNAN : sprite.backRNAN;
            RCMNFile rcmn = tlFrontRigCheckbox.Checked ? sprite.frontRCMN : sprite.backRCMN;

            if (rnan.sequences.Count > 1)
            {
                foreach (RCMNGroup rg in rcmn.groups) foreach (RCMNCell rcell in rg.cells)
                    {
                        if (rcell.sequenceID > selectedRCMN.sequenceID) rcell.sequenceID--;
                        if (rcell.sequenceID == selectedRCMN.sequenceID) rcell.sequenceID = 0;
                    }
                rnan.sequences.RemoveAt(selectedRCMN.sequenceID);
            }
            ClickTimelineRCMN(selectedRCMN, selectedRCMNGroup, selectedRCMN.id);
        }

        private void tlKeyframeRigCellNumberBox_ValueChanged(object sender, EventArgs e) => selectedRNAN.cellID = (int)tlKeyframeRigCellNumberBox.Value;
        private void tlKeyframeDurationNumberBox_ValueChanged(object sender, EventArgs e)
        {
            if (tlKeyframeDurationNumberBox.Value <= 0) tlKeyframeDurationNumberBox.Value = 1;
            else selectedRNAN.duration = (int)tlKeyframeDurationNumberBox.Value;
        }
        private void tlKeyframeXPosNumberBox_ValueChanged(object sender, EventArgs e) => selectedRNAN.xPos = (short)tlKeyframeXPosNumberBox.Value;
        private void tlKeyframeYPosNumberBox_ValueChanged(object sender, EventArgs e) => selectedRNAN.yPos = (short)tlKeyframeYPosNumberBox.Value;
        private void tlKeyframeXScaleNumberBox_ValueChanged(object sender, EventArgs e) => selectedRNAN.xScale = (float)tlKeyframeXScaleNumberBox.Value;
        private void tlKeyframeYScaleNumberBox_ValueChanged(object sender, EventArgs e) => selectedRNAN.yScale = (float)tlKeyframeYScaleNumberBox.Value;
        private void tlKeyframeRotationNumberBox_ValueChanged(object sender, EventArgs e) => selectedRNAN.rot = (float)tlKeyframeRotationNumberBox.Value;

        private void tlAddKeyframeButton_Click(object sender, EventArgs e)
        {
            if (selectedRNAN == null) return;
            RNANFrame newFrame = new RNANFrame()
            {
                cellID = selectedRNAN.cellID,
                duration = selectedRNAN.duration,
                rot = selectedRNAN.rot,
                xPos = selectedRNAN.xPos,
                yPos = selectedRNAN.yPos,
                xScale = selectedRNAN.xScale,
                yScale = selectedRNAN.yScale
            };
            int i = selectedRNANSeq.frames.IndexOf(selectedRNAN) + 1;
            selectedRNANSeq.frames.Insert(i, newFrame);
            ClickTimelineRNAN(newFrame, selectedRNANSeq);
        }

        private void tlRemoveKeyframeButton_Click(object sender, EventArgs e)
        {
            if (selectedRNAN == null || selectedRNANSeq.frames.Where(r => r.duration > 0).Count() <= 1) return;

            int i = selectedRNANSeq.frames.IndexOf(selectedRNAN) - 1;
            selectedRNANSeq.frames.Remove(selectedRNAN);
            ClickTimelineRNAN(selectedRNANSeq.frames[Math.Max(i, 0)], selectedRNANSeq);
        }

        private void tlSaveAnimationButton_Click(object sender, EventArgs e)
        {
            sprite.WriteAnimationFiles();
        }

        private void tlStopSequenceDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tlStopSequenceDropdown.SelectedIndex < 0) return;

            RAMNFile ramn = tlFrontRigCheckbox.Checked ? sprite.frontRAMN : sprite.backRAMN;
            if (tlStopSequenceDropdown.SelectedIndex == 1 && ramn.sequences.Count == 1)
            {
                if (MessageBox.Show("This animation does not have a stop sequence. Would you like to create one?", "Create stop sequence", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    ramn.sequences.Add(new RAMNSequence()
                    {
                        cellType = ramn.sequences[0].cellType,
                        loopMode = ramn.sequences[0].loopMode,
                        motionType = 0,
                        subSequences = new List<RAMNSubsequence>()
                        {
                            new RAMNSubsequence()
                            {
                                duration = 6,
                            }
                        }
                    });
                }
                else
                {
                    tlStopSequenceDropdown.SelectedIndex = 0;
                    return;
                }
            }
            
            RAMNStopSequence = tlStopSequenceDropdown.SelectedIndex;
            ClickTimelineRAMN((tlFrontRigCheckbox.Checked ? sprite.frontRAMN : sprite.backRAMN).sequences[RAMNStopSequence].subSequences[0], 0);
        }

        private void tlRNANLoopModeDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tlRAMNLoopModeDropdown.SelectedIndex >= 0) selectedRNANSeq.loopType = tlRNANLoopModeDropdown.SelectedIndex;
        }

        private void stopSequenceHelpButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("During a battle, a Pokemon's animation may stop every 1-3 loops for 48, 56, or 64 frames. (chosen at random)\n" +
                "You can disable this function with the checkbox if you don't want the animation to ever stop. (often used for flying Pokemon)\n" +
                "You can also create a STOP sequence, which allows certain RCMN cells to keep playing while the rest of the animation is paused. (often used for secondary motion like fire)\n" +
                "Inside this text box, you can specify which RCMN cells to continue playing by entering their ids in a list separated by spaces, for example: \"1 2 4\".\n" +
                "Note that RCMN cells are not the same as rig cells. Refer to the animation editor to see which cells you want to freeze.\n" +
                "Cells will not appear frozen in the editor, however they will appear frozen in game if they are not included in this list.");
        }

        private void tlMoveCellUpButton_Click(object sender, EventArgs e)
        {
            if (selectedRCMN == null) return;

            RCMNFile rcmn = tlFrontRigCheckbox.Checked ? sprite.frontRCMN : sprite.backRCMN;
            int i = rcmn.groups[selectedRCMNGroup].cells.IndexOf(selectedRCMN);
            if (i == 0) return;
            rcmn.groups[selectedRCMNGroup].cells.Remove(selectedRCMN);
            rcmn.groups[selectedRCMNGroup].cells.Insert(i - 1, selectedRCMN);
        }

        private void tlMoveCellDownButton_Click(object sender, EventArgs e)
        {
            if (selectedRCMN == null) return;

            RCMNFile rcmn = tlFrontRigCheckbox.Checked ? sprite.frontRCMN : sprite.backRCMN;
            int i = rcmn.groups[selectedRCMNGroup].cells.IndexOf(selectedRCMN);
            if (i == rcmn.groups[selectedRCMNGroup].cells.Count - 1) return;
            rcmn.groups[selectedRCMNGroup].cells.Remove(selectedRCMN);
            rcmn.groups[selectedRCMNGroup].cells.Insert(i + 1, selectedRCMN);
        }
    }
}
