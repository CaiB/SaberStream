using Newtonsoft.Json.Linq;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using SaberStream.Data;
using SaberStream.Graphics;
using SaberStream.Helpers;
using SaberStream.Sources;
using System;
using System.ComponentModel;
using System.Threading;

namespace SaberStream.Targets
{
    public class Overlay : GameWindow
    {
        private float DifficultyCalcResolution;
        
        private FontRenderer? TextRender;
        private TextureRenderer? ImageRender;
        private BarRenderer? BarRender;

        private Texture? IconEasy, IconNormal, IconHard, IconExpert, IconExpertPlus;
        private Texture? TagEasy, TagNormal, TagHard, TagExpert, TagExpertPlus;
        private Texture? IconExclamation;
        private Texture? CoverArt;
        private Texture? DifficultyMap;

        private bool IsPlayingSong = false;
        private MapInfoPlaying? CurrentMap;
        private DateTime ShowResultsUntil;
        private bool CoverArtChanged = false; // true when new cover art is ready to be uploaded
        private static byte[]? DifficultyTextureData = null; // null unless new texture data is ready to be uploaded

        public Overlay(JToken config) : base(GetGameSettings(), GetNativeSettings())
        {
            this.DifficultyCalcResolution = config.Value<float?>("DifficultyResolution") ?? 2F;
        }

        public static void NewDifficultyTexture(byte[] rawTexture) => DifficultyTextureData = rawTexture;

        private static GameWindowSettings GetGameSettings()
        {
            GameWindowSettings Settings = GameWindowSettings.Default;
            Settings.RenderFrequency = 10;
            return Settings;
        }

        private static NativeWindowSettings GetNativeSettings()
        {
            NativeWindowSettings Settings = NativeWindowSettings.Default;
            Settings.Size = new(1920, 100);
            Settings.Title = "Macyler's Beat Saber Overlay";
            return Settings;
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            this.VSync = VSyncMode.On;
            GL.ClearColor(0x81 / 256F, 0x14 / 256F, 0x26 / 256F, 1F);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);
            //GL.DepthFunc(DepthFunction.Lequal);
            GL.DepthMask(false);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            this.TextRender = new FontRenderer("PTS55F.ttf", 48);
            this.ImageRender = new();
            this.BarRender = new();

            this.IconEasy = new("EasyIcon.png", true);
            this.IconNormal = new("NormalIcon.png", true);
            this.IconHard = new("HardIcon.png", true);
            this.IconExpert = new("ExpertIcon.png", true);
            this.IconExpertPlus = new("ExpertPlusIcon.png", true);
            this.TagEasy = new("EasyTag.png", true);
            this.TagNormal = new("NormalTag.png", true);
            this.TagHard = new("HardTag.png", true);
            this.TagExpert = new("ExpertTag.png", true);
            this.TagExpertPlus = new("ExpertPlusTag.png", true);
            this.IconExclamation = new("Exclamation.png", true);
            this.CoverArt = new(null, true);
            this.DifficultyMap = new(null, false);

            UpdateProjections();
            
            GameStatus.StateTransition += HandleStateTransition;
            GameStatus.SongStarted += HandleSongStart;
            GameStatus.SongEnded += HandleSongEnd;
            CommonEvents.Exit += HandleExit;
            Console.WriteLine("Overlay loaded");
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            if (this.TextRender == null) { return; }

            this.TextRender.SetColour(1F, 1F, 1F);
            if (this.IsPlayingSong || this.ShowResultsUntil >= DateTime.UtcNow) // Show results for some time after we stop playing.
            {
                RenderSongInfo();
                RenderBars();
                RenderPlaycount();
                RenderBest();
            }
            RenderBasicInfo();

            SwapBuffers();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            CommonEvents.InvokeExit(this, new EventArgs());
        }

        private void RenderBasicInfo()
        {
            //this.TextRender!.RenderText("twitch.tv/macyler", 1655, 35, 0.7F);
        }

