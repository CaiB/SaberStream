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
            this.tableLayoutPanelQueue = new System.Windows.Forms.TableLayoutPanel();
            this.buttonAddToQueue = new System.Windows.Forms.Button();
            this.textBoxKeyToAdd = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
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
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanelQueue, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.buttonAddToQueue, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.textBoxKeyToAdd, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.button1, 3, 2);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 2);
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
            // tableLayoutPanelQueue
            // 
            this.tableLayoutPanelQueue.AutoScroll = true;
            this.tableLayoutPanelQueue.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.tableLayoutPanelQueue.ColumnCount = 1;
            this.tableLayoutPanel1.SetColumnSpan(this.tableLayoutPanelQueue, 4);
            this.tableLayoutPanelQueue.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelQueue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelQueue.Location = new System.Drawing.Point(3, 38);
            this.tableLayoutPanelQueue.Name = "tableLayoutPanelQueue";
            this.tableLayoutPanelQueue.RowCount = 1;
            this.tableLayoutPanelQueue.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelQueue.Size = new System.Drawing.Size(794, 374);
            this.tableLayoutPanelQueue.TabIndex = 0;
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
            // button1
            // 
            this.button1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button1.Location = new System.Drawing.Point(603, 418);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(194, 29);
            this.button1.TabIndex = 3;
            this.button1.Text = "Delete Last Played";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.label1, 3);
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(3, 415);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(594, 35);
            this.label1.TabIndex = 4;
            this.label1.Text = "No Song Played Yet";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
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
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelQueue;
        private System.Windows.Forms.Button buttonAddToQueue;
        private System.Windows.Forms.TextBox textBoxKeyToAdd;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label1;
    }
}