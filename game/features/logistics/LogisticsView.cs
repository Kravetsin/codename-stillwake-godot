using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Logistics;
using Stillwake.Sim.Power;
using L = Stillwake.Game.DroneLook;
using Tune = Stillwake.Content.Logistics;

namespace Stillwake.Game.Features.Logistics;

/// <summary>
/// Drones in the world without a word (0062): each flying drone its picture, lit with relief, following the sim on a
/// spring, leaning, bobbing and swinging its cargo from its clamp (<see cref="Hover"/>), its eye and status lamp unlit
/// over it; taking off from its pad and landing back on one. A bay's docked drones on its pads, each pad's charge lamps,
/// its strip and light pulsing green while it charges, and the beacon on its top while it has power. A bay's zone
/// outlined, dashed sky, while its frame is open or a bay's card is in hand, where it would stand.
/// </summary>
public partial class LogisticsView : Node2D
{
    private const int Cell = ShipCamera.Cell;
    private const float Unit = 1000;
    /// <summary>A drone's speed in flight, pixels a second.</summary>
    private const double Cruise = Tune.Speed / 1000.0 * Cell * World.TicksPerSecond;
    private static readonly L.Motion Look = L.Lively;
    private static readonly string[] Fly = [.. Enumerable.Range(0, L.RotorFrames).Select(i => $"drone_fly_{i}")];
    private static readonly string[] ChargeLampsLit = [.. Enumerable.Range(0, 5).Select(i => $"droneBay_charge_{i}")];
    private static readonly Rect2 FlyRect = new(-L.AnchorX, -L.AnchorY, L.FlyWidth, L.FlyHeight);
    private static readonly Vector2 DockSize = new(L.DockWidth, L.DockHeight);

    private static readonly Color Zone = new("60a5fa"), Sky = new(L.Sky), Green = new(Charges.Green), Amber = new(Charges.Amber);
    private static readonly Color Tether = new("7f848f"), Jaw = new("8f99ad"), JawShade = new("474c57");
    private static readonly CanvasItemMaterial Unlit = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    private readonly IGridQueries _grid;
    private readonly IItemsQueries _items;
    private readonly IPowerQueries _power;
    private readonly LogisticsModule _logistics;
    private readonly Func<double> _progress;
    private readonly Func<int?> _openBay;
    private readonly Func<(double X, double Y)?> _planning;
    /// <summary>Docked drones, lit, over their bay; then what glows on the bay, unlit, over them.</summary>
    private readonly Node2D _docks = new() { ZIndex = Layers.Screens };
    private readonly Node2D _glowing = new() { ZIndex = Layers.Screens, Material = Unlit };
    /// <summary>Drones in the air with their cargo, lit; then their eyes and lamps, unlit so they read in the dark.</summary>
    private readonly Node2D _flying = new() { ZIndex = Layers.Drones };
    private readonly Node2D _eyes = new() { ZIndex = Layers.Drones, Material = Unlit };
    private readonly Node2D _zones = new() { ZIndex = Layers.Overlay, Material = Unlit };
    private readonly List<Glow> _padGlows = [], _lampGlows = [];
    private readonly List<Rect2> _outlined = [];

    /// <summary>A drone drawn: in the air while the sim flies it, then landing on <see cref="Slot"/> of its bay till it touches down.</summary>
    private sealed class Flier(int id)
    {
        public Hover Hover { get; } = new(id);
        public int Bay;
        /// <summary>The pad it takes off from, and the seconds left holding over it.</summary>
        public int From = -1;
        public double Hold;
        /// <summary>The pad it lands on, -1 while it flies; seconds into its landing.</summary>
        public int Slot = -1;
        public double Landed;
        public bool Low, Seen;
        public string? Cargo;
    }

    /// <summary>A bay as read this frame: its corner, its slots, whether it has power and charges.</summary>
    private sealed class BayShown
    {
        public Vector2 Corner;
        public readonly bool[] Docked = new bool[L.Pads.Length];
        public readonly float[] Share = new float[L.Pads.Length];
        public bool Powered, Charging, Seen;
    }

    private readonly Dictionary<int, Flier> _fliers = new();
    private readonly Dictionary<int, BayShown> _bays = new();
    private readonly List<(int Bay, int Slot)> _filled = [], _emptied = [];
    private readonly List<int> _gone = [];
    private readonly bool[] _now = new bool[L.Pads.Length];
    private readonly List<int> _changedIn = [], _changedOut = [];
    private bool _started;
    private double _clock;