        private void RenderSongInfo()
        {
            if (this.TextRender == null || this.ImageRender == null || this.CurrentMap == null) { return; }

            // Cover art
            if (this.CoverArtChanged)
            {
                if (this.CurrentMap.CoverArt == null) { this.CoverArt!.NewDataRGBA(new byte[] { 0, 0, 0, 0 }, 1, 1); }
                else { this.CoverArt!.NewDataPNG(this.CurrentMap.CoverArt); }
                this.CoverArtChanged = false;
            }
            this.ImageRender.Render(this.CoverArt!, 0F, 100F, 100F, 0F);

            const float LEFT_OFFSET = 115; // How much to move away from the left edge for the cover art

            // First line: Song Name, SubName, Difficulty
            float NameWidth = this.TextRender.RenderText(this.CurrentMap.SongName ?? "", LEFT_OFFSET, 50, 1F);
            float SecondaryWidth = this.TextRender.RenderText(this.CurrentMap.SongSubName ?? "", (LEFT_OFFSET + NameWidth + 20), 50, 0.4F);
            Texture? DiffIcon = (this.CurrentMap.DifficultyPlaying == null) ? null : IconToUse(this.CurrentMap.DifficultyPlaying.Difficulty);
            if (DiffIcon != null) { this.ImageRender.Render(DiffIcon, (LEFT_OFFSET + NameWidth + SecondaryWidth + 30F), 55F, 42F, 0F); }
            // TODO: If the title & subtitle are too long, the text or difficulty tag will intersect with the bars. Consider cutting short the text if it is very long.
            
            // Second line: Author, Mapper, Key
            string Subtext = $"{this.CurrentMap.SongAuthor} / Mapper: {this.CurrentMap.MapAuthor}" + (this.CurrentMap.Key != null ? $" / Key: {this.CurrentMap.Key}" : "");
            this.TextRender.RenderText(Subtext, LEFT_OFFSET, 80, 0.4F);
        }

        private void RenderBars()
        {
            if (this.TextRender == null || this.ImageRender == null || this.BarRender == null || this.CurrentMap == null) { return; }

            const float X_OFFSET = 1400F;
            const float BAR_WIDTH = 500F;

            // Text
            this.TextRender.RenderText("NPS", X_OFFSET - 40, 28, 0.35F);
            this.TextRender.RenderText("Combos", X_OFFSET - 68, 48, 0.35F);

            // Difficulty bar
            if (DifficultyTextureData != null)
            {
                this.DifficultyMap!.NewDataRGBA(DifficultyTextureData, DifficultyTextureData.Length / 4, 1);
                DifficultyTextureData = null;
            }
            this.ImageRender.Render(this.DifficultyMap!, X_OFFSET, 30, BAR_WIDTH, 15, 0F);

            // Combo bar
            lock (GameStatus.MapHistory) // TODO: Only do this update if there is new data to upload
            {
                this.BarRender.Prepare(BAR_WIDTH, 10, GameStatus.MapHistory.Count + 1);
                foreach (HistoryEntry Entry in GameStatus.MapHistory)
                {
                    float Right = (float)(Entry.EndTime / CurrentMap.Length);
                    if (Entry.Type == HistoryType.Hit)
                    {
                        float Left = this.BarRender.GetCurrentWidth();
                        this.BarRender.AddSegment(Right, false, 0F, 0.8F, 0F);
                        if (Entry.NoteCount >= (CurrentMap.DifficultyPlaying?.NoteCount / 20 ?? 50)) // TODO: This only depends on the current difficulty, and will change rendering for parts of the song played on another difficulty.
                        {
                            float TextWidth = this.TextRender.TextWidth(Entry.NoteCount.ToString(), 0.4F);
                            float XInset = (((Left + Right) * BAR_WIDTH) - TextWidth) / 2F;
                            this.TextRender.RenderText(Entry.NoteCount.ToString(), X_OFFSET + XInset, 68, 0.4F);
                        }
                    }
                    else if (Entry.Type == HistoryType.Mistake)
                    {
                        this.BarRender.AddSegment(Right, false, 0.8F, 0F, 0F);
                        this.ImageRender.Render(this.IconExclamation!, X_OFFSET + (Right * BAR_WIDTH) - 8, 50, 16, 0.1F);
                    }
                    else if (Entry.Type == HistoryType.LevelFail)
                    {
                        this.BarRender.AddSegment(Right, false, 0F, 0F, 0.5F);
                    }
                    else if (Entry.Type == HistoryType.Join)
                    {
                        Texture? DiffIcon = IconToUse(Entry.NewDifficulty);
                        if (DiffIcon != null) { this.ImageRender.Render(DiffIcon, (this.BarRender.GetCurrentWidth() * BAR_WIDTH) + X_OFFSET - 12, 95, 24, 0F); }
                    }
                }
            }
            this.BarRender.Render(X_OFFSET, 45);
        }

