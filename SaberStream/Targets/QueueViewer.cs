using SaberStream.Data;
using SaberStream.Helpers;
using SaberStream.Sources;
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

        private void UpdateQueueItems(QueueChangeEventArgs evt)
        {
            if (evt.Added)
            {
                QueueListEntry Entry = new(evt.Map)
                {
                    Tag = evt.Map,
                    BorderStyle = BorderStyle.FixedSingle
                };

                this.flowLayoutPanelQueue.Controls.Add(Entry);
                this.flowLayoutPanelQueue.Controls.SetChildIndex(Entry, evt.Index);
            }
            else
            {
                Control? ToRemove = null;
                foreach(Control Ctrl in this.flowLayoutPanelQueue.Controls)
                {
                    if ((Ctrl.Tag as MapInfo) == evt.Map) { ToRemove = Ctrl; break; }
                }

                if (ToRemove != null) { this.flowLayoutPanelQueue.Controls.Remove(ToRemove); }
                
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

        private void flowLayoutPanelQueue_Layout(object sender, LayoutEventArgs e)
        {
            foreach(Control Control in flowLayoutPanelQueue.Controls)
            {
                Control.Width = flowLayoutPanelQueue.ClientSize.Width - 6;
            }
        }

        private void QueueViewer_FormClosed(object sender, FormClosedEventArgs e) => CommonEvents.InvokeExit(sender, new());
    }
}
