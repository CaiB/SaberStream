namespace SaberStream.Targets
{
    partial class QueueViewer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(QueueViewer));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.buttonAddToQueue = new System.Windows.Forms.Button();
            this.textBoxKeyToAdd = new System.Windows.Forms.TextBox();
            this.buttonDeleteLast = new System.Windows.Forms.Button();
            this.labelLastSong = new System.Windows.Forms.Label();
            this.flowLayoutPanelQueue = new System.Windows.Forms.FlowLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.Controls.Add(this.buttonAddToQueue, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.textBoxKeyToAdd, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.buttonDeleteLast, 3, 2);
            this.tableLayoutPanel1.Controls.Add(this.labelLastSong, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanelQueue, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(800, 450);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // buttonAddToQueue
            // 
            this.buttonAddToQueue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonAddToQueue.Location = new System.Drawing.Point(603, 3);
            this.buttonAddToQueue.Name = "buttonAddToQueue";
            this.buttonAddToQueue.Size = new System.Drawing.Size(194, 29);
            this.buttonAddToQueue.TabIndex = 1;
            this.buttonAddToQueue.Text = "Add to Queue";
            this.buttonAddToQueue.UseVisualStyleBackColor = true;
            this.buttonAddToQueue.Click += new System.EventHandler(this.buttonAddToQueue_Click);
            // 
            // textBoxKeyToAdd
            // 
            this.textBoxKeyToAdd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxKeyToAdd.Font = new System.Drawing.Font("Segoe UI", 12.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.textBoxKeyToAdd.Location = new System.Drawing.Point(403, 3);
            this.textBoxKeyToAdd.Name = "textBoxKeyToAdd";
            this.textBoxKeyToAdd.Size = new System.Drawing.Size(194, 30);
            this.textBoxKeyToAdd.TabIndex = 2;
            this.textBoxKeyToAdd.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxKeyToAdd_KeyDown);
            // 
            // buttonDeleteLast
            // 
            this.buttonDeleteLast.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonDeleteLast.Enabled = false;
            this.buttonDeleteLast.Location = new System.Drawing.Point(603, 418);
            this.buttonDeleteLast.Name = "buttonDeleteLast";
            this.buttonDeleteLast.Size = new System.Drawing.Size(194, 29);
            this.buttonDeleteLast.TabIndex = 3;
            this.buttonDeleteLast.Text = "Delete Last Played";
            this.buttonDeleteLast.UseVisualStyleBackColor = true;
            this.buttonDeleteLast.Click += new System.EventHandler(this.buttonDeleteLast_Click);
            // 
            // labelLastSong
            // 
            this.labelLastSong.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.labelLastSong, 3);
            this.labelLastSong.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelLastSong.Location = new System.Drawing.Point(3, 415);
            this.labelLastSong.Name = "labelLastSong";
            this.labelLastSong.Size = new System.Drawing.Size(594, 35);
            this.labelLastSong.TabIndex = 4;
            this.labelLastSong.Text = "No Song Played Yet";
            this.labelLastSong.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // flowLayoutPanelQueue
            // 
            this.flowLayoutPanelQueue.AutoScroll = true;
            this.tableLayoutPanel1.SetColumnSpan(this.flowLayoutPanelQueue, 4);
            this.flowLayoutPanelQueue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelQueue.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelQueue.Location = new System.Drawing.Point(0, 35);
            this.flowLayoutPanelQueue.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanelQueue.Name = "flowLayoutPanelQueue";
            this.flowLayoutPanelQueue.Size = new System.Drawing.Size(800, 380);
            this.flowLayoutPanelQueue.TabIndex = 5;
            this.flowLayoutPanelQueue.WrapContents = false;
            this.flowLayoutPanelQueue.Layout += new System.Windows.Forms.LayoutEventHandler(this.flowLayoutPanelQueue_Layout);
            // 
            // QueueViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(600, 150);
            this.Name = "QueueViewer";
            this.Text = "SaberStream Queue Viewer";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.QueueViewer_FormClosed);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button buttonAddToQueue;
        private System.Windows.Forms.TextBox textBoxKeyToAdd;
        private System.Windows.Forms.Button buttonDeleteLast;
        private System.Windows.Forms.Label labelLastSong;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelQueue;
    }
}