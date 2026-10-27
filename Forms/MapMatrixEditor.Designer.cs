namespace NewEditor.Forms
{
    partial class MapMatrixEditor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.mapMatrixGrid = new System.Windows.Forms.DataGridView();
            this.matrixHeadersCheckbox = new System.Windows.Forms.CheckBox();
            this.matrixIDNumberBox = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.mapMatrixGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.matrixIDNumberBox)).BeginInit();
            this.SuspendLayout();
            // 
            // mapMatrixGrid
            // 
            this.mapMatrixGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.mapMatrixGrid.Location = new System.Drawing.Point(12, 49);
            this.mapMatrixGrid.Name = "mapMatrixGrid";
            this.mapMatrixGrid.Size = new System.Drawing.Size(1000, 660);
            this.mapMatrixGrid.TabIndex = 5;
            this.mapMatrixGrid.CellContentDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.mapMatrixGrid_CellContentDoubleClick);
            // 
            // matrixHeadersCheckbox
            // 
            this.matrixHeadersCheckbox.AutoSize = true;
            this.matrixHeadersCheckbox.Location = new System.Drawing.Point(938, 23);
            this.matrixHeadersCheckbox.Name = "matrixHeadersCheckbox";
            this.matrixHeadersCheckbox.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.matrixHeadersCheckbox.Size = new System.Drawing.Size(74, 20);
            this.matrixHeadersCheckbox.TabIndex = 6;
            this.matrixHeadersCheckbox.Text = "Headers";
            this.matrixHeadersCheckbox.UseVisualStyleBackColor = true;
            this.matrixHeadersCheckbox.CheckedChanged += new System.EventHandler(this.ToggleHeaders);
            // 
            // matrixIDNumberBox
            // 
            this.matrixIDNumberBox.Location = new System.Drawing.Point(66, 21);
            this.matrixIDNumberBox.Name = "matrixIDNumberBox";
            this.matrixIDNumberBox.Size = new System.Drawing.Size(60, 22);
            this.matrixIDNumberBox.TabIndex = 7;
            this.matrixIDNumberBox.ValueChanged += new System.EventHandler(this.ChangeMatrixID);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(47, 16);
            this.label1.TabIndex = 8;
            this.label1.Text = "Matrix:";
            // 
            // MapMatrixEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1024, 721);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.matrixIDNumberBox);
            this.Controls.Add(this.matrixHeadersCheckbox);
            this.Controls.Add(this.mapMatrixGrid);
            this.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MapMatrixEditor";
            this.Text = "Map Editor";
            ((System.ComponentModel.ISupportInitialize)(this.mapMatrixGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.matrixIDNumberBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.DataGridView mapMatrixGrid;
        private System.Windows.Forms.CheckBox matrixHeadersCheckbox;
        private System.Windows.Forms.NumericUpDown matrixIDNumberBox;
        private System.Windows.Forms.Label label1;
    }
}