    /// <param name="progress">How far the world is between its last tick and the next, 0 to 1.</param>
    /// <param name="openBay">The bay whose frame is open, if one is.</param>
    /// <param name="planning">Where the pointer is, in cells, while a bay's card is in hand.</param>
    public LogisticsView(IGridQueries grid, IItemsQueries items, IPowerQueries power, LogisticsModule logistics, Func<double> progress, Func<int?> openBay, Func<(double X, double Y)?> planning)
    {
        (_grid, _items, _power, _logistics, _progress, _openBay, _planning) = (grid, items, power, logistics, progress, openBay, planning);
        Lit.Set(_docks);
        Lit.Set(_flying);
        AddChild(_docks);
        AddChild(_glowing);
        AddChild(_flying);
        AddChild(_eyes);
        AddChild(_zones);
        _docks.Draw += DrawDocks;
        _glowing.Draw += DrawGlowing;
        _flying.Draw += DrawFlying;
        _eyes.Draw += DrawEyes;
        _zones.Draw += DrawZones;
    }

    public override void _Process(double delta)
    {
        _clock += delta;
        ReadBays();
        Follow(delta);
        _started = true;
        PadLights();
        LampLights();
        Zones();
        _docks.QueueRedraw();
        _glowing.QueueRedraw();
        _flying.QueueRedraw();
        _eyes.QueueRedraw();
        _zones.QueueRedraw();
    }

    private Vector2 CornerOf(Machine m) => new Vector2(m.Cell % _grid.Width, m.Cell / _grid.Width) * Cell;

    /// <summary>The middle of a docked drone's body on pad <paramref name="slot"/> of a bay at <paramref name="corner"/>.</summary>
    private static Vector2 PadCentre(Vector2 corner, int slot) =>
        corner + new Vector2(L.Pads[slot].X + L.DockCentreX, L.Pads[slot].Y + L.DockCentreY);

    /// <summary>Every bay's slots as the sim has them now, and which came to hold a drone or stopped since the last frame.</summary>
    private void ReadBays()
    {
        _filled.Clear();
        _emptied.Clear();
        foreach (var shown in _bays.Values) shown.Seen = false;
        foreach (var bay in _grid.MachinesOf(Tune.Bay))
        {
            bool known = _bays.TryGetValue(bay.Id, out var shown);
            shown ??= _bays[bay.Id] = new BayShown();
            var slots = _items.Crate(bay.Id);
            for (int i = 0; i < _now.Length; i++)
            {
                var held = i < slots.Count ? slots[i] : null;
                _now[i] = held?.Item == Tune.Drone;
                shown.Share[i] = _now[i] ? Charges.Share(held!) : 0;
            }
            if (known)
            {
                _changedIn.Clear();
                _changedOut.Clear();
                Served.Changed(shown.Docked, _now, _changedIn, _changedOut);
                foreach (int i in _changedIn) _filled.Add((bay.Id, i));
                foreach (int i in _changedOut) _emptied.Add((bay.Id, i));
            }
            Array.Copy(_now, shown.Docked, _now.Length);
            shown.Corner = CornerOf(bay);
            shown.Powered = _power.NodeAt(bay.Cell) is int node && _power.NetworkOf(node) is { Supply: > 0 };
            shown.Charging = _power.Charges(bay.Id);
            shown.Seen = true;
        }
        _gone.Clear();
        foreach (var (id, shown) in _bays)
            if (!shown.Seen) _gone.Add(id);
        foreach (int id in _gone) _bays.Remove(id);
    }

