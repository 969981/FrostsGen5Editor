using LibNDSFormats.NSBMD;
using LibNDSFormats.NSBTX;
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media.TextFormatting;
using Tao.OpenGl;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using static Tao.Platform.Windows.Winmm;

namespace NewEditor.Forms
{
    public partial class MapEditor : Form
    {
        public List<byte> npcSpriteIndex = MainEditor.fileSystem.narcs[MainEditor.RomType == RomType.BW2 ? VersionConstants.BW2_NPCSpriteIndexNARCID : VersionConstants.BW1_NPCSpriteIndexNARCID].GetFileEntry(0);

        public List<MapFileEntry> loadedMaps = new List<MapFileEntry>();
        public List<Point> loadedMapOffsets = new List<Point>();
        public MapTextureEntry currentTextures;
        public int currentAreaID = -1;

        public static NSBMDGlRenderer mapRenderer = new NSBMDGlRenderer();
        public static NSBMDGlRenderer buildingsRenderer = new NSBMDGlRenderer();
        public static NSBMDGlRenderer npcRenderer = new NSBMDGlRenderer();

        public bool hideBuildings = new bool();
        public bool mapTexturesOn = true;
        public bool bldTexturesOn = true;
        public float camX;
        public float camY;
        public static float ang = 0;
        public static float dist = 12.8f;
        public static float elev = 50.0f;
        public float perspective = 45f;

        public NSBMD npc;
        public OverworldObjectsEntry objects;
        public Point matrixPos;

        public MapEditor()
        {
            InitializeComponent();
            mapOpenGlControl.InitializeContexts();
            mapOpenGlControl.MouseWheel += new MouseEventHandler((sender, e) =>
            {
                dist -= (float)e.Delta / 100;
                RenderMap();
            });

            LoadMap(279, 210, 210);
        }

        public void LoadMap(int mapFile, int texture, int areaDataID, OverworldObjectsEntry ow = null, Point matrixPos = default)
        {
            loadedMaps.Clear();
            loadedMapOffsets.Clear();
            loadedMaps.Add(MainEditor.mapFilesNarc.files[mapFile]);
            loadedMapOffsets.Add(new Point(0, 0));
            objects = ow;
            this.matrixPos = matrixPos;
            currentAreaID = areaDataID;

            perspective = 45;
            elev = 45;
            dist = 16;

            currentTextures = MainEditor.mapTexturesNarc.files[texture];

            foreach (var map in loadedMaps)
            {
                map.mapModel.materials = NSBTXLoader.LoadNsbtx(new MemoryStream(currentTextures.bytes), out map.mapModel.Textures, out map.mapModel.Palettes);
                try
                {
                    map.mapModel.MatchTextures();
                }
                catch { }
                mapRenderer.Model = map.mapModel.models[0];

                int bld = HelperFunctions.ReadShort(MainEditor.fileSystem.narcs[13].byteData, 10 * areaDataID);
                bool inside = MainEditor.fileSystem.narcs[13].byteData[10 * areaDataID + 6] == 0;
                foreach (Building b in map.buildings)
                {
                    b.LoadModelDataFromID(inside ? MainEditor.interiorBuildingsNarc.files[bld] : MainEditor.exteriorBuildingsNarc.files[bld]);
                    if (b.NSBMDFile == null) continue;
                    b.NSBMDFile.materials = NSBTXLoader.LoadNsbtx(new MemoryStream(inside ? MainEditor.interiorTexturesNarc.files[bld].bytes : MainEditor.exteriorTexturesNarc.files[bld].bytes), out b.NSBMDFile.Textures, out b.NSBMDFile.Palettes);
                    try
                    {
                        b.NSBMDFile.MatchTextures();
                    }
                    catch { }
                }

                npc = NSBMDLoader.LoadNSBMD(new MemoryStream(MainEditor.npcSpriteNarc.files[3].bytes));
            }
            RenderMap();
        }

        public void ReloadMap()
        {
            perspective = 45;
            elev = 45;
            dist = 16;

            foreach (var map in loadedMaps)
            {
                map.mapModel.materials = NSBTXLoader.LoadNsbtx(new MemoryStream(currentTextures.bytes), out map.mapModel.Textures, out map.mapModel.Palettes);
                try
                {
                    map.mapModel.MatchTextures();
                }
                catch { }
                mapRenderer.Model = map.mapModel.models[0];

                int bld = HelperFunctions.ReadShort(MainEditor.fileSystem.narcs[13].byteData, 10 * currentAreaID);
                bool inside = MainEditor.fileSystem.narcs[13].byteData[10 * currentAreaID + 6] == 0;
                foreach (Building b in map.buildings)
                {
                    b.LoadModelDataFromID(inside ? MainEditor.interiorBuildingsNarc.files[bld] : MainEditor.exteriorBuildingsNarc.files[bld]);
                    if (b.NSBMDFile == null) continue;
                    b.NSBMDFile.materials = NSBTXLoader.LoadNsbtx(new MemoryStream(inside ? MainEditor.interiorTexturesNarc.files[bld].bytes : MainEditor.exteriorTexturesNarc.files[bld].bytes), out b.NSBMDFile.Textures, out b.NSBMDFile.Palettes);
                    try
                    {
                        b.NSBMDFile.MatchTextures();
                    }
                    catch { }
                }

                npc = NSBMDLoader.LoadNSBMD(new MemoryStream(MainEditor.npcSpriteNarc.files[3].bytes));
            }
            RenderMap();
        }

