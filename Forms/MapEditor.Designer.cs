namespace NewEditor.Forms
{
    partial class MapEditor
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
            this.mapOpenGlControl = new Tao.Platform.Windows.SimpleOpenGlControl();
            this.panel1 = new System.Windows.Forms.Panel();
            this.replaceMapModelButton = new System.Windows.Forms.Button();
            this.replaceTextureButton = new System.Windows.Forms.Button();
            this.replacePermissions2Button = new System.Windows.Forms.Button();
            this.replaceBuildingsButton = new System.Windows.Forms.Button();
            this.replacePermissionsButton = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // mapOpenGlControl
            // 
            this.mapOpenGlControl.AccumBits = ((byte)(0));
            this.mapOpenGlControl.AutoCheckErrors = false;
            this.mapOpenGlControl.AutoFinish = false;
            this.mapOpenGlControl.AutoMakeCurrent = true;
            this.mapOpenGlControl.AutoSwapBuffers = true;
            this.mapOpenGlControl.BackColor = System.Drawing.Color.Black;
            this.mapOpenGlControl.ColorBits = ((byte)(32));
            this.mapOpenGlControl.DepthBits = ((byte)(64));
            this.mapOpenGlControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapOpenGlControl.ForeColor = System.Drawing.Color.Black;
            this.mapOpenGlControl.Location = new System.Drawing.Point(0, 0);
            this.mapOpenGlControl.Name = "mapOpenGlControl";
            this.mapOpenGlControl.Size = new System.Drawing.Size(640, 640);
            this.mapOpenGlControl.StencilBits = ((byte)(0));
            this.mapOpenGlControl.TabIndex = 3;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.mapOpenGlControl);
            this.panel1.Location = new System.Drawing.Point(12, 69);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(640, 640);
            this.panel1.TabIndex = 4;
            // 
            // replaceMapModelButton
            // 
            this.replaceMapModelButton.Location = new System.Drawing.Point(658, 669);
            this.replaceMapModelButton.Name = "replaceMapModelButton";
            this.replaceMapModelButton.Size = new System.Drawing.Size(100, 40);
            this.replaceMapModelButton.TabIndex = 5;
            this.replaceMapModelButton.Text = "Replace Model";
            this.replaceMapModelButton.UseVisualStyleBackColor = true;
            this.replaceMapModelButton.Click += new System.EventHandler(this.replaceMapModelButton_Click);
            // 
            // replaceTextureButton
            // 
            this.replaceTextureButton.Location = new System.Drawing.Point(658, 623);
            this.replaceTextureButton.Name = "replaceTextureButton";
            this.replaceTextureButton.Size = new System.Drawing.Size(100, 40);
            this.replaceTextureButton.TabIndex = 6;
            this.replaceTextureButton.Text = "Replace Texture";
            this.replaceTextureButton.UseVisualStyleBackColor = true;
            this.replaceTextureButton.Click += new System.EventHandler(this.replaceTextureButton_Click);
            // 
            // replacePermissions2Button
            // 
            this.replacePermissions2Button.Location = new System.Drawing.Point(658, 577);
            this.replacePermissions2Button.Name = "replacePermissions2Button";
            this.replacePermissions2Button.Size = new System.Drawing.Size(100, 40);
            this.replacePermissions2Button.TabIndex = 7;
            this.replacePermissions2Button.Text = "Replace Permissions 2";
            this.replacePermissions2Button.UseVisualStyleBackColor = true;
            this.replacePermissions2Button.Click += new System.EventHandler(this.replacePermissions2Button_Click);
            // 
            // replaceBuildingsButton
            // 
            this.replaceBuildingsButton.Location = new System.Drawing.Point(658, 485);
            this.replaceBuildingsButton.Name = "replaceBuildingsButton";
            this.replaceBuildingsButton.Size = new System.Drawing.Size(100, 40);
            this.replaceBuildingsButton.TabIndex = 8;
            this.replaceBuildingsButton.Text = "Replace Buildings";
            this.replaceBuildingsButton.UseVisualStyleBackColor = true;
            this.replaceBuildingsButton.Click += new System.EventHandler(this.replaceBuildingsButton_Click);
            // 
            // replacePermissionsButton
            // 
            this.replacePermissionsButton.Location = new System.Drawing.Point(658, 531);
            this.replacePermissionsButton.Name = "replacePermissionsButton";
            this.replacePermissionsButton.Size = new System.Drawing.Size(100, 40);
            this.replacePermissionsButton.TabIndex = 9;
            this.replacePermissionsButton.Text = "Replace Permissions";
            this.replacePermissionsButton.UseVisualStyleBackColor = true;
            this.replacePermissionsButton.Click += new System.EventHandler(this.replacePermissionsButton_Click);
            // 
            // MapEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1064, 721);
            this.Controls.Add(this.replacePermissionsButton);
            this.Controls.Add(this.replaceBuildingsButton);
            this.Controls.Add(this.replacePermissions2Button);
            this.Controls.Add(this.replaceTextureButton);
            this.Controls.Add(this.replaceMapModelButton);
            this.Controls.Add(this.panel1);
            this.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MapEditor";
            this.Text = "Map Editor";
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Tao.Platform.Windows.SimpleOpenGlControl mapOpenGlControl;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button replaceMapModelButton;
        private System.Windows.Forms.Button replaceTextureButton;
        private System.Windows.Forms.Button replacePermissions2Button;
        private System.Windows.Forms.Button replaceBuildingsButton;
        private System.Windows.Forms.Button replacePermissionsButton;
    }
}