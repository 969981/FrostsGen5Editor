using NewEditor.Data;
using NewEditor.Data.NARCTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NewEditor.Forms
{
    public class AnimationTimeline
    {
        int totalDuration = 0;
        float zoom = 2;
        float timelineScroll = 0;
        List<RCMNCell> rcmnCells = new List<RCMNCell>();
        RCMNCell selectedCell;
        RAMNSequence ramnSeq;
        RAMNSubsequence ramn;
        RNANFrame hoveredFrame;
        static Rectangle displayRectangle = new Rectangle(506, 0, 640, 320);
        List<Rectangle> ramnRectangles = new List<Rectangle>();
        List<Rectangle> rnanRectangles = new List<Rectangle>();
        static Font font = new Font("Arial", 12);

        public void RenderImage(PokemonSpriteEntry sprite, Graphics g, int frame, Bitmap rig, bool back, int ramnSequence, int mouseX, int mouseY)
        {
            Pen pen = new Pen(Color.SlateGray, 1);
            g.Clear(Color.FromArgb(170, 170, 180));
            g.FillRectangle(new SolidBrush(Color.FromArgb(180, 200, 210)), displayRectangle);

            for (int i = 0; i < 40; i++)
            {
                g.DrawLines(pen, new Point[]
                {
                    new Point(506 + i * 16, 0),
                    new Point(506 + i * 16, 320),
                });
            }
            for (int i = 0; i < 20; i++)
            {
                g.DrawLines(pen, new Point[]
                {
                    new Point(506 + 0, i * 16),
                    new Point(1146, i * 16),
                });
            }

            lock (rig)
            {
                if (back ? ramnSequence >= sprite.backRAMN.sequences.Count : ramnSequence >= sprite.frontRAMN.sequences.Count) ramnSequence = 0;
                ramn = GetRAMNAtTime(sprite, frame, back, ramnSequence, out int ramnTime);

                rcmnCells = back ? sprite.backRCMN.groups[ramn.rcmnGroupID].cells : sprite.frontRCMN.groups[ramn.rcmnGroupID].cells.ToList();
                rcmnCells.Reverse();
                selectedCell = rcmnCells[0];

                totalDuration = 0;
                foreach (RAMNSubsequence s in ramnSeq.subSequences) totalDuration += s.duration;
                g.DrawString("Frame: " + frame + " / " + totalDuration, new Font("Arial", 16), new SolidBrush(Color.White), 8, 12);

                #region Preview Render
                foreach (RCMNCell rcmn in rcmnCells)
                {
                    RNANFrame rnan = GetRNANAtRAMNTime(sprite, ramn, rcmn, ramnTime, back);
                    if (rnan.xScale == 0 || rnan.yScale == 0) continue;
                    int cell = rnan.cellID;
                    if (cell >= (!back ? sprite.frontRigCells.cells.Count : sprite.backRigCells.cells.Count)) continue;
                    RigCell rc = !back ? sprite.frontRigCells.cells[cell] : sprite.backRigCells.cells[cell];
                    g.TranslateTransform(rc.spriteX, -rc.spriteY, MatrixOrder.Append);
                    g.ScaleTransform(rnan.xScale, rnan.yScale, MatrixOrder.Append);
                    g.RotateTransform(rnan.rot, MatrixOrder.Append);
                    g.TranslateTransform(rcmn.xOffset, rcmn.yOffset, MatrixOrder.Append);
                    g.TranslateTransform((short)rnan.xPos, (short)rnan.yPos, MatrixOrder.Append);
                    g.ScaleTransform(ramn.xScale, ramn.yScale, MatrixOrder.Append);
                    g.RotateTransform(ramn.rot, MatrixOrder.Append);
                    g.TranslateTransform((short)ramn.xPos, (short)ramn.yPos, MatrixOrder.Append);
                    g.ScaleTransform(zoom, zoom, MatrixOrder.Append);
                    g.TranslateTransform(820, 240, MatrixOrder.Append);
                    g.DrawImage(rig, new Rectangle(0, 0, rc.width, rc.height),
                            new Rectangle(rc.cellX, rc.cellY, rc.width, rc.height), GraphicsUnit.Pixel);

                    if (rcmn == selectedCell)
                    {
                        pen.Color = Color.Yellow;
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
                        g.ScaleTransform(rnan.xScale, rnan.yScale, MatrixOrder.Append);
                        g.RotateTransform(rnan.rot, MatrixOrder.Append);
                        g.TranslateTransform(rcmn.xOffset, rcmn.yOffset, MatrixOrder.Append);
                        g.TranslateTransform((short)rnan.xPos, (short)rnan.yPos, MatrixOrder.Append);
                        g.ScaleTransform(ramn.xScale, ramn.yScale, MatrixOrder.Append);
                        g.RotateTransform(ramn.rot, MatrixOrder.Append);
                        g.TranslateTransform((short)ramn.xPos, (short)ramn.yPos, MatrixOrder.Append);
                        g.ScaleTransform(zoom, zoom, MatrixOrder.Append);
                        g.TranslateTransform(820, 240, MatrixOrder.Append);
                        g.DrawImage(rig, new Rectangle(0, 0, rc.width, rc.height),
                                new Rectangle(rc.cellX, rc.cellY, rc.width, rc.height), GraphicsUnit.Pixel);

                        if (rcmn == selectedCell)
                        {
                            pen.Color = Color.Yellow;
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
                #endregion

                #region RAMN Segments
                int x = 0;
                int hue = 10;
                ramnRectangles.Clear();
                RAMNSequence seq = back ? sprite.backRAMN.sequences[ramnSequence] : sprite.frontRAMN.sequences[ramnSequence];
                SolidBrush brush = new SolidBrush(HelperFunctions.ColorFromHSV(hue, 0.3, 0.6));
                RNANFrame prepHoveredFrame = null;
                foreach (RAMNSubsequence sub in seq.subSequences)
                {
                    //Ramn
                    Rectangle r = new Rectangle(x - (int)timelineScroll, 340, sub.duration * 4, 320);
                    ramnRectangles.Add(r);
                    g.FillRectangle(brush, r);
                    g.DrawString(sub.rcmnGroupID.ToString(), font, new SolidBrush(Color.White), x + 4 - (int)timelineScroll, 340);
                    hue = (hue + 10) % 360;
                    brush.Color = HelperFunctions.ColorFromHSV(hue, 0.3, 0.6);
                    x += sub.duration * 4;

                    //Rnan
                    int y = 0;
                    int x2 = x - sub.duration * 4;
                    double val = 0.8;
                    rnanRectangles.Clear();
                    foreach (RCMNCell rcmn in back ? sprite.backRCMN.groups[sub.rcmnGroupID].cells : sprite.frontRCMN.groups[sub.rcmnGroupID].cells)
                    {
                        x2 = x - sub.duration * 4;
                        for (int t = 0; t < sub.duration; t += 0)
                        {
                            RNANFrame rFrame = GetRNANAtRAMNTime(sprite, sub, rcmn, t, back);
                            r = new Rectangle(x2 - (int)timelineScroll, 360 + y, rFrame.duration * 4, 16);
                            rnanRectangles.Add(r);
                            double sat = 0.4;
                            if (back ? sprite.backRNAN.sequences[rcmn.sequenceID].frames.IndexOf(rFrame) == 0 : sprite.frontRNAN.sequences[rcmn.sequenceID].frames.IndexOf(rFrame) == 0) sat = 1;
                            if (rFrame == hoveredFrame)
                            {
                                brush.Color = Color.White;
                            }
                            else brush.Color = HelperFunctions.ColorFromHSV(160 + rFrame.cellID * 20, sat, val);
                            g.FillRectangle(brush, r);
                            pen.Color = Color.Black;
                            g.DrawRectangle(pen, r);
                            if (r.Width >= 16)
                            {
                                g.DrawString(rFrame.cellID.ToString(), font, new SolidBrush(Color.Black), x2 - (int)timelineScroll, 360 + y);
                            }
                            x2 += rFrame.duration * 4;
                            t += rFrame.duration == 0 ? 1 : rFrame.duration;
                            if (r.Contains(mouseX, mouseY)) prepHoveredFrame = rFrame;
                        }
                        y += 20;
                        val -= 0.2;
                        if (val <= 0.4) val = 0.8;
                    }
                    brush = new SolidBrush(HelperFunctions.ColorFromHSV(hue, 0.3, 0.6));
                }
                hoveredFrame = prepHoveredFrame;
                #endregion
                pen.Color = Color.Black;
                g.DrawLines(pen, new Point[]
                {
                    new Point(frame * 4 - (int)timelineScroll, 360),
                    new Point(frame * 4 - (int)timelineScroll, 660),
                });
            }

            pen.Dispose();
        }

        RAMNSubsequence GetRAMNAtTime(PokemonSpriteEntry sprite, int frame, bool back, int sequenceID, out int ramnTime)
        {
            RAMNSequence seq = back ? sprite.backRAMN.sequences[sequenceID] : sprite.frontRAMN.sequences[sequenceID];
            ramnSeq = seq;
            int sub = 0;
            ramnTime = frame;
            int fakeRamnTime = ramnTime;
            while (seq.subSequences[sub].duration <= frame)
            {
                frame -= seq.subSequences[sub].duration;
                fakeRamnTime -= seq.subSequences[sub].duration;
                sub++;
                if (sub >= seq.subSequences.Count) sub = 0;
                if (sub == 0 || seq.subSequences[sub - 1].rcmnGroupID != seq.subSequences[sub].rcmnGroupID)
                {
                    ramnTime = fakeRamnTime;
                }
            }
            return seq.subSequences[sub];
        }

        RNANFrame GetRNANAtRAMNTime(PokemonSpriteEntry sprite, RAMNSubsequence ramn, RCMNCell rcmn, int frame, bool back)
        {
            RNANSequence seq = back ? sprite.backRNAN.sequences[rcmn.sequenceID] : sprite.frontRNAN.sequences[rcmn.sequenceID];
            int sub = 0;
            while (seq.frames[sub].duration <= frame)
            {
                frame -= seq.frames[sub].duration;
                sub++;
                if (sub >= seq.frames.Count) sub = 0;
            }
            return seq.frames[sub];
        }

        public void MouseDown(int mouseX, int mouseY)
        {

        }

        public void MouseUp()
        {

        }

        public void ScrollWheel(int mouseX, int mouseY, float amount)
        {
            if (displayRectangle.Contains(mouseX, mouseY))
            {
                zoom *= 1 + (float)amount / 1000;
                zoom = Math.Min(Math.Max(zoom, 1), 6);
            }

            if (ramnRectangles.ToList().Any(r => r.Contains(mouseX, mouseY)))
            {
                if (totalDuration * 4 < 1000) timelineScroll = 0;
                else
                {
                    timelineScroll -= (int)(amount / 4f);
                    timelineScroll = HelperFunctions.Clamp((int)timelineScroll, 0, totalDuration * 4 - 1000);
                }
            }
        }
    }
}
