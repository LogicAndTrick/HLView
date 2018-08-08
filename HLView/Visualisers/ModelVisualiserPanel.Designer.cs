namespace HLView.Visualisers
{
    partial class ModelVisualiserPanel
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ModelTabs = new System.Windows.Forms.TabControl();
            this.BodyPartsTab = new System.Windows.Forms.TabPage();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.ModelSelector = new System.Windows.Forms.ComboBox();
            this.PartSelector = new System.Windows.Forms.ComboBox();
            this.SequencesTab = new System.Windows.Forms.TabPage();
            this.label3 = new System.Windows.Forms.Label();
            this.SequenceSelector = new System.Windows.Forms.ComboBox();
            this.ModelTabs.SuspendLayout();
            this.BodyPartsTab.SuspendLayout();
            this.SequencesTab.SuspendLayout();
            this.SuspendLayout();
            // 
            // ModelTabs
            // 
            this.ModelTabs.Controls.Add(this.BodyPartsTab);
            this.ModelTabs.Controls.Add(this.SequencesTab);
            this.ModelTabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ModelTabs.Location = new System.Drawing.Point(0, 0);
            this.ModelTabs.Name = "ModelTabs";
            this.ModelTabs.SelectedIndex = 0;
            this.ModelTabs.Size = new System.Drawing.Size(785, 91);
            this.ModelTabs.TabIndex = 0;
            // 
            // BodyPartsTab
            // 
            this.BodyPartsTab.Controls.Add(this.label2);
            this.BodyPartsTab.Controls.Add(this.label1);
            this.BodyPartsTab.Controls.Add(this.ModelSelector);
            this.BodyPartsTab.Controls.Add(this.PartSelector);
            this.BodyPartsTab.Location = new System.Drawing.Point(4, 22);
            this.BodyPartsTab.Name = "BodyPartsTab";
            this.BodyPartsTab.Padding = new System.Windows.Forms.Padding(3);
            this.BodyPartsTab.Size = new System.Drawing.Size(777, 65);
            this.BodyPartsTab.TabIndex = 0;
            this.BodyPartsTab.Text = "Body Parts";
            this.BodyPartsTab.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 36);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(57, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Sub-model";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(38, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(26, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Part";
            // 
            // ModelSelector
            // 
            this.ModelSelector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ModelSelector.FormattingEnabled = true;
            this.ModelSelector.Location = new System.Drawing.Point(70, 33);
            this.ModelSelector.Name = "ModelSelector";
            this.ModelSelector.Size = new System.Drawing.Size(178, 21);
            this.ModelSelector.TabIndex = 0;
            this.ModelSelector.SelectedIndexChanged += new System.EventHandler(this.ModelChanged);
            // 
            // PartSelector
            // 
            this.PartSelector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.PartSelector.FormattingEnabled = true;
            this.PartSelector.Location = new System.Drawing.Point(70, 6);
            this.PartSelector.Name = "PartSelector";
            this.PartSelector.Size = new System.Drawing.Size(178, 21);
            this.PartSelector.TabIndex = 0;
            this.PartSelector.SelectedIndexChanged += new System.EventHandler(this.PartChanged);
            // 
            // SequencesTab
            // 
            this.SequencesTab.Controls.Add(this.label3);
            this.SequencesTab.Controls.Add(this.SequenceSelector);
            this.SequencesTab.Location = new System.Drawing.Point(4, 22);
            this.SequencesTab.Name = "SequencesTab";
            this.SequencesTab.Padding = new System.Windows.Forms.Padding(3);
            this.SequencesTab.Size = new System.Drawing.Size(777, 65);
            this.SequencesTab.TabIndex = 1;
            this.SequencesTab.Text = "Sequences";
            this.SequencesTab.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 9);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(56, 13);
            this.label3.TabIndex = 3;
            this.label3.Text = "Sequence";
            // 
            // SequenceSelector
            // 
            this.SequenceSelector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.SequenceSelector.FormattingEnabled = true;
            this.SequenceSelector.Location = new System.Drawing.Point(70, 6);
            this.SequenceSelector.Name = "SequenceSelector";
            this.SequenceSelector.Size = new System.Drawing.Size(178, 21);
            this.SequenceSelector.TabIndex = 2;
            this.SequenceSelector.SelectedIndexChanged += new System.EventHandler(this.SequenceChanged);
            // 
            // ModelVisualiserPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ModelTabs);
            this.Name = "ModelVisualiserPanel";
            this.Size = new System.Drawing.Size(785, 91);
            this.ModelTabs.ResumeLayout(false);
            this.BodyPartsTab.ResumeLayout(false);
            this.BodyPartsTab.PerformLayout();
            this.SequencesTab.ResumeLayout(false);
            this.SequencesTab.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl ModelTabs;
        private System.Windows.Forms.TabPage BodyPartsTab;
        private System.Windows.Forms.TabPage SequencesTab;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox ModelSelector;
        private System.Windows.Forms.ComboBox PartSelector;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox SequenceSelector;
    }
}