    /// <summary>
    /// Every drone the sim flies followed on its spring, a new one taking off from the pad it left; one gone from the sim
    /// into a pad of its bay landing there, one gone otherwise (dropped) gone at once.
    /// </summary>
    private void Follow(double delta)
    {
        double progress = _progress();
        int width = _grid.Width;
        foreach (var f in _fliers.Values) f.Seen = false;
        foreach (var d in _logistics.Drones)
        {
            if (!_fliers.TryGetValue(d.Id, out var f)) f = Launched(d);
            (f.Seen, f.Low, f.Cargo) = (true, LogisticsModule.Low(d), d.Cargo?.Item);
            if (f.Hold > 0 && _bays.TryGetValue(f.Bay, out var bay))
            {
                f.Hold -= delta;
                var pad = PadCentre(bay.Corner, f.From);
                f.Hover.Step(delta, pad.X, pad.Y - 1, 0, 0, 1, Cruise, Look);
                continue;
            }
            int toX = d.To % width * (int)Unit + (int)Unit / 2, toY = d.To / width * (int)Unit + (int)Unit / 2;
            var (x, y) = Served.Smoothed(d.X, d.Y, toX, toY, Tune.Speed, progress);
            int cell = (int)(y / Unit) * width + (int)(x / Unit);
            bool solid = cell < width || Hover.Solid(_grid.StructureAt(cell - width));
            f.Hover.Step(delta, x / Unit * Cell, y / Unit * Cell - Hover.Lift(solid, Look),
                Math.Sign(toX - d.X) * Cruise, Math.Sign(toY - d.Y) * Cruise, 1, Cruise, Look);
        }
        _gone.Clear();
        foreach (var (id, f) in _fliers)
        {
            if (f.Seen) continue;
            if (f.Slot < 0 ? !Lands(f) : !Landing(f, delta)) _gone.Add(id);
        }
        foreach (int id in _gone) _fliers.Remove(id);
    }

    /// <summary>
    /// A drone new to the sim's list: from the first frame on, one launched from the pad of its bay that stopped holding a
    /// drone (taking over the one landing there, which never touched down); before it, one already in the air.
    /// </summary>
    private Flier Launched(Drone d)
    {
        int at = _started ? _emptied.FindIndex(e => e.Bay == d.Bay) : -1;
        if (at >= 0 && _bays.TryGetValue(d.Bay, out var bay))
        {
            int slot = _emptied[at].Slot;
            _emptied.RemoveAt(at);
            foreach (var (id, landing) in _fliers)
                if (!landing.Seen && landing.Bay == d.Bay && landing.Slot == slot)
                {
                    _fliers.Remove(id);
                    (landing.Slot, landing.Landed, landing.Hold) = (-1, 0, 0);
                    return _fliers[d.Id] = landing;
                }
            var f = new Flier(d.Id) { Bay = d.Bay, Hold = L.LaunchHoldS, From = slot };
            var pad = PadCentre(bay.Corner, slot);
            f.Hover.Place(pad.X, pad.Y);
            return _fliers[d.Id] = f;
        }
        var flier = new Flier(d.Id) { Bay = d.Bay };
        flier.Hover.Place(d.X / Unit * Cell, d.Y / Unit * Cell - Look.Lift);
        flier.Hover.Spin = 1;
        return _fliers[d.Id] = flier;
    }

    /// <summary>A flying drone gone from the sim: landing on the pad of its bay that came to hold a drone, the nearest; whether there was one.</summary>
    private bool Lands(Flier f)
    {
        if (!_bays.TryGetValue(f.Bay, out var bay)) return false;
        int best = -1;
        float nearest = float.MaxValue;
        foreach (var (b, slot) in _filled)
        {
            if (b != f.Bay) continue;
            float far = PadCentre(bay.Corner, slot).DistanceSquaredTo(new Vector2((float)f.Hover.X, (float)f.Hover.Y));
            if (far < nearest) (best, nearest) = (slot, far);
        }
        if (best < 0) return false;
        _filled.Remove((f.Bay, best));
        (f.Slot, f.Landed, f.Hold, f.Cargo, f.Low) = (best, 0, 0, null, false);
        return true;
    }

    /// <summary>A landing drone moved on <paramref name="delta"/>: over its pad, then down onto it; whether it is still in the air.</summary>
    private bool Landing(Flier f, double delta)
    {
        if (!_bays.TryGetValue(f.Bay, out var bay) || !bay.Docked[f.Slot]) return false;
        f.Landed += delta;
        var (above, spinTo, down) = Hover.Landing(f.Landed);
        var pad = PadCentre(bay.Corner, f.Slot);
        f.Hover.Step(delta, pad.X, pad.Y - above, 0, 0, spinTo, Cruise, Look);
        return !down;
    }

    /// <summary>Whether pad <paramref name="slot"/> of bay <paramref name="bay"/> has a drone landing on it, drawn in the air till it touches down.</summary>
    private bool LandingOn(int bay, int slot)
    {
        foreach (var f in _fliers.Values)
            if (f.Bay == bay && f.Slot == slot && !f.Seen) return true;
        return false;
    }

    /// <summary>How bright a pad's strip and light are: pulsing while it charges, steady else.</summary>
    private float Pulse(bool charging, float low, float high) =>
        charging ? low + (high - low) * (0.5f + 0.5f * (float)Math.Cos(_clock * L.PadPulseHz * 2 * Math.PI)) : high;

