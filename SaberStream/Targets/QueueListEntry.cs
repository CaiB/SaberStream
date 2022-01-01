using SaberStream.Data;
using SaberStream.Sources;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SaberStream.Targets
{
    public partial class QueueListEntry : UserControl
    {
        private readonly MapInfo Map;

        public QueueListEntry(MapInfo map)
        {
            InitializeComponent();
            this.Map = map;

            this.labelSongName.Text = map.SongName;
            this.labelMapper.Text = map.MapAuthor;
            this.labelKey.Text = $"🔑{map.Key}";
            
            if (map is MapInfoRequest mapReq)
            {
                this.labelApproval.Text = $"{(mapReq.ApprovalRating * 100):F0}%";
                this.labelRequestor.Text = mapReq.Requestor;
                this.labelVotes.Text = mapReq.TotalVotes.ToString();
            }
            else
            {
                this.labelApproval.Text = "??%";
                this.labelRequestor.Text = "Nobody?";
                this.labelVotes.Text = "?????";
            }

            this.iconEasy.Visible = map.Easy != null;
            this.labelEasySpeed.Visible = map.Easy != null;
            if (map.Easy != null) { this.labelEasySpeed.Text = (map.Easy.NoteCount / map.Length.TotalSeconds).ToString("F2"); }

            this.iconNormal.Visible = map.Normal != null;
            this.labelNormalSpeed.Visible = map.Normal != null;
            if (map.Normal != null) { this.labelNormalSpeed.Text = (map.Normal.NoteCount / map.Length.TotalSeconds).ToString("F2"); }

            this.iconHard.Visible = map.Hard != null;
            this.labelHardSpeed.Visible = map.Hard != null;
            if (map.Hard != null) { this.labelHardSpeed.Text = (map.Hard.NoteCount / map.Length.TotalSeconds).ToString("F2"); }

            this.iconExpert.Visible = map.Expert != null;
            this.labelExpertSpeed.Visible = map.Expert != null;
            if (map.Expert != null) { this.labelExpertSpeed.Text = (map.Expert.NoteCount / map.Length.TotalSeconds).ToString("F2"); }

            this.iconPlus.Visible = map.ExpertPlus != null;
            this.labelPlusSpeed.Visible = map.ExpertPlus != null;
            if (map.ExpertPlus != null) { this.labelPlusSpeed.Text = (map.ExpertPlus.NoteCount / map.Length.TotalSeconds).ToString("F2"); }
        }

        private void buttonYes_Click(object sender, EventArgs e)
        {
            if (this.Map.Key != null)
            {
                CommonEvents.InvokeDownloadRequest(this, new(this.Map.Key));
                RequestQueue.RemoveItem(this.Map);
            }
        }

        private void buttonNo_Click(object sender, EventArgs e) => RequestQueue.RemoveItem(this.Map);
    }
}
