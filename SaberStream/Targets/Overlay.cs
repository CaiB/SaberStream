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
        private Texture? IconExclamation;
        private Texture? CoverArt;
        private Texture? DifficultyMap;

        private bool IsPlayingSong = false;
        private MapInfoPlaying? CurrentMap;
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
            this.IconExclamation = new("Exclamation.png", true);
            this.CoverArt = new(null, true);
            this.DifficultyMap = new(null, false);

            UpdateProjections();
            
            GameStatus.StateTransition += HandleStateTransition;
            GameStatus.SongStarted += HandleSongStart;
            CommonEvents.Exit += HandleExit;
            Console.WriteLine("Overlay loaded");
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            if (this.TextRender == null) { return; }

            this.TextRender.SetColour(1F, 1F, 1F);
            if (this.IsPlayingSong)
            {
                RenderSongInfo();
                RenderBars();
                //RenderPlaycount();
                // TODO: Implement playcount
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
            this.TextRender!.RenderText("twitch.tv/macyler", 1655, 35, 0.7F);
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
            
            // Second line: Author, Mapper, Key
            string Subtext = $"{this.CurrentMap.SongAuthor} / Mapper: {this.CurrentMap.MapAuthor}" + (this.CurrentMap.Key != null ? $" / Key: {this.CurrentMap.Key}" : "");
            this.TextRender.RenderText(Subtext, LEFT_OFFSET, 80, 0.4F);
        }

        private void RenderBars()
        {
            if (this.TextRender == null || this.ImageRender == null || this.BarRender == null || this.CurrentMap == null) { return; }

            const float X_OFFSET = 1100F;
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
            lock (GameStatus.CurrentPerformance) // TODO: Only do this update if there is new data to upload
            {
                this.BarRender.Prepare(BAR_WIDTH, 10, GameStatus.CurrentPerformance.Count + 1);
                foreach (PerformanceEntry Entry in GameStatus.CurrentPerformance)
                {
                    float Right = (float)(Entry.LastActionTime / CurrentMap.Length.TotalMilliseconds);
                    if (Entry.WasCorrect)
                    {
                        float Left = this.BarRender.GetCurrentWidth();
                        this.BarRender.AddSegment(Right, false, 0F, 0.8F, 0F);
                        if (Entry.NoteCount >= 50)
                        {
                            float TextWidth = this.TextRender.TextWidth(Entry.NoteCount.ToString(), 0.4F);
                            float XInset = (((Left + Right) * BAR_WIDTH) - TextWidth) / 2F;
                            this.TextRender.RenderText(Entry.NoteCount.ToString(), X_OFFSET + XInset, 68, 0.4F);
                        }
                    }
                    else
                    {
                        this.BarRender.AddSegment(Right, false, 0.8F, 0F, 0F);
                        this.ImageRender.Render(this.IconExclamation!, X_OFFSET + (Right * BAR_WIDTH) - 8, 50, 16, 0.1F);
                    }
                }
            }
            this.BarRender.Render(X_OFFSET, 45);
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

        private void HandleStateTransition(object? sender, GameStatus.StateTransitionEventArgs evt) => this.IsPlayingSong = evt.IsPlayingSong;
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
        }

        private void HandleExit(object? sender, EventArgs evt)
        {
            if (sender != this) { Close(); }
        }
    }
}