    /// <summary>The light each powered bay's docked drones give, green or amber under a quarter, pulsing while they charge; and its beacon's.</summary>
    private void PadLights()
    {
        int n = 0;
        foreach (var (id, bay) in _bays)
        {
            if (!bay.Powered) continue;
            Light(_padGlows, n++, bay.Corner + new Vector2(L.BeaconX + 1, L.BeaconY), Sky * L.BeaconLight);
            for (int i = 0; i < L.Pads.Length; i++)
            {
                if (!bay.Docked[i] || LandingOn(id, i)) continue;
                bool charging = bay.Charging && bay.Share[i] < 1;
                var at = bay.Corner + new Vector2(L.PadGlows[i].X + 4, L.PadGlows[i].Y);
                Light(_padGlows, n++, at, (bay.Share[i] < 0.25f ? Amber : Green) * (L.PadLight * Pulse(charging, 0.4f, 1f)));
            }
        }
        Hide(_padGlows, n);
    }

    /// <summary>Each flying drone's lamp's light: sky while it works, amber blinking while it flies home low.</summary>
    private void LampLights()
    {
        int n = 0;
        foreach (var f in _fliers.Values)
        {
            var (x, y) = f.Hover.Body(L.LampX - L.AnchorX, L.LampY - L.AnchorY, f.Hover.Bob(Cruise, Look));
            if (f.Low && Blink()) Light(_lampGlows, n++, new Vector2((float)x, (float)y), Amber * L.LowLight);
            else if (!f.Low && f.Hover.Spin > 0.5) Light(_lampGlows, n++, new Vector2((float)x, (float)y), Sky * L.WorkLight);
        }
        Hide(_lampGlows, n);
    }

    private bool Blink() => Math.Sin(_clock * L.LowBlinkHz * 2 * Math.PI) > -0.3;

    /// <summary>Light <paramref name="i"/> of <paramref name="pool"/> shown at <paramref name="at"/> in <paramref name="colour"/>, the pool grown to hold it.</summary>
    private void Light(List<Glow> pool, int i, Vector2 at, Color colour)
    {
        while (pool.Count <= i)
        {
            var glow = new Glow();
            AddChild(glow);
            pool.Add(glow);
        }
        (pool[i].Position, pool[i].Colour, pool[i].Visible) = (at, colour, true);
    }

    private static void Hide(List<Glow> pool, int from)
    {
        for (int i = from; i < pool.Count; i++) pool[i].Visible = false;
    }

    /// <summary>The zones outlined now: the open bay's, and while a bay's card is in hand every bay's and the one it would make.</summary>
    private void Zones()
    {
        _outlined.Clear();
        if (_openBay() is int open && _logistics.ZoneOf(open) is { } zone) _outlined.Add(RectOf(zone));
        if (_planning() is not { } pointer) return;
        foreach (var bay in _grid.MachinesOf(Tune.Bay))
            if (bay.Id != _openBay() && _logistics.ZoneOf(bay.Id) is { } each) _outlined.Add(RectOf(each));
        var spec = Content.Machines.Get(Tune.Bay);
        int x = (int)Math.Floor(pointer.X), y = (int)Math.Floor(pointer.Y);
        // Clipped at the grid's edge, as the sim clips a bay's.
        _outlined.Add(RectOf(new Box(Math.Max(0, x - Tune.Reach), Math.Max(0, y - Tune.Reach),
            Math.Min(_grid.Width - 1, x + spec.Width - 1 + Tune.Reach), Math.Min(_grid.Height - 1, y + spec.Height - 1 + Tune.Reach))));
    }

    private static Rect2 RectOf(Box b) => new(b.Left * Cell, b.Top * Cell, b.Width * Cell, b.Height * Cell);

    /// <summary>Each docked drone folded on its pad, lit as the bay is; not one still landing there.</summary>
    private void DrawDocks()
    {
        var picture = PixelArt.Lit("drone_dock");
        foreach (var (id, bay) in _bays)
            for (int i = 0; i < L.Pads.Length; i++)
                if (bay.Docked[i] && !LandingOn(id, i))
                    _docks.DrawTextureRect(picture, new Rect2(bay.Corner + new Vector2(L.Pads[i].X, L.Pads[i].Y), DockSize), false);
    }

