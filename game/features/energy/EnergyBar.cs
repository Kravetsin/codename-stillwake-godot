using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Look = Stillwake.Game.WristLook;

namespace Stillwake.Game.Features.Energy;

/// <summary>
/// The energy tab at the gauge row's right end (0053, 0081, energy-ring): the belt's charge in power units ("4 300"), never
/// seconds, counting down evenly between reads (<see cref="EnergyCount"/>); round the bolt a ring of the belt's stores
/// (<see cref="EnergyRing"/>); the bolt beating while a tool draws and throwing discharges onto the frame
/// (<see cref="BoltPulse"/>). Amber and pulsing while the belt runs low, dim with none worn. Read four times a second.
/// </summary>
public partial class EnergyBar : CanvasLayer
{
    private const double RefreshS = 0.25;

    private readonly Func<EnergyRead?> _read;
    private readonly Tab _tab = new();
    private double _sinceRead = RefreshS;

    /// <param name="read">The belt as <see cref="EnergyRead.Of"/> gives it; null with no world to read, which hides it.</param>
    /// <param name="ui">The slots' size, which the hotbar's size and so the tab's place follow (0058).</param>
    public EnergyBar(Func<EnergyRead?> read, Ui ui)
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
        var read = _read();
        Shown = read?.Belt;
        _tab.Visible = read != null;
        if (read is not { Belt: var belt }) return;
        _tab.Count.Read(belt.Units);
        _tab.Ring.Read(read.Stores, read.Draw);
        _tab.Pulse.Read(belt.Units, read.Draw, belt.Low);
        _tab.Low = belt.Low;
        _tab.Bright = belt.Worn ? 1 : Look.NoBattery;
    }

    /// <summary>The tab: its screen, the ring and the bolt, the belt's charge grouped by thin spaces, right-aligned.</summary>
    private sealed partial class Tab : Control
    {
        private const int FontSize = 13, ThinWidth = 3;
        private static readonly Texture2D Bolt = Signs.Bolt(false), Dark = Signs.Bolt(true);
        private static readonly Rect2 BoltAt = new(9, 6, 14, 18);
        private static readonly Vector2 Centre = BoltAt.GetCenter();
        // What a discharge burns in and how its core and the light it strikes look.
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

        private readonly NinePatchRect _frame = WristArt.Frame(WristPieces.Gauge, MouseFilterEnum.Ignore);
        private readonly Light _light = new();
        private double _clock;

        public Tab()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            AddChild(_frame);
            _light.Tab = this;
            AddChild(_light);
        }

        public EnergyCount Count { get; } = new();
        public EnergyRing Ring { get; } = new();
        public BoltPulse Pulse { get; } = new((BoltAt.Position.X, BoltAt.Position.Y, BoltAt.Size.X, BoltAt.Size.Y), Look.TabHeight, new Random());
        public bool Low { get; set; }
        public float Bright { get; set; } = 1;

        public void Place(Vector2 at, int width)
        {
            Position = at;
            Size = new Vector2(width, Look.TabHeight);
            _light.Size = Size;
            WristArt.Fit(_frame, WristPieces.Gauge, Size);
        }

        public override void _Process(double delta)
        {
            _clock += delta;
            Count.Step(delta);
            Ring.Step(delta);
            Pulse.Step(delta);
            QueueRedraw();
            _light.QueueRedraw();
        }

        private Color Tone(float alpha = 1)
        {
            float pulse = Low ? WristGauge.Pulse(_clock) : 1;
            return AsciiArt.Colour(Low ? Look.EnergyLow : Look.Energy) with { A = alpha * pulse * Bright };
        }

        public override void _Draw()
        {
            WristArt.DrawScreen(this, Size, WristArt.Screen, glare: false);
            DrawRingArcs();
            DrawBolt();
            var groups = Charges.Grouped((int)Math.Round(Count.Value)).Split(Charges.Thin);
            // Right-aligned, a group at a time from the right, a thin space between.
            float x = Size.X - Look.EnergyRight;
            var colour = Tone();
            for (int i = groups.Length - 1; i >= 0; i--)
            {
                var font = Style.Mono;
                x -= font.GetStringSize(groups[i], HorizontalAlignment.Left, -1, FontSize).X;
                DrawString(font, new Vector2(x, Look.EnergyBaseline), groups[i], HorizontalAlignment.Left, -1, FontSize, colour);
                x -= ThinWidth;
            }
        }

        private void DrawRingArcs()
        {
            for (int k = 0; k < Ring.Count; k++)
            {
                var (a0, a1) = EnergyRing.Arc(k, Ring.Count);
                Arc(this, a0, a1, Tone(0.12f));
                float share = Ring.Share(k);
                if (share > 0) Arc(this, a0, a0 + (a1 - a0) * share, Tone(0.3f + 0.65f * Ring.Lit(k)));
            }
        }

        private void DrawBolt()
        {
            var (on, bright) = Pulse.Look();
            var white = Colors.White with { A = Bright };
            if (Pulse.Kindle is double k && k < 0.45)
            {
                // Kindled from below: the dark bolt, lit up from its tip.
                DrawTextureRect(Dark, BoltAt, false, white);
                float lit = (float)(1 - Math.Pow(1 - k / 0.45, 3)) * BoltAt.Size.Y;
                BoltPart(this, Bolt, BoltAt.End.Y - lit, BoltAt.End.Y, white);
            }
            else if (!on) DrawTextureRect(Dark, BoltAt, false, white);
            else
            {
                DrawTextureRect(Bolt, BoltAt, false, white);
                if (bright < 1) DrawTextureRect(Bolt, BoltAt, false, Colors.Black with { A = (float)(1 - bright) * 0.8f * Bright });
            }
        }

        /// <summary>What lights the tab, added over it: the bolt's halo and current, the drained arc's glint, the hop, discharges.</summary>
        private void DrawLight(Control on)
        {
            var (lit, _) = Pulse.Look();
            double kindle = Pulse.Kindle is double k ? 0.35 * (1 - k / BoltPulse.KindleS) : 0;
            float halo = lit ? (float)Math.Clamp(0.05 + 0.16 * Pulse.Beat * Pulse.Level + kindle + Pulse.Flare, 0, 0.8) : 0;
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
            DrawRingLight(on);
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
        }

        private void DrawRingLight(Control on)
        {
            var bright = Tone().Lerp(Colors.White, 0.55f);
            for (int k = 0; k < Ring.Count; k++)
            {
                var (a0, a1) = EnergyRing.Arc(k, Ring.Count);
                float share = Ring.Share(k), lit = Ring.Lit(k), end = a0 + (a1 - a0) * share;
                if (share <= 0) continue;
                if (lit > 0.85f)
                {
                    Arc(on, MathF.Max(a0, end - 0.09f), end, bright);
                    float at = end - (float)Ring.Glint * (end - a0);
                    Arc(on, MathF.Max(a0, at - 0.12f), MathF.Min(end, at + 0.12f), bright with { A = bright.A * 0.5f * (lit - 0.85f) / 0.15f });
                }
                if (Ring.FillingIn(k)) Arc(on, MathF.Max(a0, end - 0.18f), end, bright with { A = bright.A * (float)(0.8 * (1 - Ring.FillIn(k)) + 0.3) });
            }
            if (Ring.Hop is { Gone: < 1 } hop)
            {
                float from = EnergyRing.Arc(hop.From, Ring.Count).Start, to = EnergyRing.Arc(hop.To, Ring.Count).End;
                if (to < from) to += MathF.Tau;
                float angle = from + (to - from) * (float)(1 - Math.Pow(1 - hop.Gone, 3));
                var at = Centre + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * EnergyRing.Radius;
                Glow(on, at, 4, Tone().Lerp(Colors.White, 0.6f) with { A = 0.7f * Bright });
                on.DrawRect(new Rect2(at - new Vector2(0.75f, 0.75f), new Vector2(1.5f, 1.5f)), Colors.White with { A = 0.9f * Bright });
            }
        }

        private static void Arc(CanvasItem on, float start, float end, Color colour)
        {
            if (end <= start) return;
            int points = Math.Max(3, (int)((end - start) * EnergyRing.Radius / 1.5f));
            on.DrawArc(Centre, EnergyRing.Radius, start, end, points, colour, 1.5f, true);
        }

        private static void Glow(CanvasItem on, Vector2 at, float radius, Color colour) =>
            on.DrawTextureRect(Soft, new Rect2(at - new Vector2(radius, radius), new Vector2(2 * radius, 2 * radius)), false, colour);

        /// <summary>The rows of the bolt's picture between <paramref name="top"/> and <paramref name="bottom"/>, in the tab's pixels.</summary>
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

        /// <summary>The tab's light, drawn added over it.</summary>
        private sealed partial class Light : Control
        {
            public Tab? Tab { get; set; }

            public Light()
            {
                MouseFilter = MouseFilterEnum.Ignore;
                Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
            }

            public override void _Draw() => Tab?.DrawLight(this);
        }
    }
}
