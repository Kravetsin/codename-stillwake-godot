using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using static Stillwake.Game.I18n.En;
using Look = Stillwake.Game.WristLook;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Energy;

/// <summary>
/// The charge row of the vitals (hud-vitals): the bolt, and a piece of light for each store on the belt, as long as it holds
/// full (a battery <see cref="Vitals.BatteryWidth"/>), lit as far as it holds now (<see cref="EnergyRing"/>); no number, a
/// store's charge on hover. The stores stand in reverse slot order, so the one a tool drains is at the right end. The bolt
/// beats while a tool draws and throws discharges (<see cref="BoltPulse"/>). Amber and pulsing while the belt runs low, dim
/// with none worn; a press of a tool it cannot pay blinks the bolt and the pieces red (<see cref="FlatBlink"/>). Read four
/// times a second.
/// </summary>
public partial class EnergyBar : CanvasLayer
{
    private const double RefreshS = 0.25;

    private readonly Func<EnergyRead?> _read;
    private readonly Ui _ui;
    private readonly Row _row = new();
    private readonly TipSpots _tips;
    private double _sinceRead = RefreshS;

    /// <param name="read">The belt as <see cref="EnergyRead.Of"/> gives it; null with no world to read, which hides it.</param>
    /// <param name="ui">The slots' size, which the block's size follows (0058), and the one tooltip.</param>
    public EnergyBar(Func<EnergyRead?> read, Ui ui)
    {
        (_read, _ui) = (read, ui);
        _tips = new TipSpots(ui.Tip);
        ui.PressedFlat += _row.Blink.Press;
        Layer = ScreenLayers.Hud;
        AddChild(_row);
        _row.Visible = false;
        for (int i = 0; i < _row.Spots.Length; i++)
        {
            int k = i;
            _tips.Add(_row.Spots[k], () => _row.TipOf(k));
        }
    }

    /// <summary>What it shows now, null while hidden: for scripted runs.</summary>
    public Belt? Shown { get; private set; }

    public override void _Process(double delta)
    {
        var (x, y) = Vitals.Origin(GetViewport().GetVisibleRect().Size.Y, _ui.Slot);
        _row.Place(new Vector2(x, y), Vitals.ScaleOf(_ui.Slot));
        _tips.Frame(_row.Visible && !_ui.MenuOpen.Get() ? GetViewport().GetMousePosition() : null);
        _sinceRead += delta;
        if (_sinceRead < RefreshS) return;
        _sinceRead = 0;
        var read = _read();
        Shown = read?.Belt;
        _row.Visible = read != null;
        if (read is not { Belt: var belt }) return;
        _row.Read(read);
        _row.Pulse.Read(belt.Units, read.Draw, belt.Low);
        _row.Low = belt.Low;
        _row.Bright = belt.Worn ? 1 : Look.NoBattery;
    }

    /// <summary>The row: the bolt at the block's sign column, the stores' pieces from <see cref="Vitals.RowLeft"/>.</summary>
    private sealed partial class Row : Control
    {
        private static readonly Texture2D Bolt = Signs.Bolt(false), Dark = Signs.Bolt(true);
        // The bolt's place in its own pixels, as the old tab drew it: the beat's discharges are laid out round it there.
        private static readonly Rect2 BoltAt = new(9, 6, 14, 18);
        private const float BoltHeight = 30;
        private static readonly Vector2 Centre = BoltAt.GetCenter(), BoltCentre = new(Vitals.SignX, Vitals.ChargeY);
        private const float Top = Vitals.ChargeY - Vitals.PieceHeight / 2, Height = Vitals.PieceHeight;
        /// <summary>The red a press of a flat belt blinks the bolt, and the hot heart of its light.</summary>
        private static readonly Color Flat = new(1, 70 / 255f, 60 / 255f);
        private static readonly Color FlatCore = new(1, 190 / 255f, 160 / 255f);
        private static readonly Color Ember = new(1, 140 / 255f, 45 / 255f), Hot = new(1, 190 / 255f, 90 / 255f), Current = new(1, 248 / 255f, 225 / 255f);
        private static readonly GradientTexture2D Soft = new()
        {
            Gradient = new Gradient { Colors = [Colors.White, Colors.White with { A = 0 }], Offsets = [0, 1] },
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f),
            FillTo = new Vector2(1, 0.5f),
            Width = 32,
            Height = 32,
        };