    /// <summary>
    /// What glows on a bay: its beacon while it has power; under each docked drone its strip, pulsing while it charges and
    /// steady once full on power, its four charge lamps by quarters (the next one blinking in while it charges, one amber
    /// under a quarter), and its eye unless it is flat.
    /// </summary>
    private void DrawGlowing()
    {
        foreach (var (id, bay) in _bays)
        {
            if (bay.Powered) _glowing.DrawRect(new Rect2(bay.Corner + new Vector2(L.BeaconX, L.BeaconY), new Vector2(2, 1)), Sky);
            for (int i = 0; i < L.Pads.Length; i++)
            {
                if (!bay.Docked[i] || LandingOn(id, i)) continue;
                float share = bay.Share[i];
                bool charging = bay.Charging && share < 1;
                if (bay.Powered && (charging || share >= 1))
                    _glowing.DrawTexture(PixelArt.Picture("droneBay_pad"), bay.Corner + new Vector2(L.PadGlows[i].X, L.PadGlows[i].Y),
                        Colors.White with { A = Pulse(charging, 0.2f, 1f) });
                int lamps = (int)Math.Floor(share * 4 + 1e-4);
                if (charging && lamps < 4 && Math.Sin(_clock * 2 * Math.PI) > 0) lamps++;
                _glowing.DrawTexture(PixelArt.Picture(share < 0.25f ? "droneBay_charge_low" : ChargeLampsLit[Math.Min(4, lamps)]),
                    bay.Corner + new Vector2(L.ChargeLamps[i].X, L.ChargeLamps[i].Y));
                if (share > 0) _glowing.DrawTexture(PixelArt.Picture("drone_dock_eye"), bay.Corner + new Vector2(L.Pads[i].X, L.Pads[i].Y));
            }
        }
    }

    private void DrawZones()
    {
        foreach (var r in _outlined)
        {
            _zones.DrawRect(r, Zone with { A = 0.05f });
            var (a, b, c, d) = (r.Position, r.Position + new Vector2(r.Size.X, 0), r.End, r.Position + new Vector2(0, r.Size.Y));
            foreach (var (from, to) in new[] { (a, b), (b, c), (c, d), (d, a) }) _zones.DrawDashedLine(from, to, Zone with { A = 0.55f }, 1, 4);
        }
    }

    /// <summary>Turns <paramref name="node"/>'s drawing to drone <paramref name="f"/>'s body: at its middle, leaning, facing its way.</summary>
    private void Turn(Node2D node, Flier f)
    {
        var h = f.Hover;
        node.DrawSetTransform(new Vector2((float)h.X, (float)(h.Y + h.Bob(Cruise, Look))), (float)h.Tilt, new Vector2(h.Face, 1));
    }

    /// <summary>Each drone in the air, its rotors' frame, lit; under it its cargo hanging from the clamp, kept upright, swinging.</summary>
    private void DrawFlying()
    {
        foreach (var f in _fliers.Values)
        {
            Turn(_flying, f);
            _flying.DrawTextureRect(PixelArt.Lit(Fly[f.Hover.Frame]), FlyRect, false);
            _flying.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
            if (f.Cargo is not { } item) continue;
            double bob = f.Hover.Bob(Cruise, Look);
            var (cx, cy) = f.Hover.Body(0, L.Clamp, bob);
            var (x, y) = f.Hover.Cargo(bob);
            var icon = new Vector2(Mathf.Round((float)x), Mathf.Round((float)y));
            _flying.DrawLine(new Vector2((float)cx, (float)cy), icon + new Vector2(0, -5), Tether, 1);
            _flying.DrawTextureRect(ItemIcons.Icon(item), new Rect2(icon - new Vector2(4, 4), new Vector2(8, 8)), false);
            _flying.DrawRect(new Rect2(icon + new Vector2(-3, -5), new Vector2(6, 1)), Jaw);
            _flying.DrawRect(new Rect2(icon + new Vector2(-3, -4), Vector2.One), JawShade);
            _flying.DrawRect(new Rect2(icon + new Vector2(2, -4), Vector2.One), JawShade);
        }
    }

    /// <summary>Each drone's eye and lamp over it, unlit: the lamp sky while its rotors run, amber blinking while it flies home low.</summary>
    private void DrawEyes()
    {
        foreach (var f in _fliers.Values)
        {
            Turn(_eyes, f);
            _eyes.DrawTextureRect(PixelArt.Picture("drone_eye"), FlyRect, false);
            if (f.Low ? Blink() : f.Hover.Spin > 0.5)
                _eyes.DrawTextureRect(PixelArt.Picture(f.Low ? "drone_lamp_low" : "drone_lamp_work"), FlyRect, false);
        }
        _eyes.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }
}
