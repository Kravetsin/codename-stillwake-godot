using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Look = Stillwake.Game.WristLook;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// The energy tab at the gauge row's right end (0053, 0081): what the batteries on the belt hold, always shown, the bolt
/// and the charge in power units ("4 300"), never seconds; amber and pulsing while they run low, dim with none worn. Read
/// four times a second, never every frame; the number moves smoothly between reads (<see cref="GaugeMotion"/>): after a
/// hit it holds, flashes white, then counts down with the lost part.
/// </summary>
public partial class EnergyBar : CanvasLayer
{
    private const double RefreshS = 0.25;

    private readonly Func<Belt?> _read;
    private readonly Tab _tab = new();
    private double _sinceRead = RefreshS;

    /// <param name="read">The belt as <see cref="Charges.BeltOf"/> gives it; null with no world to read, which hides it.</param>
    /// <param name="ui">The slots' size, which the hotbar's size and so the tab's place follow (0058).</param>
    public EnergyBar(Func<Belt?> read, Ui ui)
    {
        _read = read;
        void Place()
        {
            var row = Slots.GaugeRowOf(ui.Slot);
            _tab.Place(new Vector2(row.EnergyX, Slots.GaugeTop(ui.Slot)), row.EnergyWidth);
        }
        Place();
        ui.SlotScale.Subscribe(Place);
        Layer = ScreenLayers.Hud;
        AddChild(_tab);
        _tab.Visible = false;
    }

    /// <summary>What it shows now, null while hidden: for scripted runs.</summary>
    public Belt? Shown { get; private set; }

    /// <summary>Where the tab lies on the screen: what the markers keep out of.</summary>
    public Rect2 Covers => _tab.GetGlobalRect();

    public override void _Process(double delta)
    {
        _sinceRead += delta;
        if (_sinceRead < RefreshS) return;
        _sinceRead = 0;
        Shown = _read();
        _tab.Visible = Shown != null;
        if (Shown is not { } belt) return;
        _tab.Motion.Read(belt.Share);
        _tab.Low = belt.Low;
        _tab.Bright = belt.Worn ? 1 : Look.NoBattery;
    }

    /// <summary>
    /// The tab: its screen, the bolt and the belt's charge, grouped by thin spaces, right-aligned. It counts the motion's
    /// lost part, so after a hit it holds, flashes white, then runs down with it.
    /// </summary>
    private sealed partial class Tab : Control
    {
        private const int FontSize = 13, ThinWidth = 3;
        private static readonly int Full = Charges.BeltSlots * (Content.Charge.Battery / Content.Charge.Scale);
        private static readonly Texture2D Bolt = AsciiArt.Bolt(false);
        private static readonly Rect2 BoltAt = new(9, 6, 14, 18);

        private readonly NinePatchRect _frame = WristArt.Frame(WristPieces.Gauge, MouseFilterEnum.Ignore);
        private double _clock;

        public Tab()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            AddChild(_frame);
        }

        public GaugeMotion Motion { get; } = new();
        public bool Low { get; set; }
        public float Bright { get; set; } = 1;

        public void Place(Vector2 at, int width)
        {
            Position = at;
            Size = new Vector2(width, Look.TabHeight);
            WristArt.Fit(_frame, WristPieces.Gauge, Size);
        }

        public override void _Process(double delta)
        {
            _clock += delta;
            Motion.Step(delta);
            QueueRedraw();
        }

        public override void _Draw()
        {
            WristArt.DrawScreen(this, Size, WristArt.Screen, glare: false);
            float flash = (float)Motion.Flash;
            // A hit washes the tab light for its flash, as it does the suit's.
            if (flash > 0) DrawRect(new Rect2(Vector2.Zero, Size), Colors.White with { A = 0.1f * flash });
            float pulse = Low ? WristGauge.Pulse(_clock) : 1;
            var colour = AsciiArt.Colour(Low ? Look.EnergyLow : Look.Energy).Lerp(Colors.White, flash);
            colour.A = pulse * Bright;
            DrawTextureRect(Bolt, BoltAt, false, Colors.White with { A = Bright });
            var groups = Charges.Grouped((int)Math.Round(Motion.Trail * Full)).Split(Charges.Thin);
            // Right-aligned, a group at a time from the right, a thin space between.
            float x = Size.X - Look.EnergyRight;
            for (int i = groups.Length - 1; i >= 0; i--)
            {
                var font = Style.Mono;
                float width = font.GetStringSize(groups[i], HorizontalAlignment.Left, -1, FontSize).X;
                x -= width;
                var at = new Vector2(x, Look.EnergyBaseline);
                if (flash > 0) DrawStringOutline(font, at, groups[i], HorizontalAlignment.Left, -1, FontSize, 3, Colors.White with { A = 0.45f * flash });
                DrawString(font, at, groups[i], HorizontalAlignment.Left, -1, FontSize, colour);
                x -= ThinWidth;
            }
        }
    }
}