        private void RenderPlaycount()
        {
            if (this.TextRender == null || this.ImageRender == null || this.CurrentMap == null) { return; }

            const float X_OFFSET = 1200F; // The location of this content on the bar
            const float X_SPACING = 65F; // How far the two columns are spaced
            const float X_TEXTOFFSET = 30F; // How far from the left edge of the icon the text should be rendered
            const float ROW_TOP = 45F; // Y location of top row
            const float ROW_MID = 70F; // Y location of middle row
            const float ROW_BOT = 95F; // Y location of bottom row
            const float NUM_FONT_SCALE = 0.4F; // The scale of the font used for playcount numbers
            const float TEXT_SHIFT = -5F; // How much to shift up number text

            this.TextRender.RenderText("Play Count", X_OFFSET + 10F, 16F, 0.4F);
            // Top row
            if (this.CurrentMap.StatsEasy != null && this.IconEasy != null)
            {
                this.ImageRender.Render(this.IconEasy, X_OFFSET, ROW_TOP, 24, 0F);
                this.TextRender.RenderText(this.CurrentMap.StatsEasy.PlayCount.ToString(), X_OFFSET + X_TEXTOFFSET, ROW_TOP + TEXT_SHIFT, NUM_FONT_SCALE);
            }
            if (this.CurrentMap.StatsNormal != null && this.IconNormal != null)
            {
                this.ImageRender.Render(this.IconNormal, X_OFFSET + X_SPACING, ROW_TOP, 24, 0F);
                this.TextRender.RenderText(this.CurrentMap.StatsNormal.PlayCount.ToString(), X_OFFSET + X_SPACING + X_TEXTOFFSET, ROW_TOP + TEXT_SHIFT, NUM_FONT_SCALE);
            }
            // Middle row
            if (this.CurrentMap.StatsHard != null && this.IconHard != null)
            {
                this.ImageRender.Render(this.IconHard, X_OFFSET, ROW_MID, 24, 0F);
                this.TextRender.RenderText(this.CurrentMap.StatsHard.PlayCount.ToString(), X_OFFSET + X_TEXTOFFSET, ROW_MID + TEXT_SHIFT, NUM_FONT_SCALE);
            }
            if (this.CurrentMap.StatsExpert != null && this.IconExpert != null)
            {
                this.ImageRender.Render(this.IconExpert, X_OFFSET + X_SPACING, ROW_MID, 24, 0F);
                this.TextRender.RenderText(this.CurrentMap.StatsExpert.PlayCount.ToString(), X_OFFSET + X_SPACING + X_TEXTOFFSET, ROW_MID + TEXT_SHIFT, NUM_FONT_SCALE);
            }
            // Bottom row
            if (this.CurrentMap.StatsExpertPlus != null && this.IconExpertPlus != null)
            {
                this.ImageRender.Render(this.IconExpertPlus, X_OFFSET, ROW_BOT, 24, 0F);
                this.TextRender.RenderText(this.CurrentMap.StatsExpertPlus.PlayCount.ToString(), X_OFFSET + X_TEXTOFFSET, ROW_BOT + TEXT_SHIFT, NUM_FONT_SCALE);
            }
        }

