using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Look = Stillwake.Game.WristLook;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// The energy gauge right of the suit's (0053, 0081): what the batteries on the belt hold, always shown, the bolt for its
/// label and four cells a battery the belt can wear, a gap between batteries; amber and pulsing while they run low, dim
/// with none worn. Beside it on the column's rail, its readout: the same charge in power units, never seconds. Read four
/// times a second, never every frame; both move smoothly between reads (<see cref="GaugeMotion"/>).
/// </summary>
public partial class EnergyBar : CanvasLayer
{
    private const double RefreshS = 0.25;

    private readonly Func<Belt?> _read;
    private readonly WristGauge _gauge = new(
        AsciiArt.Bolt(false), new Rect2(11, 6, 14, 18), Look.EnergyMeterLeft, Charges.BeltSlots * Look.CellsPerBattery, Look.CellsPerBattery,
        AsciiArt.Colour(Look.Energy), AsciiArt.Colour(Look.EnergyLow), motes: false);
    private readonly Readout _readout;
    private double _sinceRead = RefreshS;

    /// <param name="read">The belt as <see cref="Charges.BeltOf"/> gives it; null with no world to read, which hides it.</param>
    /// <param name="ui">The slots' size, which the hotbar's size and so the gauge's place and width follow (0058).</param>
    public EnergyBar(Func<Belt?> read, Ui ui)
    {
        _read = read;
        _readout = new Readout(_gauge);
        void Place()
        {
            var row = Slots.GaugeRowOf(ui.Slot);
            float top = Slots.GaugeTop(ui.Slot);
            _gauge.Place(new Vector2(row.EnergyX, top), row.EnergyWidth);
            _readout.Place(new Vector2(row.ReadoutX, top));
        }
        Place();
        ui.SlotScale.Subscribe(Place);
        Layer = ScreenLayers.Hud;
        // The readout, drawn after, covers the gauge tab's right edge: one seam between two screens.
        AddChild(_gauge);
        AddChild(_readout);
        _gauge.Visible = _readout.Visible = false;
    }

    /// <summary>What it shows now, null while hidden: for scripted runs.</summary>
    public Belt? Shown { get; private set; }

    /// <summary>Where the gauge and its readout lie on the screen: what the markers keep out of.</summary>
    public Rect2 Covers => _gauge.GetGlobalRect().Merge(_readout.GetGlobalRect());

    public override void _Process(double delta)
    {
        _sinceRead += delta;
        if (_sinceRead < RefreshS) return;
        _sinceRead = 0;
        Shown = _read();
        _gauge.Visible = _readout.Visible = Shown != null;
        if (Shown is not { } belt) return;
        _gauge.Motion.Read(belt.Share);
        _gauge.Low = _readout.Low = belt.Low;
        _gauge.Bright = _readout.Bright = belt.Worn ? 1 : Look.NoBattery;
    }

    /// <summary>
    /// The readout's tab: the belt's charge in power units, grouped by thin spaces, green, amber and pulsing while low. It
    /// counts the gauge's lost part, so after a hit it holds, flashes white, then runs down with it.
    /// </summary>
    private sealed partial class Readout : Control
    {
        private const int FontSize = 13, ThinWidth = 3, Right = 8, Baseline = 20;
        private static readonly int Full = Charges.BeltSlots * (Content.Charge.Battery / Content.Charge.Scale);

        private readonly WristGauge _gauge;
        private readonly NinePatchRect _tab = WristArt.Frame(WristPieces.Gauge, MouseFilterEnum.Ignore);
        private double _clock;

        public Readout(WristGauge gauge)
        {
            _gauge = gauge;
            MouseFilter = MouseFilterEnum.Ignore;
            AddChild(_tab);
        }

        public bool Low { get; set; }
        public float Bright { get; set; } = 1;

        public void Place(Vector2 at)
        {
            Position = at;
            Size = new Vector2(Look.ReadoutWidth, Look.TabHeight);
            WristArt.Fit(_tab, WristPieces.Gauge, Size);
        }

        public override void _Process(double delta)
        {
            _clock += delta;
            QueueRedraw();
        }

        public override void _Draw()
        {
            WristArt.DrawScreen(this, Size, WristArt.Screen, glare: false);
            var motion = _gauge.Motion;
            float flash = (float)motion.Flash;
            var colour = AsciiArt.Colour(Low ? Look.EnergyLow : Look.Energy).Lerp(Colors.White, flash);
            colour.A = (Low ? WristGauge.Pulse(_clock) : 1) * Bright;
            var groups = Charges.Grouped((int)Math.Round(motion.Trail * Full)).Split(Charges.Thin);
            // Right-aligned, a group at a time from the right, a thin space between.
            float x = Size.X - Right;
            for (int i = groups.Length - 1; i >= 0; i--)
            {
                var font = Style.Mono;
                float width = font.GetStringSize(groups[i], HorizontalAlignment.Left, -1, FontSize).X;
                x -= width;
                var at = new Vector2(x, Baseline);
                if (flash > 0) DrawStringOutline(font, at, groups[i], HorizontalAlignment.Left, -1, FontSize, 3, Colors.White with { A = 0.45f * flash });
                DrawString(font, at, groups[i], HorizontalAlignment.Left, -1, FontSize, colour);
                x -= ThinWidth;
            }
        }
    }
}