        protected override void OnInvalidated(InvalidateEventArgs e)
        {
            RenderMap();
        }

        private void RenderMap()
        {
            DateTime t1 = DateTime.Now;
            #region Useless variables that the rendering API still needs
            MKDS_Course_Editor.NSBTA.NSBTA.NSBTA_File ani = new MKDS_Course_Editor.NSBTA.NSBTA.NSBTA_File();
            MKDS_Course_Editor.NSBTP.NSBTP.NSBTP_File tp = new MKDS_Course_Editor.NSBTP.NSBTP.NSBTP_File();
            MKDS_Course_Editor.NSBCA.NSBCA.NSBCA_File ca = new MKDS_Course_Editor.NSBCA.NSBCA.NSBCA_File();
            int[] aniframeS = new int[0];
            #endregion

            /* Invalidate drawing surfaces */
            mapOpenGlControl.Invalidate();

            /* Adjust rendering settings */
            SetupRenderer();
            if (!mapTexturesOn)
            {
                Gl.glDisable(Gl.GL_TEXTURE_2D);
            }
            else
            {
                Gl.glEnable(Gl.GL_TEXTURE_2D);
            }

            foreach (var map in loadedMaps)
            {
                mapRenderer.Model = map.mapModel.models[0];
                Point p = loadedMapOffsets[loadedMaps.IndexOf(map)];
                Gl.glTranslatef(p.X, 0, p.Y);
                Gl.glScalef(map.mapModel.models[0].modelScale / 64, map.mapModel.models[0].modelScale / 64, map.mapModel.models[0].modelScale / 64);
                mapRenderer.RenderModel("", ani, aniframeS, aniframeS, aniframeS, aniframeS, aniframeS, ca, false, -1, 0, 0, dist, elev, ang, true, tp, map.mapModel); // Render map model

                if (!hideBuildings)
                {
                    if (bldTexturesOn)
                    {
                        Gl.glEnable(Gl.GL_TEXTURE_2D);
                    }
                    else
                    {
                        Gl.glDisable(Gl.GL_TEXTURE_2D);
                    }

                    for (int i = 0; i < map.buildings.Count; i++)
                    {
                        NSBMD file = map.buildings[i].NSBMDFile;
                        if (file is null)
                        {
                            Console.WriteLine("Null building can't be rendered");
                        }
                        else
                        {
                            buildingsRenderer.Model = file.models[0];
                            Gl.glTranslatef(p.X, 0, p.Y);
                            ScaleTranslateRotateBuilding(map.buildings[i]);
                            buildingsRenderer.RenderModel("", ani, aniframeS, aniframeS, aniframeS, aniframeS, aniframeS, ca, false, -1, 0.0f, 0.0f, dist, elev, ang, true, tp, file);
                        }
                    }
                }

                if (objects != null)
                {
                    foreach (OverworldNPC o in objects.NPCs)
                    {
                        int spriteid = HelperFunctions.ReadShort(npcSpriteIndex, o.sprite * 0x1c + 0x14);
                        npc.materials = NSBTXLoader.LoadNsbtx(new MemoryStream(MainEditor.npcSpriteNarc.files[spriteid].bytes), out npc.Textures, out npc.Palettes);
                        bool flipX = false;
                        if (npc.materials != null && npc.materials.Count() > 1)
                        {
                            foreach (var t in npc.Textures)
                            {
                                int i = NPCSpriteEntry.TextureIDFromDirection(npc.materials.ToList(), o.defaultDirection, out flipX);
                                t.texdata = npc.materials.ToList()[i].texdata;
                            }
                        }
                        try
                        {
                            npc.MatchTextures();
                        }
                        catch { }
                        npcRenderer.Model = npc.models[0];

                        int x = o.xPosition - matrixPos.X;
                        int y = o.yPosition - matrixPos.Y;
                        int z = o.zPosition;

                        Gl.glScalef(1 / 16f, 1 / 16f, 1 / 16f);
                        Gl.glTranslatef((x - 15.5f) * 4, (z + 0.5f) * 4, (y - 15.5f) * 4);
                        Gl.glRotatef(-30, 1, 0, 0);
                        Gl.glScalef(flipX ? -1 : 1, 1, 1);
                        npcRenderer.RenderModel("", ani, aniframeS, aniframeS, aniframeS, aniframeS, aniframeS, ca, false, -1, 0, 0, dist, elev, ang, true, tp, npc); // Render model
                    }
                }
            }
        }
        private void ScaleTranslateRotateBuilding(Building building)
        {
            float fullXcoord = building.xPosition;
            float fullYcoord = building.yPosition;
            float fullZcoord = -building.zPosition;

            float scaleFactor = building.NSBMDFile.models[0].modelScale / 64f;
            float translateFactor = 1f / building.NSBMDFile.models[0].modelScale;

            Gl.glScalef(scaleFactor, scaleFactor, scaleFactor);
            Gl.glTranslatef(fullXcoord * translateFactor, fullYcoord * translateFactor, fullZcoord * translateFactor);
            Gl.glRotatef(Building.U16ToDeg((ushort)building.rotation), 0, 1, 0);
        }
        private void SetupRenderer()
        {
            //TODO: improve this
            Gl.glEnable(Gl.GL_RESCALE_NORMAL);
            Gl.glEnable(Gl.GL_COLOR_MATERIAL);
            Gl.glEnable(Gl.GL_DEPTH_TEST);
            Gl.glEnable(Gl.GL_NORMALIZE);
            Gl.glDisable(Gl.GL_CULL_FACE);
            Gl.glFrontFace(Gl.GL_CCW);
            Gl.glClearDepth(1);
            Gl.glEnable(Gl.GL_ALPHA_TEST);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glAlphaFunc(Gl.GL_GREATER, 0f);
            Gl.glClearColor(51f / 255f, 51f / 255f, 51f / 255f, 1f);
            float aspect;
            Gl.glViewport(0, 0, mapOpenGlControl.Width, mapOpenGlControl.Height);
            aspect = mapOpenGlControl.Width / mapOpenGlControl.Height;//(vp[2] - vp[0]) / (vp[3] - vp[1]);
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glLoadIdentity();
            Glu.gluPerspective(perspective, aspect, 0.2f, 500.0f);//0.02f, 32.0f);
            Gl.glTranslatef(camX / 100, camY / 100, -dist);
            Gl.glRotatef(elev, 1, 0, 0);
            Gl.glRotatef(ang, 0, 1, 0);
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glLoadIdentity();
            Gl.glTranslatef(camX / 100, camY / 100, -dist);
            Gl.glRotatef(elev, 1, 0, 0);
            Gl.glRotatef(-ang, 0, 1, 0);
            Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_POSITION, new float[] { 1, 1, 1, 0 });
            Gl.glLightfv(Gl.GL_LIGHT1, Gl.GL_POSITION, new float[] { 1, 1, 1, 0 });
            Gl.glLightfv(Gl.GL_LIGHT2, Gl.GL_POSITION, new float[] { 1, 1, 1, 0 });
            Gl.glLightfv(Gl.GL_LIGHT3, Gl.GL_POSITION, new float[] { 1, 1, 1, 0 });
            Gl.glLoadIdentity();
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
            Gl.glColor3f(1.0f, 1.0f, 1.0f);
            Gl.glDepthMask(Gl.GL_TRUE);
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
        }