        private void RenderBest()
        {
            if (this.TextRender == null || this.ImageRender == null || this.CurrentMap == null) { return; }

            const float X_CENTER = 1110F; // The location of this content on the bar

            this.TextRender.RenderTextCentered("My Best:", X_CENTER, 16F, 0.4F);
            PlayStats? Stats = this.CurrentMap.GetStats(this.CurrentMap.DifficultyPlaying?.Difficulty ?? Difficulty.None);
            if (Stats == null || !Stats.ScoreIsValid)
            {
                this.TextRender.RenderTextCentered("None yet!", X_CENTER, 50F, 0.5F);
            }
            else
            {
                this.TextRender.RenderTextCentered($"{Stats.HighScore} ({Stats.MaxRank})", X_CENTER, 40F, 0.5F);
                this.TextRender.RenderTextCentered("Max Combo:", X_CENTER, 70F, 0.4F);
                this.TextRender.RenderTextCentered(Stats.FullCombo ? "FULL COMBO" : Stats.MaxCombo.ToString(), X_CENTER, 95F, 0.5F);
            }
        }

        private Texture? IconToUse(Difficulty diff)
        {
            if (diff.HasFlag(Difficulty.Easy)) { return this.IconEasy; }
            if (diff.HasFlag(Difficulty.Normal)) { return this.IconNormal; }
            if (diff.HasFlag(Difficulty.Hard)) { return this.IconHard; }
            if (diff.HasFlag(Difficulty.Expert)) { return this.IconExpert; }
            if (diff.HasFlag(Difficulty.ExpertPlus)) { return this.IconExpertPlus; }
            return null;
        }

        private void UpdateProjections()
        {
            Matrix4 Projection = Matrix4.CreateOrthographicOffCenter(0F, this.ClientRectangle.Size.X, this.ClientRectangle.Size.Y, 0F, -1F, 1F);
            this.TextRender?.UpdateProjection(ref Projection);
            this.ImageRender?.UpdateProjection(ref Projection);
            this.BarRender?.UpdateProjection(ref Projection);
        }

        /// <summary>Called when the player switches between playing and using a menu.</summary>
        private void HandleStateTransition(object? sender, GameStatus.StateTransitionEventArgs evt)
        {
            this.IsPlayingSong = evt.IsPlayingSong;
            if (!this.IsPlayingSong) { this.ShowResultsUntil = DateTime.UtcNow.AddSeconds(30); }
        }

        /// <summary>Called when the player starts playing a song.</summary>
        private void HandleSongStart(object? sender, GameStatus.SongStartedEventArgs evt)
        {
            this.CurrentMap = evt.Beatmap;
            this.CoverArtChanged = true;
            if (evt.Beatmap.MapFolder != null)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object? state)
                {
                    NPSCalc.CalculateDifficultyMap(evt.Beatmap.MapFolder, evt.Beatmap.DifficultyPlaying?.Difficulty ?? Difficulty.None, "Standard", (float)evt.Beatmap.Length.TotalSeconds, DifficultyCalcResolution); // TODO: hardcoded standard
                    if (this.CurrentMap.Hash != null) { this.CurrentMap.Key = BeatSaver.GetKeyFromHash(this.CurrentMap.Hash); }
                }), null);
            }
            else { NewDifficultyTexture(new byte[] { 0, 0, 0, 0 }); }
        }

        /// <summary>Called when the player stops playing a song, via exiting or failure.</summary>
        private void HandleSongEnd(object? sender, GameStatus.SongEndedEventArgs evt) { }

        /// <summary>Called when the application is closing.</summary>
        private void HandleExit(object? sender, EventArgs evt)
        {
            if (sender != this) { Close(); }
        }
    }
}
