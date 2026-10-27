using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Forms.Controls;
using NewEditor.Data;
using NewEditor.Data.NARCTypes;
using OpenTK.Audio.OpenAL;
using SharpDX.Direct3D9;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using static OpenTK.Graphics.OpenGL.GL;
using Color = Microsoft.Xna.Framework.Color;
using Cursors = System.Windows.Forms.Cursors;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using MouseEventHandler = System.Windows.Forms.MouseEventHandler;
using Point = System.Drawing.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace NewEditor.Forms
{
    public class AnimationTimelineControl : MonoGameControl
    {
        PokemonSpriteEntry spriteData;
        public double animTime = 0;
        public int totalDuration;
        int totalHeight;
        float timelineHScroll = -80;
        float timelineVScroll = 0;
        int timelineZoom = 4;
        public bool back = false;
        public bool playing = false;
        List<(RAMNSubsequence, Rectangle)> topRamnBlocks = new List<(RAMNSubsequence, Rectangle)>();
        List<(RAMNSubsequence, Rectangle)> ramnBlocks = new List<(RAMNSubsequence, Rectangle)>();
        List<(RNANFrame, Rectangle, bool)> rnanBlocks = new List<(RNANFrame, Rectangle, bool)>();
        List<(RCMNCell, Rectangle)> rcmnBlocks = new List<(RCMNCell, Rectangle)>();
        Dictionary<RAMNSubsequence, (int start, int end)> ramnTimings = new Dictionary<RAMNSubsequence, (int start, int end)>();
        RNANFrame hoveredFrame;
        RNANSequence hoveredFrameSequence;
        Rectangle? hoveredRNANRectangle;
        int hoveredRAMN;
        public RCMNCell hoveredRCMN;
        public int hoveredRigCell = -1;
        bool mouseDown = false;
        bool mouseHovering = false;
        bool dragMoved = false;
        Point panAnchor = Point.Empty;
        public PaletteEditor paletteEditor;

        public AnimationTimelineControl(PokemonSpriteEntry spriteData)
        {
            this.spriteData = spriteData;
        }

        protected override void Initialize()
        {
            Size = new Size(1024, 320);
            Location = new Point(20, 340);
            MouseWheel += new MouseEventHandler((sender, e) =>
            {
                ScrollWheel(e.X, e.Y, e.Delta);
            });
        }

        protected override void Update(GameTime gameTime)
        {
            if (playing)
            {
                animTime += gameTime.ElapsedGameTime.TotalSeconds * 60;
            }
            totalDuration = 0;
            totalHeight = 32;
            RAMNSequence ramn = !back ? spriteData.frontRAMN.sequences[paletteEditor.RAMNStopSequence] : spriteData.backRAMN.sequences[paletteEditor.RAMNStopSequence];
            foreach (RAMNSubsequence seq in ramn.subSequences)
            {
                totalDuration += seq.duration;
                int height = 32 + 28 * (back ? spriteData.backRCMN.groups[seq.rcmnGroupID].cells.Count : spriteData.frontRCMN.groups[seq.rcmnGroupID].cells.Count);
                if (height > totalHeight) totalHeight = height;
            }
            while (animTime >= totalDuration) animTime -= totalDuration;

            if (mouseDown)
            {
                Vector2 move = new Vector2(Editor.GetRelativeMousePosition.X - panAnchor.X, Editor.GetRelativeMousePosition.Y - panAnchor.Y);
                panAnchor = new Point(Editor.GetRelativeMousePosition.X, Editor.GetRelativeMousePosition.Y);
                timelineHScroll -= (int)move.X;
                timelineVScroll -= (int)move.Y;
                if (move.Length() > 1f) dragMoved = true;
            }
            if (totalDuration * timelineZoom - 1024 > -80)
                timelineHScroll = MathHelper.Clamp(timelineHScroll, -80, totalDuration * timelineZoom - 1024);
            else timelineHScroll = -80;
            if (totalHeight - 320 > 0)
                timelineVScroll = MathHelper.Clamp(timelineVScroll, 0, totalHeight - 320);
            else timelineVScroll = 0;

            topRamnBlocks.Clear();
            ramnBlocks.Clear();
            rnanBlocks.Clear();
            rcmnBlocks.Clear();
            hoveredRAMN = -1;
            hoveredFrame = null;
            hoveredFrameSequence = null;
            hoveredRNANRectangle = null;
            hoveredRCMN = null;
            hoveredRigCell = -1;

            ramnTimings.Clear();

            int x = 0;
            int time = 0;
            foreach (RAMNSubsequence seq in ramn.subSequences)
            {
                Rectangle r = new Rectangle(x - (int)timelineHScroll, 0, seq.duration * timelineZoom, 32);
                topRamnBlocks.Add((seq, r));
                if (mouseHovering && r.Contains(Editor.GetRelativeMousePosition.X, Editor.GetRelativeMousePosition.Y))
                    hoveredRAMN = ramn.subSequences.IndexOf(seq);

                r = new Rectangle(x - (int)timelineHScroll, -(int)timelineVScroll, seq.duration * timelineZoom, totalHeight - 4);
                ramnBlocks.Add((seq, r));
                ramnTimings.Add(seq, (time, time + seq.duration));
                time += seq.duration;
                x += seq.duration * timelineZoom;
            }

            int y = 31;
            int id = 0;
            RAMNSubsequence currentRamn = GetRAMNAtTime(out int ramnTime);
            foreach (RCMNCell rcmn in back ? spriteData.backRCMN.groups[currentRamn.rcmnGroupID].cells.ToList() : spriteData.frontRCMN.groups[currentRamn.rcmnGroupID].cells.ToList())
            {
                Rectangle r = new Rectangle(0, y - (int)timelineVScroll, 80, 26);
                rcmnBlocks.Add((rcmn, r));
                if (hoveredRAMN == -1 && mouseHovering && r.Contains(Editor.GetRelativeMousePosition.X, Editor.GetRelativeMousePosition.Y))
                {
                    hoveredRCMN = rcmn;
                }
                id++;
                y += 28;
            }

            y = 32;
            for (int c = 0; c < (totalHeight - 32) / 28; c++)
            {
                int rnanTime = 0;
                int prevRamnID = -1;
                if (y - (int)timelineVScroll > 320) break;
                if (y - (int)timelineVScroll + 24 < 0)
                {
                    y += 28;
                    continue;
                }
                for (int t = 0; t < totalDuration; t += 0)
                {
                    if (t * timelineZoom - (int)timelineHScroll > 1024) break;
                    RAMNSequence seq = back ? spriteData.backRAMN.sequences[paletteEditor.RAMNStopSequence] : spriteData.frontRAMN.sequences[paletteEditor.RAMNStopSequence];
                    RAMNSubsequence sub = GetRAMNAtTime(t, out int ramnTime2);
                    if (sub.rcmnGroupID != prevRamnID) rnanTime = 0;
                    while (back ? spriteData.backRCMN.groups[sub.rcmnGroupID].cells.Count <= c : spriteData.frontRCMN.groups[sub.rcmnGroupID].cells.Count <= c && t < totalDuration)
                    {
                        t = ramnTimings[sub].end;
                        sub = GetRAMNAtTime(t, out int ramnTime3);
                    }
                    if (t >= totalDuration) continue;
                    RCMNCell rcmn = back ? spriteData.backRCMN.groups[sub.rcmnGroupID].cells[c] : spriteData.frontRCMN.groups[sub.rcmnGroupID].cells[c];
                    RNANFrame rFrame = GetRNANAtRAMNTime(rcmn, rnanTime, back, out bool duplicateFrame);
                    int width = rFrame.duration;
                    if ((t + width) * timelineZoom - (int)timelineHScroll < 0)
                    {
                        rnanTime += width == 0 ? 1 : width;
                        t += width == 0 ? 1 : width;
                        prevRamnID = sub.rcmnGroupID;
                        continue;
                    }
                    if (t + width > ramnTimings[sub].end && seq.subSequences.IndexOf(sub) < seq.subSequences.Count - 1 && seq.subSequences[seq.subSequences.IndexOf(sub) + 1].rcmnGroupID != sub.rcmnGroupID)
                    {
                        width = ramnTimings[sub].end - t;
                    }

                    Rectangle r = new Rectangle(t * timelineZoom - (int)timelineHScroll, y - (int)timelineVScroll, rFrame.duration * timelineZoom, 24);
                    rnanBlocks.Add((rFrame, r, duplicateFrame));
                    if (hoveredRCMN == null && hoveredRAMN == -1 && mouseHovering && r.Contains(Editor.GetRelativeMousePosition.X, Editor.GetRelativeMousePosition.Y))
                    {
                        hoveredFrame = rFrame;
                        hoveredFrameSequence = back ? spriteData.backRNAN.sequences[rcmn.sequenceID] : spriteData.frontRNAN.sequences[rcmn.sequenceID];
                        hoveredRNANRectangle = r;
                        hoveredRigCell = rFrame.cellID;
                    }
                    rnanTime += width == 0 ? 1 : width;
                    t += width == 0 ? 1 : width;
                    prevRamnID = sub.rcmnGroupID;
                }
                y += 28;
            }
        }

        protected override void Draw()
        {
            Editor.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp);
            Editor.spriteBatch.Draw(Editor.Pixel, new Rectangle(-8, -8, 1200, 340), new Color(110, 110, 110));
            RAMNSubsequence currentRamn = GetRAMNAtTime(out int ramnTime);

            List<RNANFrame> usedFrames = new List<RNANFrame>();
            foreach (var r in ramnBlocks)
            {
                DrawBorderedRectangle(r.Item2, ramnBlocks.IndexOf(r) == hoveredRAMN ? Color.LightYellow : r.Item1 == paletteEditor.selectedRAMN ? new Color(160, 160, 140) : new Color(120, 120, 120));
            }
            foreach (var r2 in rnanBlocks)
            {
                DrawBorderedRectangle(r2.Item2, r2.Item1 == hoveredFrame ? Color.LightYellow : r2.Item1 == paletteEditor.selectedRNAN ? new Color(240, 240, 200) : usedFrames.Contains(r2.Item1) ? new Color(100, 100, 90) : new Color(160, 160, 150));
                if (r2.Item2.Width >= 20)
                    Editor.spriteBatch.DrawString(Editor.Font, r2.Item1.cellID.ToString(), new Vector2(r2.Item2.X + 4, r2.Item2.Y + 4), r2.Item1 == hoveredFrame || r2.Item1 == paletteEditor.selectedRNAN ? Color.Black : Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0.5f);
                if (!usedFrames.Contains(r2.Item1)) usedFrames.Add(r2.Item1);
            }
            Editor.spriteBatch.Draw(Editor.Pixel, new Rectangle((int)(animTime * timelineZoom - 1) - (int)timelineHScroll, 32 - (int)timelineVScroll, 2, 3000), Color.Yellow);
            foreach (var r in rcmnBlocks)
            {
                DrawBorderedRectangle(r.Item2, r.Item1 == hoveredRCMN ? Color.LightYellow : r.Item1 == paletteEditor.selectedRCMN ? new Color(240, 240, 200) : new Color(130, 160, 160));
                Editor.spriteBatch.DrawString(Editor.Font, "Cell " + currentRamn.rcmnGroupID + "-" + rcmnBlocks.IndexOf(r), new Vector2(r.Item2.X + 4, r.Item2.Y + 4), r.Item1 == hoveredRCMN || r.Item1 == paletteEditor.selectedRCMN ? Color.Black : Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, 0.5f);
            }
            foreach (var r in topRamnBlocks)
            {
                DrawBorderedRectangle(r.Item2, topRamnBlocks.IndexOf(r) == hoveredRAMN ? Color.LightYellow : r.Item1 == paletteEditor.selectedRAMN ? new Color(160, 160, 140) : new Color(120, 120, 120));
                if (r.Item2.Width >= 144)
                    Editor.spriteBatch.DrawString(Editor.Font, "Sequence " + r.Item1.rcmnGroupID.ToString(), new Vector2(r.Item2.X + 4, r.Item2.Y + 4), topRamnBlocks.IndexOf(r) == hoveredRAMN ? Color.Black : Color.White, 0, Vector2.Zero, 1.5f, SpriteEffects.None, 0.5f);
                else if (r.Item2.Width >= 80)
                    Editor.spriteBatch.DrawString(Editor.Font, "Seq " + r.Item1.rcmnGroupID.ToString(), new Vector2(r.Item2.X + 4, r.Item2.Y + 4), topRamnBlocks.IndexOf(r) == hoveredRAMN ? Color.Black : Color.White, 0, Vector2.Zero, 1.5f, SpriteEffects.None, 0.5f);
                else if (r.Item2.Width >= 16)
                    Editor.spriteBatch.DrawString(Editor.Font, r.Item1.rcmnGroupID.ToString(), new Vector2(r.Item2.X + 4, r.Item2.Y + 4), topRamnBlocks.IndexOf(r) == hoveredRAMN ? Color.Black : Color.White, 0, Vector2.Zero, 1.5f, SpriteEffects.None, 0.5f);
            }
            Editor.spriteBatch.End();
        }

        void DrawBorderedRectangle(Rectangle r, Color c, bool fill = true)
        {
            if (fill)
                Editor.spriteBatch.Draw(Editor.Pixel, r, c);
            Editor.spriteBatch.Draw(Editor.Pixel, new Rectangle(r.X, r.Y, r.Width, 1), fill ? Color.Lerp(c, Color.Black, 0.5f) : c);
            Editor.spriteBatch.Draw(Editor.Pixel, new Rectangle(r.X + r.Width - 1, r.Y, 1, r.Height), fill ? Color.Lerp(c, Color.Black, 0.5f) : c);
            Editor.spriteBatch.Draw(Editor.Pixel, new Rectangle(r.X, r.Y, 1, r.Height), fill ? Color.Lerp(c, Color.Black, 0.5f) : c);
            Editor.spriteBatch.Draw(Editor.Pixel, new Rectangle(r.X, r.Y + r.Height - 1, r.Width, 1), fill ? Color.Lerp(c, Color.Black, 0.5f) : c);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            mouseDown = true;
            dragMoved = false;
            panAnchor = new Point(Editor.GetRelativeMousePosition.X, Editor.GetRelativeMousePosition.Y);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            mouseDown = false;
            if (dragMoved) return;

            if (hoveredRCMN != null)
            {
                RAMNSubsequence currentRamn = GetRAMNAtTime(out int ramnTime);
                RCMNGroup rcmn = back ? spriteData.backRCMN.groups[currentRamn.rcmnGroupID] : spriteData.frontRCMN.groups[currentRamn.rcmnGroupID];
                paletteEditor.ClickTimelineRCMN(hoveredRCMN, currentRamn.rcmnGroupID, rcmn.cells.IndexOf(hoveredRCMN));
                if (playing)
                {
                    paletteEditor.PlayTimeline(null, null);
                }
            }
            else
            {
                if (hoveredRAMN >= 0)
                {
                    RAMNSequence ramn = !back ? spriteData.frontRAMN.sequences[paletteEditor.RAMNStopSequence] : spriteData.backRAMN.sequences[paletteEditor.RAMNStopSequence];
                    SetTimeToRAMN(ramn.subSequences[hoveredRAMN]);
                    paletteEditor.ClickTimelineRAMN(ramn.subSequences[hoveredRAMN], hoveredRAMN);
                    if (playing)
                    {
                        paletteEditor.PlayTimeline(null, null);
                    }
                }
                else if (hoveredFrame != null)
                {
                    animTime = (hoveredRNANRectangle.Value.X + timelineHScroll) / timelineZoom;
                    paletteEditor.ClickTimelineRNAN(hoveredFrame, hoveredFrameSequence);
                    if (playing)
                    {
                        paletteEditor.PlayTimeline(null, null);
                    }
                }
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hoveredFrame = null;
            hoveredRAMN = -1;
            mouseHovering = false;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            mouseHovering = true;
        }

        RAMNSubsequence GetRAMNAtTime(out int ramnTime)
        {
            RAMNSequence seq = back ? spriteData.backRAMN.sequences[paletteEditor.RAMNStopSequence] : spriteData.frontRAMN.sequences[paletteEditor.RAMNStopSequence];
            int sub = 0;
            int frame = (int)animTime;
            ramnTime = (int)animTime;
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

        RAMNSubsequence GetRAMNAtTime(int time, out int ramnTime)
        {
            RAMNSequence seq = back ? spriteData.backRAMN.sequences[paletteEditor.RAMNStopSequence] : spriteData.frontRAMN.sequences[paletteEditor.RAMNStopSequence];
            int sub = 0;
            int frame = time;
            ramnTime = time;
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

        RNANFrame GetRNANAtRAMNTime(RCMNCell rcmn, int frame, bool back, out bool duplicate)
        {
            RNANSequence seq = back ? spriteData.backRNAN.sequences[rcmn.sequenceID] : spriteData.frontRNAN.sequences[rcmn.sequenceID];
            int sub = 0;
            duplicate = false;
            while (seq.frames[sub].duration <= frame)
            {
                frame -= seq.frames[sub].duration;
                sub++;
                if (sub >= seq.frames.Count)
                {
                    duplicate = true;
                    sub = 0;
                }
            }
            return seq.frames[sub];
        }

        void SetTimeToRAMN(RAMNSubsequence ramn)
        {
            RAMNSequence seq = back ? spriteData.backRAMN.sequences[paletteEditor.RAMNStopSequence] : spriteData.frontRAMN.sequences[paletteEditor.RAMNStopSequence];
            int time = 0;
            for (int i = 0; i < seq.subSequences.IndexOf(ramn); i++)
            {
                time += seq.subSequences[i].duration;
            }
            if (time > animTime || time + ramn.duration <= animTime) animTime = time;
        }

        public void NextFrame()
        {
            int min = 0;
            foreach (var r in rnanBlocks)
            {
                float t = (r.Item2.X + timelineHScroll) / timelineZoom;
                if (t > animTime && (min == 0 || t < min)) min = (int)t;
            }
            animTime = min;
        }

        public void PreviousFrame()
        {
            int min = -1;
            foreach (var r in rnanBlocks)
            {
                float t = (r.Item2.X + timelineHScroll) / timelineZoom;
                if (t < animTime && (min == -1 || t > min)) min = (int)t;
            }
            if (min == -1 && animTime != totalDuration)
            {
                animTime = totalDuration;
                PreviousFrame();
            }
            else animTime = min;
        }

        public void ScrollWheel(int mouseX, int mouseY, float amount)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                timelineZoom += Math.Sign(amount);
                timelineZoom = MathHelper.Clamp(timelineZoom, 1, 12);
            }
            else if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) || totalHeight <= 312)
            {
                timelineHScroll -= amount;
            }
            else
            {
                timelineVScroll -= amount;
            }
            if (totalDuration * timelineZoom - 1024 > -80)
                timelineHScroll = MathHelper.Clamp(timelineHScroll, -80, totalDuration * timelineZoom - 1024);
            else timelineHScroll = -80;
            if (totalHeight - 320 > 0)
                timelineVScroll = MathHelper.Clamp(timelineVScroll, 0, totalHeight - 320);
            else timelineVScroll = 0;
        }
    }
}