        private void replaceMapModelButton_Click(object sender, EventArgs e)
        {
            if (loadedMaps.Count == 0)
            {
                MessageBox.Show("No map file loaded");
                return;
            }

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "NSBMD file|*.nsbmd";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                byte[] b = File.ReadAllBytes(dialog.FileName);
                loadedMaps[0].bdmo = b;
                loadedMaps[0].ApplyData();
                loadedMaps[0].ReadData();
                ReloadMap();
            }
        }

        private void replaceTextureButton_Click(object sender, EventArgs e)
        {
            if (currentAreaID == -1)
            {
                MessageBox.Show("No map file loaded");
                return;
            }
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "NSBTX file|*.nsbtx";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                int txtID = HelperFunctions.ReadShort(MainEditor.fileSystem.narcs[13].byteData, 10 * currentAreaID + 2);
                byte[] b = File.ReadAllBytes(dialog.FileName);
                for (int i = 0; i < 4; i++)
                {
                    MainEditor.mapTexturesNarc.files[txtID + i].bytes = b;
                }
                ReloadMap();
            }
        }

        private void replacePermissions2Button_Click(object sender, EventArgs e)
        {
            if (loadedMaps.Count == 0)
            {
                MessageBox.Show("No map file loaded");
                return;
            }

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Permission file|*.per";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                byte[] b = File.ReadAllBytes(dialog.FileName);
                loadedMaps[0].collisions2 = b;
                loadedMaps[0].ApplyData();
                loadedMaps[0].ReadData();
                ReloadMap();
            }
        }

        private void replacePermissionsButton_Click(object sender, EventArgs e)
        {
            if (loadedMaps.Count == 0)
            {
                MessageBox.Show("No map file loaded");
                return;
            }

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Permission file|*.per";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                byte[] b = File.ReadAllBytes(dialog.FileName);
                loadedMaps[0].collisions = b;
                loadedMaps[0].ApplyData();
                loadedMaps[0].ReadData();
                ReloadMap();
            }
        }

        private void replaceBuildingsButton_Click(object sender, EventArgs e)
        {
            if (loadedMaps.Count == 0)
            {
                MessageBox.Show("No map file loaded");
                return;
            }

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Permission file|*.per";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                byte[] b = File.ReadAllBytes(dialog.FileName);
                loadedMaps[0].buildingData = b;
                loadedMaps[0].ApplyData();
                loadedMaps[0].ReadData();
                ReloadMap();
            }
        }
    }
}