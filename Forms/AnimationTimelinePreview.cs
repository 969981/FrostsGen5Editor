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
using Color = Microsoft.Xna.Framework.Color;
using Point = System.Drawing.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace NewEditor.Forms
{
    public class AnimationTimelinePreview : MonoGameControl
    {
        PokemonSpriteEntry spriteData;
        double animTime = 0;
        int totalDuration;
        int timelineScroll = 0;
        int timelineZoom = 4;
        public bool back = false;
        Texture2D rig;
        float zoom = 2;
        Vector2 pan = Vector2.Zero;
        bool mouseDown = false;
        Point panAnchor = Point.Empty;
        RNANFrame hoveredFrame;
        RCMNCell hoveredRCMN;
        public AnimationTimelineControl timeline;

        public AnimationTimelinePreview(PokemonSpriteEntry spriteData)
        {
            this.spriteData = spriteData;
        }

        protected override void Initialize()
        {
            Size = new Size(480, 320);
            Location = new Point(564, 20);
            MouseWheel += new MouseEventHandler((sender, e) =>
            {
                ScrollWheel(e.X, e.Y, e.Delta);
            });

            SetRig(false, false);
        }

        public void SetRig(bool back, bool female)
        {
            if (Editor == null) return;
            //Port rig from Bitmap to Texture2D
            Bitmap src = spriteData.GetRig(false, back, female);
            if (src == null) src = spriteData.GetRig(false, back, false);
            Texture2D rig = new Texture2D(Editor.GraphicsDevice, src.Width, src.Height);
            Color[] c = new Color[src.Width * src.Height];
            for (int i = 0; i < c.Length; i++)
            {
                System.Drawing.Color pix = src.GetPixel(i % src.Width, i / src.Width);
                c[i] = pix != spriteData.palette[0] ? new Color(pix.R, pix.G, pix.B) : Color.Transparent;
            }
            rig.SetData(c);
            if (this.rig == null)
            {
                this.rig = rig;
            }
            else
            {
                lock (this.rig)
                {
                    this.rig = rig;
                }
            }
        }

        protected override void Update(GameTime gameTime)
        {
            animTime += gameTime.ElapsedGameTime.TotalSeconds * 60;
            int totalDuration = 0;
            RAMNSequence ramn = !back ? spriteData.frontRAMN.sequences[timeline.paletteEditor.RAMNStopSequence] : spriteData.backRAMN.sequences[timeline.paletteEditor.RAMNStopSequence];
            foreach (RAMNSubsequence seq in ramn.subSequences) totalDuration += seq.duration;
            while (animTime >= totalDuration) animTime -= totalDuration;

            if (mouseDown)
            {
                Vector2 move = new Vector2(Editor.GetRelativeMousePosition.X - panAnchor.X, Editor.GetRelativeMousePosition.Y - panAnchor.Y);
                panAnchor = new Point(Editor.GetRelativeMousePosition.X, Editor.GetRelativeMousePosition.Y);
                pan += move;
                pan = Vector2.Clamp(pan, new Vector2(-192, -128), new Vector2(192, 128));
            }
        }

        protected override void Draw()
        {
            lock (rig)
            {
                if (timeline != null) animTime = timeline.animTime;
                Editor.GraphicsDevice.Clear(new Color(180, 200, 210));

                Editor.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp);
                for (int i = 0; i <= 40; i++)
                {
                    if (i * 16 + (int)pan.X % 16 > 640 || i * 16 + (int)pan.X % 16 < 0) continue;
                    Editor.spriteBatch.Draw(Editor.Pixel, new Rectangle(i * 16 + (int)pan.X % 16, 0, 1, 320), Color.White);
                }
                for (int i = 0; i <= 20; i++)
                {
                    if (i * 16 + (int)pan.Y % 16 > 320 || i * 16 + (int)pan.Y % 16 < 0) continue;
                    Editor.spriteBatch.Draw(Editor.Pixel, i == 18 + (int)(pan.Y / 16) ? new Rectangle(0, i * 16 - 1 + (int)pan.Y % 16, 640, 3) : new Rectangle(0, i * 16 + (int)pan.Y % 16, 640, 1), i == 18 + (int)(pan.Y / 16) ? Color.Green : Color.White);
                }

                if (timeline != null)
                    Editor.spriteBatch.DrawString(Editor.Font, "Frame " + (int)timeline.animTime + " / " + timeline.totalDuration, new Vector2(8, 8), Color.Black, 0, Vector2.Zero, 2f, SpriteEffects.None, 0.6f);
                Editor.spriteBatch.End();

                totalDuration = 0;
                foreach (RAMNSubsequence s in back ? spriteData.backRAMN.sequences[timeline.paletteEditor.RAMNStopSequence].subSequences : spriteData.frontRAMN.sequences[timeline.paletteEditor.RAMNStopSequence].subSequences) totalDuration += s.duration;

                //Draw pokemon animation
                RAMNSubsequence ramn = GetRAMNAtTime(out int ramnTime);
                List<RCMNCell> rcmnCells = back ? spriteData.backRCMN.groups[ramn.rcmnGroupID].cells.ToList() : spriteData.frontRCMN.groups[ramn.rcmnGroupID].cells.ToList();
                rcmnCells.Reverse();
                foreach (RCMNCell rcmn in rcmnCells)
                {
                    RNANFrame rnan = GetRNANAtRAMNTime(ramn, rcmn, ramnTime, back);
                    int cell = rnan.cellID;
                    if (cell >= (!back ? spriteData.frontRigCells.cells.Count : spriteData.backRigCells.cells.Count)) continue;
                    RigCell rc = !back ? spriteData.frontRigCells.cells[cell] : spriteData.backRigCells.cells[cell];
                    Matrix mat = Matrix.CreateTranslation(new Vector3(rc.spriteX, -rc.spriteY, 0)) *
                        Matrix.CreateScale(rnan.xScale, rnan.yScale, 1) * Matrix.CreateRotationZ(MathHelper.ToRadians(rnan.rot)) *
                        Matrix.CreateTranslation(new Vector3(rcmn.xOffset, rcmn.yOffset, 0)) *
                        Matrix.CreateTranslation(new Vector3(rnan.xPos, rnan.yPos, 0)) *
                        Matrix.CreateScale(ramn.xScale, ramn.yScale, 1) * Matrix.CreateRotationZ(MathHelper.ToRadians(ramn.rot)) * Matrix.CreateTranslation(new Vector3((short)ramn.xPos, (short)ramn.yPos, 0));
                    mat *= Matrix.CreateScale(zoom, zoom, 1);
                    mat *= Matrix.CreateTranslation(new Vector3(240 + pan.X, 288 + pan.Y, 0));
                    Editor.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp, null, null, null, mat);
                    Editor.spriteBatch.Draw(rig, Vector2.Zero, new Rectangle(rc.cellX, rc.cellY, rc.width, rc.height), Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0.5f);
                    if (timeline != null && (timeline.hoveredRigCell == rnan.cellID || timeline.hoveredRCMN == rcmn)) DrawBorderedRectangle(new Rectangle(0, 0, rc.width, rc.height), Color.Yellow, false);
                    Editor.spriteBatch.End();

                    rc = rc.subCell;
                    if (rc.width > 0)
                    {
                        mat = Matrix.CreateTranslation(new Vector3(rc.spriteX, -rc.spriteY, 0)) *
                            Matrix.CreateScale(rnan.xScale, rnan.yScale, 1) * Matrix.CreateRotationZ(MathHelper.ToRadians(rnan.rot)) *
                            Matrix.CreateTranslation(new Vector3(rcmn.xOffset, rcmn.yOffset, 0)) *
                            Matrix.CreateTranslation(new Vector3(rnan.xPos, rnan.yPos, 0)) *
                            Matrix.CreateScale(ramn.xScale, ramn.yScale, 1) * Matrix.CreateRotationZ(MathHelper.ToRadians(ramn.rot)) * Matrix.CreateTranslation(new Vector3((short)ramn.xPos, (short)ramn.yPos, 0));
                        mat *= Matrix.CreateScale(zoom, zoom, 1);
                        mat *= Matrix.CreateTranslation(new Vector3(240 + pan.X, 288 + pan.Y, 0));
                        Editor.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp, null, null, null, mat);
                        Editor.spriteBatch.Draw(rig, Vector2.Zero, new Rectangle(rc.cellX, timeline.paletteEditor.timelineEyesClosedCheckBox.Checked ? (rc.cellY + rc.height) : rc.cellY, rc.width, rc.height), Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0.5f);
                        if (timeline != null && (timeline.hoveredRigCell == rnan.cellID || timeline.hoveredRCMN == rcmn)) DrawBorderedRectangle(new Rectangle(0, 0, rc.width, rc.height), Color.Yellow, false);
                        Editor.spriteBatch.End();
                    }
                }
            }
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
            panAnchor = new Point(Editor.GetRelativeMousePosition.X, Editor.GetRelativeMousePosition.Y);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            mouseDown = false;
        }

        RAMNSubsequence GetRAMNAtTime(out int ramnTime)
        {
            RAMNSequence seq = back ? spriteData.backRAMN.sequences[timeline.paletteEditor.RAMNStopSequence] : spriteData.frontRAMN.sequences[timeline.paletteEditor.RAMNStopSequence];
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

        RNANFrame GetRNANAtRAMNTime(RAMNSubsequence ramn, RCMNCell rcmn, int frame, bool back)
        {
            RNANSequence seq = back ? spriteData.backRNAN.sequences[rcmn.sequenceID] : spriteData.frontRNAN.sequences[rcmn.sequenceID];
            int sub = 0;
            while (seq.frames[sub].duration <= frame)
            {
                frame -= seq.frames[sub].duration;
                sub++;
                if (sub >= seq.frames.Count) sub = 0;
            }
            return seq.frames[sub];
        }

        public void ScrollWheel(int mouseX, int mouseY, float amount)
        {
            zoom *= 1 + (float)amount / 1000;
            zoom = Math.Min(Math.Max(zoom, 1), 6);
        }
    }
}
