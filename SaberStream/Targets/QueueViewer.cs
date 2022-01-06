using SaberStream.Data;
using SaberStream.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SaberStream.Targets
{
    public partial class QueueViewer : Form
    {
        public void UpdateQueueItems(object? sender, QueueChangeEventArgs evt)
        {
            Invoke(() => UpdateQueueItems(evt));
        }

        private readonly List<Control> QueueItems = new();

        private void UpdateQueueItems(QueueChangeEventArgs evt)
        {
            if (evt.Added)
            {
                int Row = this.tableLayoutPanelQueue.RowCount++;
                this.tableLayoutPanelQueue.RowStyles.Add(new(SizeType.Absolute, 50));
                QueueListEntry Entry = new(evt.Map)
                {
                    Dock = DockStyle.Fill
                };
                this.tableLayoutPanelQueue.Controls.Add(Entry, 0, Row);
                this.QueueItems.Add(Entry);
            }
            else
            {
                int Index = evt.Index + 1;
                Control Item = this.tableLayoutPanelQueue.GetControlFromPosition(0, Index);
                if (Item is QueueListEntry)
                {
                    this.tableLayoutPanelQueue.Controls.Remove(Item);
                    this.tableLayoutPanelQueue.RowStyles.RemoveAt(Index);
                    for (int i = Index + 1; i < this.tableLayoutPanelQueue.RowCount; i++)
                    {
                        Control LowerControl = this.tableLayoutPanelQueue.GetControlFromPosition(0, i);
                        this.tableLayoutPanelQueue.SetRow(LowerControl, i - 1);
                    }
                    this.tableLayoutPanelQueue.RowCount--;
                }
            }
        }

        public QueueViewer()
        {
            InitializeComponent();
            RequestQueue.QueueChanged += UpdateQueueItems;
        }

        private void buttonAddToQueue_Click(object sender, EventArgs e)
        {
            if (CheckKeyInput() != Validity.VALID) { return; }
            string Key = this.textBoxKeyToAdd.Text;
            MapInfoBeatSaver? Map = BeatSaver.GetMapInfo(Key);
            if (Map != null)
            {
                MapInfoRequest MapRequest = new(Map, Key) { Requestor = "(Manually Added)" };
                RequestQueue.AddItem(MapRequest);
            }
            this.textBoxKeyToAdd.Text = "";
        }

        private Validity CheckKeyInput()
        {
            string Key = this.textBoxKeyToAdd.Text;
            if (string.IsNullOrWhiteSpace(Key)) { return Validity.EMPTY; }
            return Regex.IsMatch(Key, "^[a-fA-F\\d]{1,6}$") ? Validity.VALID : Validity.INVALID;
        }

        private enum Validity { EMPTY, VALID, INVALID }

        private void textBoxKeyToAdd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                buttonAddToQueue_Click(sender, new());
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }
    }
}
