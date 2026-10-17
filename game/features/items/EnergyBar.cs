using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// The energy bar right of the suit's (0053): what the batteries on the belt hold of full, shown only
/// while the hero holds what runs on it, in the colour of its energy. A bolt for its label, no words. Read
/// four times a second, never every frame.
/// </summary>
public partial class EnergyBar : CanvasLayer
{
    private const double RefreshS = 0.25;

    private readonly Func<Energy?> _read;
    private readonly Bar _bar = new();
    private double _sinceRead = RefreshS;

    /// <param name="read">The bar as <see cref="Charges.EnergyOf"/> gives it for what is held now; null to hide it.</param>
    /// <param name="ui">The slots' size, which the hotbar's height and so the bar's place follow (0058).</param>
    public EnergyBar(Func<Energy?> read, Ui ui)
    {
        _read = read;
        void Place() => _bar.Position = _bar.Position with { Y = Slots.GaugeTop(ui.Slot) };
        Place();
        ui.SlotScale.Subscribe(Place);
        Layer = ScreenLayers.Hud;
        AddChild(_bar);
    }

    /// <summary>What the bar shows now, null while hidden: for scripted runs.</summary>
    public Energy? Shown { get; private set; }

    public override void _Process(double delta)
    {
        _sinceRead += delta;
        if (_sinceRead < RefreshS) return;
        _sinceRead = 0;
        Shown = _read();
        _bar.Visible = Shown != null;
        if (Shown is not { } energy) return;
        (_bar.Share, _bar.Fill) = (energy.Share, new Color(energy.Colour));
        _bar.QueueRedraw();
    }

    /// <summary>The bar drawn by hand as the suit's is, beside it: the slate box, a bolt, the track and its fill.</summary>
    private sealed partial class Bar : Control
    {
        private static readonly Color Back = new(new Color("0f172a"), 0.9f), Edge = new("334155"), Track = new("334155");

        public float Share { get; set; }
        public Color Fill { get; set; }

        public Bar()
        {
            // The suit's gauge is 224 wide at the margin: this one sits 6 px right of it, under the hotbar as it is.
            Position = new Vector2(Slots.Margin + 224 + 6, 0);
            Size = new Vector2(150, 30);
            MouseFilter = MouseFilterEnum.Ignore;
            Visible = false;
        }

        public override void _Draw()
        {
            var box = new Rect2(Vector2.Zero, Size);
            DrawRect(box, Back);
            DrawRect(box, Edge, false, 1);
            var bolt = AsciiArt.Bolt(false);
            DrawTextureRect(bolt, new Rect2(11, 6, 14, 18), false);
            var track = new Rect2(32, 12, Size.X - 44, 6);
            DrawRect(track, Track);
            DrawRect(track with { Size = new Vector2(track.Size.X * Math.Clamp(Share, 0, 1), track.Size.Y) }, Fill);
        }
    }
}
