namespace HLView
{
    partial class Viewer
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
            this.FileTree = new System.Windows.Forms.TreeView();
            this.MenuStrip = new System.Windows.Forms.MenuStrip();
            this.FileMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.OpenMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.EnvironmentMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.SelectEnvironmentMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // FileTree
            // 
            this.FileTree.Dock = System.Windows.Forms.DockStyle.Left;
            this.FileTree.Location = new System.Drawing.Point(0, 24);
            this.FileTree.Name = "FileTree";
            this.FileTree.Size = new System.Drawing.Size(268, 478);
            this.FileTree.TabIndex = 0;
            this.FileTree.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.OpenSelectedNode);
            // 
            // MenuStrip
            // 
            this.MenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileMenu,
            this.EnvironmentMenu});
            this.MenuStrip.Location = new System.Drawing.Point(0, 0);
            this.MenuStrip.Name = "MenuStrip";
            this.MenuStrip.Size = new System.Drawing.Size(1002, 24);
            this.MenuStrip.TabIndex = 1;
            this.MenuStrip.Text = "menuStrip1";
            // 
            // FileMenu
            // 
            this.FileMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OpenMenuItem});
            this.FileMenu.Name = "FileMenu";
            this.FileMenu.Size = new System.Drawing.Size(37, 20);
            this.FileMenu.Text = "File";
            // 
            // OpenMenuItem
            // 
            this.OpenMenuItem.Name = "OpenMenuItem";
            this.OpenMenuItem.Size = new System.Drawing.Size(112, 22);
            this.OpenMenuItem.Text = "Open...";
            this.OpenMenuItem.Click += new System.EventHandler(this.OpenMenuItem_Click);
            // 
            // EnvironmentMenu
            // 
            this.EnvironmentMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SelectEnvironmentMenuItem});
            this.EnvironmentMenu.Name = "EnvironmentMenu";
            this.EnvironmentMenu.Size = new System.Drawing.Size(87, 20);
            this.EnvironmentMenu.Text = "Environment";
            // 
            // SelectEnvironmentMenuItem
            // 
            this.SelectEnvironmentMenuItem.Name = "SelectEnvironmentMenuItem";
            this.SelectEnvironmentMenuItem.Size = new System.Drawing.Size(114, 22);
            this.SelectEnvironmentMenuItem.Text = "Select...";
            // 
            // Viewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1002, 502);
            this.Controls.Add(this.FileTree);
            this.Controls.Add(this.MenuStrip);
            this.MainMenuStrip = this.MenuStrip;
            this.Name = "Viewer";
            this.Text = "HLView";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Viewer_FormClosing);
            this.Load += new System.EventHandler(this.Viewer_Load);
            this.MenuStrip.ResumeLayout(false);
            this.MenuStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TreeView FileTree;
        private System.Windows.Forms.MenuStrip MenuStrip;
        private System.Windows.Forms.ToolStripMenuItem FileMenu;
        private System.Windows.Forms.ToolStripMenuItem OpenMenuItem;
        private System.Windows.Forms.ToolStripMenuItem EnvironmentMenu;
        private System.Windows.Forms.ToolStripMenuItem SelectEnvironmentMenuItem;
    }
}

