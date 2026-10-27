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
using Tao.OpenGl;
using static Tao.Platform.Windows.Winmm;

namespace NewEditor.Forms
{
    public partial class MapMatrixEditor : Form
    {
        public MapMatrixEditor()
        {
            InitializeComponent();

            SetupMapMatrix();

            matrixIDNumberBox.Minimum = 0;
            matrixIDNumberBox.Maximum = MainEditor.mapMatrixNarc.matricies.Count - 1;
        }

        private void SetupMapMatrix()
        {
            mapMatrixGrid.SuspendLayout();
            mapMatrixGrid.Rows.Clear();
            mapMatrixGrid.Columns.Clear();

            MapMatrixEntry matrix = MainEditor.mapMatrixNarc.matricies[(int)matrixIDNumberBox.Value];

            /* Generate table columns */
            for (int i = 0; i < matrix.width; i++)
            {
                mapMatrixGrid.Columns.Add("Column" + i, i.ToString("D"));
                mapMatrixGrid.Columns[i].Width = 32; // Set column size
                mapMatrixGrid.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                mapMatrixGrid.Columns[i].Frozen = false;
            }

            /* Generate table rows */
            for (int i = 0; i < matrix.height; i++)
            {
                mapMatrixGrid.Rows.Add("Row" + i, i.ToString("D"));
                mapMatrixGrid.Rows[i].HeaderCell.Value = i.ToString();
            }

            /* Fill tables */
            for (int i = 0; i < matrix.height; i++)
            {
                for (int j = 0; j < matrix.width; j++)
                {
                    mapMatrixGrid.Rows[i].Cells[j].Value = matrixHeadersCheckbox.Checked ? matrix.mapHeaders[j, i] : matrix.mapFiles[j, i];
                }
            }
        }

        private void mapMatrixGrid_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            MapMatrixEntry matrix = MainEditor.mapMatrixNarc.matricies[(int)matrixIDNumberBox.Value];
            int mapFile = matrix.mapFiles[e.ColumnIndex, e.RowIndex];
            if (mapFile < 0) return;

            int mapHeader = 0;
            int texture = 0;
            int areaData = 0;
            int overworldObjects = -1;
            if (matrix.mapHeaders != null)
            {
                mapHeader = matrix.mapHeaders[e.ColumnIndex, e.RowIndex];
                areaData = MainEditor.zoneDataNarc.zones[mapHeader > 0 ? mapHeader : 0].areaDataID;
                texture = HelperFunctions.ReadShort(MainEditor.fileSystem.narcs[13].byteData, 10 * areaData + 2);
                if (mapHeader >= 0) overworldObjects = MainEditor.zoneDataNarc.zones[mapHeader].mapId;
            }
            else
            {
                mapHeader = MainEditor.zoneDataNarc.zones.FindIndex(z => z.matrix == matrixIDNumberBox.Value);
                areaData = MainEditor.zoneDataNarc.zones[mapHeader > 0 ? mapHeader : 0].areaDataID;
                texture = HelperFunctions.ReadShort(MainEditor.fileSystem.narcs[13].byteData, 10 * areaData + 2);
                if (mapHeader >= 0) overworldObjects = MainEditor.zoneDataNarc.zones[mapHeader].mapId;
            }

            MainEditor.instance.OpenMapEditor(null, null);
            MainEditor.mapEditor.LoadMap(mapFile, texture, areaData, mapHeader >= 0 ? MainEditor.overworldsNarc.objects[overworldObjects] : null, new Point(e.ColumnIndex * 32, e.RowIndex * 32));
        }

        bool loading = false;
        private void ToggleHeaders(object sender, EventArgs e)
        {
            MapMatrixEntry matrix = MainEditor.mapMatrixNarc.matricies[(int)matrixIDNumberBox.Value];
            if (matrix.mapHeaders == null) return;

            if (!loading)
                SetupMapMatrix();
        }

        private void ChangeMatrixID(object sender, EventArgs e)
        {
            loading = true;
            MapMatrixEntry matrix = MainEditor.mapMatrixNarc.matricies[(int)matrixIDNumberBox.Value];
            if (matrix.mapHeaders == null)
            {
                matrixHeadersCheckbox.Checked = false;
                matrixHeadersCheckbox.Enabled = false;
            }
            else matrixHeadersCheckbox.Enabled = true;
            loading = false;

            SetupMapMatrix();
        }
    }
}