        private readonly Light _light = new();
        private readonly BoltLayer _bolt = new();
        private double _clock;
        private IReadOnlyList<(int Charge, int Capacity)?> _stores = [];
        private IReadOnlyList<string?> _items = [];
        /// <summary>Each store's piece by its slot order: its left edge and width in block units, none for a slot with no store.</summary>
        private (float X, float W)?[] _pieces = [];

        public Row()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            _bolt.Row = this;
            AddChild(_bolt);
            _light.Row = this;
            AddChild(_light);
            Spots = new Control[Charges.BeltSlots];
            for (int i = 0; i < Spots.Length; i++) AddChild(Spots[i] = new Control { MouseFilter = MouseFilterEnum.Ignore });
        }

        public EnergyRing Ring { get; } = new();
        public BoltPulse Pulse { get; } = new((BoltAt.Position.X, BoltAt.Position.Y, BoltAt.Size.X, BoltAt.Size.Y), BoltHeight, new Random());
        public FlatBlink Blink { get; } = new();
        public bool Low { get; set; }
        public float Bright { get; set; } = 1;

        /// <summary>A hover spot over each store's piece, by slot order; empty for a slot with none.</summary>
        public Control[] Spots { get; }

        public void Place(Vector2 origin, float scale)
        {
            Position = origin;
            Scale = new Vector2(scale, scale);
        }

        /// <summary>A read: the stores and what they are, laid out a piece each, in reverse slot order, fitted to the row.</summary>
        public void Read(EnergyRead read)
        {
            Ring.Read(read.Stores, read.Draw);
            (_stores, _items) = (read.Stores, read.Items);
            var order = Vitals.ChargeOrder(_stores.Count);
            var worn = new List<int>();
            foreach (int k in order)
                if (_stores[k] is not null) worn.Add(k);
            var row = Vitals.Fit(worn.ConvertAll(k => (double)_stores[k]!.Value.Capacity / Spec.Charge.Battery), Vitals.BatteryWidth);
            _pieces = new (float, float)?[_stores.Count];
            for (int i = 0; i < worn.Count; i++) _pieces[worn[i]] = row[i];
            for (int k = 0; k < Spots.Length; k++)
                (Spots[k].Position, Spots[k].Size) = k < _pieces.Length && _pieces[k] is { } p
                    ? (new Vector2(p.X, Top), new Vector2(Math.Max(8, p.W), Height)) : (Vector2.Zero, Vector2.Zero);
        }

        /// <summary>Store <paramref name="k"/>'s tip: what it is, what it holds of its full, in power units.</summary>
        public Tip? TipOf(int k) =>
            k < _stores.Count && _stores[k] is { } s && _items[k] is { } item
                ? new Tip(T("hud.store", ("name", T($"item.{item}")), ("charge", Charges.Grouped(s.Charge / Spec.Charge.Scale)), ("capacity", Charges.Grouped(s.Capacity / Spec.Charge.Scale))), [])
                : null;

        public override void _Process(double delta)
        {
            _clock += delta;
            Ring.Step(delta);
            Pulse.Step(delta);
            Blink.Step(delta);
            QueueRedraw();
            _bolt.QueueRedraw();
            _light.QueueRedraw();
        }

        private string Tone => Low ? "amber" : "green";

        private float PulseOf => Low ? Vitals.Pulse(_clock) : 1;

        private void Span(CanvasItem on, string tone, (float X, float W) p, int k, float from, float to, float alpha) =>
            LightStrip.Span(on, LightStrip.Of(tone, k), p.X, Top, p.W, Height, from, to, Colors.White with { A = alpha });

        public override void _Draw()
        {
            for (int k = 0; k < _pieces.Length; k++)
            {
                if (_pieces[k] is not { } p) continue;
                Span(this, "darkGreen", p, k, 0, p.W, 0.9f * Bright);
                float share = Ring.Share(k);
                if (share > 0) Span(this, Tone, p, k, 0, p.W * share, (0.3f + 0.65f * Ring.Lit(k)) * PulseOf * Bright);
            }
        }

        /// <summary>The bolt in its own pixels, scaled to the block's sign and centred on the charge row, thrown by the beat and a flat press.</summary>
        private void BoltTransform(CanvasItem on) =>
            on.DrawSetTransform(BoltCentre - Centre * Vitals.SignScale + new Vector2(Pulse.Jolt.X + Blink.Jolt.X, Pulse.Jolt.Y + Blink.Jolt.Y) * Vitals.SignScale, 0, new Vector2(Vitals.SignScale, Vitals.SignScale));

        private void DrawBolt(CanvasItem layer)
        {
            var (on, bright) = Pulse.Look();
            var white = Colors.White with { A = Bright };
            BoltTransform(layer);
            if (Pulse.Kindle is double k && k < 0.45)
            {
                // Kindled from below: the dark bolt, lit up from its tip.
                layer.DrawTextureRect(Dark, BoltAt, false, white);
                float lit = (float)(1 - Math.Pow(1 - k / 0.45, 3)) * BoltAt.Size.Y;
                BoltPart(layer, Bolt, BoltAt.End.Y - lit, BoltAt.End.Y, white);
            }
            else if (!on) layer.DrawTextureRect(Dark, BoltAt, false, white);
            else
            {
                layer.DrawTextureRect(Bolt, BoltAt, false, white);
                if (bright < 1) layer.DrawTextureRect(Bolt, BoltAt, false, Colors.Black with { A = (float)(1 - bright) * 0.8f * Bright });
            }
            if (Blink.Red > 0) layer.DrawTextureRect(Dark, BoltAt, false, Colors.White with { A = (float)Blink.Red });
            layer.DrawSetTransform(Vector2.Zero);
        }

        /// <summary>What lights the row, added over it: the glow under the lit pieces, the drained store's glint, the hop, the bolt's light, discharges.</summary>
        private void DrawLight(Control on)
        {
            DrawPiecesLight(on);
            var (lit, _) = Pulse.Look();
            if (Blink.Red > 0)
            {
                float red = (float)Blink.Red;
                for (int k = 0; k < _pieces.Length; k++)
                    if (_pieces[k] is { } p) Span(on, "red", p, k, 0, p.W, 0.45f * red);
                // The red bolt throws its light round it, a hot core at its heart.
                BoltTransform(on);
                Glow(on, Centre, 24, Flat with { A = 0.5f * red });
                Glow(on, Centre, 12, Flat with { A = 0.75f * red });
                on.DrawTextureRect(Dark, BoltAt, false, Colors.White with { A = 0.8f * red });
                Glow(on, Centre, 5, FlatCore with { A = 0.55f * red });
                on.DrawSetTransform(Vector2.Zero);
            }
            double kindle = Pulse.Kindle is double kd ? 0.35 * (1 - kd / BoltPulse.KindleS) : 0;
            float halo = lit ? (float)Math.Clamp(0.05 + 0.16 * Pulse.Beat * Pulse.Level + kindle + Pulse.Flare, 0, 0.8) : 0;
            BoltTransform(on);
            if (halo > 0) Glow(on, Centre, 12, Hot with { A = halo * Bright });
            if (lit && Pulse.Level > 0.02 && Pulse.Kindle is null)
                for (int band = 0; band < 2; band++)
                {
                    float y = BoltAt.Position.Y - 3 + (float)((Pulse.Phase + band * 10.5) % 21);
                    float a = (float)(0.55 * Pulse.Level) * Bright;
                    BoltPart(on, Bolt, y - 1, y + 1, Current with { A = a });
                    BoltPart(on, Bolt, y - 2, y - 1, Current with { A = a * 0.4f });
                    BoltPart(on, Bolt, y + 1, y + 2, Current with { A = a * 0.4f });
                }
            foreach (var arc in Pulse.Arcs)
            {
                float a = (float)arc.Life * arc.Strength * Bright;
                foreach (var line in arc.Fork is { } fork ? new[] { arc.Points, fork } : [arc.Points])
                {
                    var points = Array.ConvertAll(line, p => new Vector2(p.X, p.Y));
                    on.DrawPolyline(points, Ember with { A = 0.22f * a }, arc.Harsh ? 2.6f : 2, true);
                    on.DrawPolyline(points, Current with { A = 0.85f * a }, arc.Harsh ? 1.1f : 0.8f, true);
                }
                Glow(on, new Vector2(arc.End.X, arc.End.Y), arc.Harsh ? 3 : 2, Hot with { A = 0.6f * a });
            }
            on.DrawSetTransform(Vector2.Zero);
        }

        /// <summary>The glow under each lit piece; on the drained one a bright end and a glint running back; a store put on filling in; the hop.</summary>
        private void DrawPiecesLight(Control on)
        {
            for (int k = 0; k < _pieces.Length; k++)
            {
                if (_pieces[k] is not { } p) continue;
                float share = Ring.Share(k), lit = Ring.Lit(k), end = p.W * share;
                if (share <= 0) continue;
                LightStrip.Glow(on, LightStrip.Of(Tone, k), p.X, Top, p.W, Height, 0, end, lit * PulseOf * Bright);
                if (lit > 0.85f)
                {
                    Span(on, "paleGreen", p, k, end - 2, end, 0.6f * Bright);
                    float at = end - (float)Ring.Glint * end;
                    Span(on, "paleGreen", p, k, at - 3, Math.Min(end, at + 3), 0.75f * (lit - 0.85f) / 0.15f * Bright);
                }
                if (Ring.FillingIn(k)) Span(on, "paleGreen", p, k, end - 6, end, (float)(0.8 * (1 - Ring.FillIn(k)) + 0.3) * Bright);
            }
            if (Ring.Hop is { Gone: < 1 } hop && FillEnd(hop.From) is { } from && FillEnd(hop.To) is { } to)
            {
                float x = from + (to - from) * (float)(1 - Math.Pow(1 - hop.Gone, 3));
                var at = new Vector2(x, Vitals.ChargeY);
                Glow(on, at, 5, new Color(Charges.Green).Lerp(Colors.White, 0.6f) with { A = 0.7f * Bright });
                on.DrawRect(new Rect2(at - new Vector2(0.75f, 0.75f), new Vector2(1.5f, 1.5f)), Colors.White with { A = 0.9f * Bright });
            }
        }

        /// <summary>Where store <paramref name="k"/>'s light ends, in block units; null for a slot with no store.</summary>
        private float? FillEnd(int k) => k < _pieces.Length && _pieces[k] is { } p ? p.X + p.W * Ring.Share(k) : null;

        private static void Glow(CanvasItem on, Vector2 at, float radius, Color colour) =>
            on.DrawTextureRect(Soft, new Rect2(at - new Vector2(radius, radius), new Vector2(2 * radius, 2 * radius)), false, colour);

        /// <summary>The rows of the bolt's picture between <paramref name="top"/> and <paramref name="bottom"/>, in the bolt's pixels.</summary>
        private static void BoltPart(CanvasItem on, Texture2D picture, float top, float bottom, Color colour)
        {
            top = Math.Max(top, BoltAt.Position.Y);
            bottom = Math.Min(bottom, BoltAt.End.Y);
            if (bottom <= top) return;
            var size = picture.GetSize();
            float from = (top - BoltAt.Position.Y) / BoltAt.Size.Y, to = (bottom - BoltAt.Position.Y) / BoltAt.Size.Y;
            on.DrawTextureRectRegion(picture, new Rect2(BoltAt.Position.X, top, BoltAt.Size.X, bottom - top),
                new Rect2(0, from * size.Y, size.X, (to - from) * size.Y), colour);
        }

        /// <summary>The bolt, over the pieces: drawn every frame as it beats.</summary>
        private sealed partial class BoltLayer : Node2D
        {
            public Row? Row { get; set; }

            public override void _Draw() => Row?.DrawBolt(this);
        }

        /// <summary>The row's light, drawn added over it.</summary>
        private sealed partial class Light : Control
        {
            public Row? Row { get; set; }

            public Light()
            {
                MouseFilter = MouseFilterEnum.Ignore;
                Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
            }

            public override void _Draw() => Row?.DrawLight(this);
        }
    }
}
