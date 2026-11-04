using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Logistics;
using Stillwake.Sim.Power;
using B = Stillwake.Game.Features.Logistics.BayInk;
using L = Stillwake.Game.DroneLook;
using S = Stillwake.Game.Sound;
using SI = Stillwake.Game.Features.Logistics.SalvageInk;
using SL = Stillwake.Game.SalvageLook;
using Tune = Stillwake.Content.Logistics;

namespace Stillwake.Game.Features.Logistics;

/// <summary>
/// Drones in the world without a word (0062): each flying drone its picture, lit with relief, following the sim on a
/// spring, leaning, bobbing and swinging its cargo from its clamp (<see cref="Hover"/>), its eye and status lamp unlit
/// over it; taking off from its pad and landing back on one. A salvage drone (salvage-drones) lifts on puffs of air
/// (<see cref="Puffs"/>), bobbing only in the ship's air, and carries what it caught in its net (<see cref="Grip"/>,
/// <see cref="NetPile"/>).
/// A bay (<see cref="BayInk"/>): its docked drones on its arms, the lugs over their feet, each arm's lamps by its drone's
/// charge, the fuel slot's canister, valve, vane and lamp, the beacon on its top while it has power. A bay's zone
/// outlined, dashed sky, while its frame is open or a bay's card is in hand, where it would stand. Heard where they are:
/// each drone's rotors while it flies (<see cref="Whir"/>), a salvage drone's hiss, a thud as it lifts off its pad and as
/// it touches down, and a click as its clamp takes a stack or puts it in.
/// </summary>
public partial class LogisticsView : Node2D
{
    private const int Cell = ShipCamera.Cell;
    private const float Unit = 1000;
    /// <summary>A drone's speed in flight, pixels a second.</summary>
    private const double Cruise = Tune.Speed / 1000.0 * Cell * World.TicksPerSecond;
    private static readonly L.Motion Look = L.Lively;
    private static readonly string[] Fly = [.. Enumerable.Range(0, L.RotorFrames).Select(i => $"drone_fly_{i}")];
    private static readonly string[] LampsLit = [.. Enumerable.Range(0, 5).Select(i => $"droneBay_lamps_{i}")];
    private static readonly string[] Puffed = [.. Enumerable.Range(0, SI.Puffs).Select(i => $"salvage_puff_{i}")];
    private static readonly Rect2 FlyRect = new(-L.AnchorX, -L.AnchorY, L.FlyWidth, L.FlyHeight);
    private static readonly Vector2 DockSize = new(L.DockWidth, L.DockHeight);
    private static readonly Rect2 SalvageFly = new(-SI.AnchorX - SI.Margin, -SI.AnchorY - SI.Margin, SI.FlyWidth + 2 * SI.Margin, SI.FlyHeight + 2 * SI.Margin);
    private static readonly Vector2 SalvageDock = new(SI.DockWidth + 2 * SI.Margin, SI.DockHeight + 2 * SI.Margin);
    private const float PuffSide = SI.PuffSize + 2 * SI.PuffMargin;
    private static readonly Vector2 Front = new(-B.Margin, -B.Margin);

    private static readonly Color Zone = new("60a5fa"), Sky = new(L.Sky), Green = new(Charges.Green), Amber = new(Charges.Amber), Red = new(SL.Low);
    private static readonly Color Tether = new("7f848f"), Jaw = new("8f99ad"), JawShade = new("474c57");
    private static readonly CanvasItemMaterial Unlit = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    private readonly IGridQueries _grid;
    private readonly IItemsQueries _items;
    private readonly IPowerQueries _power;
    private readonly IAtmosphereQueries _air;
    private readonly LogisticsModule _logistics;
    private readonly Func<double> _progress;
    private readonly Func<int?> _openBay;
    private readonly Func<(double X, double Y, string Kind)?> _planning;
    /// <summary>
    /// Docked drones, lit, on their bay; over them the lugs, the canister and the bay's parts as they are dark; then what
    /// glows on the bay, unlit.
    /// </summary>
    private readonly Node2D _docks = new() { ZIndex = Layers.Screens };
    private readonly Node2D _fronts = new() { ZIndex = Layers.Screens };
    private readonly Node2D _glowing = new() { ZIndex = Layers.Screens, Material = Unlit };
    /// <summary>Drones in the air with their cargo, lit; then their eyes and lamps, unlit so they read in the dark.</summary>
    private readonly Node2D _flying = new() { ZIndex = Layers.Drones };
    private readonly Node2D _eyes = new() { ZIndex = Layers.Drones, Material = Unlit };
    private readonly Node2D _zones = new() { ZIndex = Layers.Overlay, Material = Unlit };
    private readonly List<Glow> _padGlows = [], _lampGlows = [];
    private readonly List<Rect2> _outlined = [];
    /// <summary>The drones' light and wash (0075).</summary>
    private readonly DroneLight _light = new();
    private readonly Heard2D _dock = Cues.Player2D(S.DroneDock, S.World, voices: 4), _grab = Cues.Player2D(S.DroneGrab, S.World, voices: 4);

    /// <summary>
    /// A drone drawn: in the air while the sim flies it, then landing on <see cref="Slot"/> of its bay till it touches down;
    /// then, <see cref="Down"/>, only heard, its motor running down, and a salvage drone's last puffs fading.
    /// </summary>
    private sealed class Flier(int id, bool salvage, Heard2D loop)
    {
        /// <summary>The id it was first drawn as: its bob, its rotors and its tuning stay its own when another launch takes it over.</summary>
        public int Id { get; } = id;
        /// <summary>Whether it is a salvage drone: puffs and a net, no rotors.</summary>
        public bool Salvage { get; } = salvage;
        public Hover Hover { get; } = new(id);
        public Trail Trail { get; } = new();
        public Heard2D Loop { get; } = loop;
        public Puffs Puffs { get; } = new();
        public Grip Grip { get; } = new(0);
        public Whir Whir;
        public LoopEnvelope Thrust;
        public int Bay;
        /// <summary>The pad it takes off from, and the seconds left holding over it.</summary>
        public int From = -1;
        public double Hold;
        /// <summary>The pad it lands on, -1 while it flies; seconds into its landing.</summary>
        public int Slot = -1;
        public double Landed;
        public bool Low, Seen, Down;
        public (string Item, int Count)? Cargo;
        /// <summary>What a salvage drone holds in its net, in catch order: each piece's own picture, a stack's icon; read from <see cref="HeldFrom"/>.</summary>
        public Texture2D[] Held = [];
        public IReadOnlyList<Netted>? HeldFrom;

        public L.Motion Motion => Salvage ? SL.Drift : Look;

        /// <summary>Whether it is still heard or seen once down: its motor running down, its puffs fading.</summary>
        public bool Lingers => Salvage ? Thrust.Playing || Puffs.Alive.Count > 0 : Whir.Playing;
    }

    /// <summary>A bay as read this frame: its corner, its slots, whether it has power and charges, its canisters and when they last changed.</summary>
    private sealed class BayShown
    {
        public Vector2 Corner;
        public readonly bool[] Docked = new bool[B.Pads.Length];
        public readonly bool[] Salvage = new bool[B.Pads.Length];
        public readonly float[] Share = new float[B.Pads.Length];
        public bool Powered, Charging, Seen;
        public int Fuel;
        /// <summary>When a canister was last spent, and when one came into an empty slot, by the view's clock.</summary>
        public double FedAt = double.NegativeInfinity, CanAt = double.NegativeInfinity;
    }

    private readonly Dictionary<int, Flier> _fliers = new();
    private readonly Dictionary<int, BayShown> _bays = new();
    private readonly List<(int Bay, int Slot, bool Salvage)> _filled = [], _emptied = [];
    private readonly List<int> _gone = [];
    private readonly bool[] _now = new bool[B.Pads.Length], _salvage = new bool[B.Pads.Length];
    private readonly List<int> _changedIn = [], _changedOut = [];
    private bool _started;
    private double _clock;

    /// <param name="progress">How far the world is between its last tick and the next, 0 to 1.</param>
    /// <param name="openBay">The bay whose frame is open, if one is.</param>
    /// <param name="planning">Where the pointer is, in cells, while a bay's card is in hand.</param>
    public LogisticsView(IGridQueries grid, IItemsQueries items, IPowerQueries power, IAtmosphereQueries air, LogisticsModule logistics, Func<double> progress, Func<int?> openBay, Func<(double X, double Y, string Kind)?> planning)
    {
        (_grid, _items, _power, _air, _logistics, _progress, _openBay, _planning) = (grid, items, power, air, logistics, progress, openBay, planning);
        Lit.Set(_docks);
        Lit.Set(_fronts);
        Lit.Set(_flying);
        AddChild(_docks);
        AddChild(_fronts);
        AddChild(_light);
        AddChild(_glowing);
        AddChild(_flying);
        AddChild(_eyes);
        AddChild(_zones);
        _docks.Draw += DrawDocks;
        _fronts.Draw += DrawFronts;
        _glowing.Draw += DrawGlowing;
        _flying.Draw += DrawFlying;
        _eyes.Draw += DrawEyes;
        _zones.Draw += DrawZones;
        AddChild(_dock);
        AddChild(_grab);
    }

    public override void _Process(double delta)
    {
        _clock += delta;
        ReadBays();
        Follow(delta);
        Wash(delta);
        _started = true;
        PadLights();
        LampLights();
        Zones();
        _docks.QueueRedraw();
        _fronts.QueueRedraw();
        _glowing.QueueRedraw();
        _flying.QueueRedraw();
        _eyes.QueueRedraw();
        _zones.QueueRedraw();
    }

    private Vector2 CornerOf(Machine m) => new Vector2(m.Cell % _grid.Width, m.Cell / _grid.Width) * Cell;

    /// <summary>Where a docked drone's picture box is set on pad <paramref name="slot"/>: a salvage drone's higher, its feet on the arm.</summary>
    private static Vector2 DockAt(Vector2 corner, int slot, bool salvage) =>
        corner + new Vector2(B.Pads[slot].X, B.Pads[slot].Y + (salvage ? B.Feet - SI.DockFeet : 0));

    /// <summary>The middle of a docked drone's body on pad <paramref name="slot"/> of a bay at <paramref name="corner"/>.</summary>
    private static Vector2 PadCentre(Vector2 corner, int slot, bool salvage) =>
        DockAt(corner, slot, salvage) + (salvage ? new Vector2(SI.DockAnchorX, SI.DockAnchorY) : new Vector2(L.DockCentreX, L.DockCentreY));

    /// <summary>Whether the cell under (<paramref name="x"/>, <paramref name="y"/>), pixels, holds the ship's air.</summary>
    private bool Aired(double x, double y)
    {
        int cx = (int)Math.Floor(x / Cell), cy = (int)Math.Floor(y / Cell);
        return cx >= 0 && cy >= 0 && cx < _grid.Width && cy < _grid.Height && _air.AiredAt(cy * _grid.Width + cx);
    }

    /// <summary>Every bay's slots as the sim has them now, and which came to hold a drone or stopped since the last frame.</summary>
    private void ReadBays()
    {
        _filled.Clear();
        _emptied.Clear();
        foreach (var shown in _bays.Values) shown.Seen = false;
        foreach (var bay in _grid.MachinesWith(Content.MachineTrait.Zone))
        {
            bool known = _bays.TryGetValue(bay.Id, out var shown);
            shown ??= _bays[bay.Id] = new BayShown();
            var slots = _items.Crate(bay.Id);
            for (int i = 0; i < _now.Length; i++)
            {
                var held = i < slots.Count ? slots[i] : null;
                _salvage[i] = held?.Item == Tune.SalvageDrone;
                _now[i] = held?.Item == Tune.Drone || _salvage[i];
                shown.Share[i] = _now[i] ? Charges.Share(held!) : 0;
            }
            int fuel = Content.Machines.Get(bay.Kind).SlotTakes is { Slot: var at } && at < slots.Count ? slots[at]?.Count ?? 0 : 0;
            if (known)
            {
                _changedIn.Clear();
                _changedOut.Clear();
                Served.Changed(shown.Docked, _now, _changedIn, _changedOut);
                foreach (int i in _changedIn) _filled.Add((bay.Id, i, _salvage[i]));
                foreach (int i in _changedOut) _emptied.Add((bay.Id, i, shown.Salvage[i]));
                if (fuel < shown.Fuel) shown.FedAt = _clock;
                if (shown.Fuel == 0 && fuel > 0) shown.CanAt = _clock;
            }
            Array.Copy(_now, shown.Docked, _now.Length);
            Array.Copy(_salvage, shown.Salvage, _salvage.Length);
            shown.Fuel = fuel;
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
    /// Every drone the sim flies followed on its spring, a new one taking off from the pad it left, its clamp heard as its
    /// cargo comes and goes; one gone from the sim into a pad of its bay landing there, one gone otherwise (dropped) no
    /// longer drawn. Each heard till its motor has run down, then let go.
    /// </summary>
    private void Follow(double delta)
    {
        double progress = _progress();
        int width = _grid.Width;
        foreach (var f in _fliers.Values) f.Seen = false;
        foreach (var d in _logistics.Drones)
        {
            if (!_fliers.TryGetValue(d.Id, out var f)) f = Launched(d);
            (string, int)? cargo = d.Cargo is { } c ? (c.Item, c.Count) : null;
            if (Served.Gripped(f.Cargo, cargo)) _grab.PlayAt(ToGlobal(At(f)));
            (f.Seen, f.Low, f.Cargo) = (true, LogisticsModule.Low(d), cargo);
            if (f.Salvage)
            {
                if (!ReferenceEquals(f.HeldFrom, d.Caught) || f.Held.Length != Math.Min(d.Caught.Count, Tune.SalvageHolds)) (f.Held, f.HeldFrom) = (HeldBy(d), d.Caught);
                f.Grip.Step(delta, f.Held.Length);
            }
            // A salvage drone's "rotors" are the air holding it up: it bobs only in the ship's air.
            double spin = !f.Salvage || Aired(f.Hover.X, f.Hover.Y) ? 1 : 0;
            f.Trail.See(d.X, d.Y, progress);
            if (f.Hold > 0 && _bays.TryGetValue(f.Bay, out var bay))
            {
                f.Hold -= delta;
                var pad = PadCentre(bay.Corner, f.From, f.Salvage);
                f.Hover.Step(delta, pad.X, pad.Y - 1, 0, 0, spin, Cruise, f.Motion);
                continue;
            }
            var (x, y) = f.Trail.At;
            var (perX, perY) = f.Trail.PerTick;
            int cell = (int)(y / Unit) * width + (int)(x / Unit);
            // Lowered for a ceiling over where it is drawn or over the cell it flies to, so it dips before it gets there.
            double lift = Math.Min(LiftAt(cell, perY != 0, f.Motion), LiftAt(d.To, d.To % width == d.Cell % width && d.To != d.Cell, f.Motion));
            f.Hover.Step(delta, x / Unit * Cell, y / Unit * Cell - f.Hover.EaseLift(lift, delta),
                perX / Unit * Cell * World.TicksPerSecond, perY / Unit * Cell * World.TicksPerSecond, spin, Cruise, f.Motion);
        }
        foreach (var f in _fliers.Values)
            if (!f.Seen && !f.Down && (f.Slot < 0 ? !Lands(f) : !Landing(f, delta))) f.Down = true;
        _gone.Clear();
        foreach (var (id, f) in _fliers)
        {
            if (f.Salvage)
            {
                var h = f.Hover;
                f.Puffs.Step(delta, h.X, h.Y + h.Bob(Cruise, f.Motion), h.Vx, h.Vy, h.Tilt, h.Face, Aired(h.X, h.Y), f.Low, !f.Down);
            }
            Hear(f, delta);
            if (f.Down && !f.Lingers) _gone.Add(id);
        }
        foreach (int id in _gone)
        {
            _fliers[id].Loop.QueueFree();
            _fliers.Remove(id);
        }
    }

    /// <summary>What salvage drone <paramref name="d"/>'s net holds, in catch order, at most <c>SalvageHolds</c>: each piece by its own picture, a lifted stack by its item's icon.</summary>
    private static Texture2D[] HeldBy(Drone d)
    {
        var held = new Texture2D[Math.Min(d.Caught.Count, Tune.SalvageHolds)];
        for (int i = 0; i < held.Length; i++)
        {
            var (kind, piece) = d.Caught[i];
            held[i] = piece != 0 && DebrisLooks.NameOf(kind, piece) is { } name ? PixelArt.Picture(name) : ItemIcons.Icon(kind, Cell);
        }
        return held;
    }

    private static Vector2 At(Flier f) => new((float)f.Hover.X, (float)f.Hover.Y);

    /// <summary>How far over <paramref name="cell"/>'s centre a drone flies there (<see cref="Hover.Lift"/>), <paramref name="upOrDown"/> or across.</summary>
    private double LiftAt(int cell, bool upOrDown, L.Motion motion)
    {
        int width = _grid.Width;
        int? above = cell < width ? null : _grid.StructureAt(cell - width);
        int? twoAbove = cell < 2 * width ? null : _grid.StructureAt(cell - 2 * width);
        return Hover.Lift(_grid.StructureAt(cell), above, twoAbove, upOrDown, motion);
    }

    /// <summary>A drone set down on or lifted off <paramref name="pad"/>, a pad's middle: heard there, a puff of dust and a ring.</summary>
    private void Thud(Vector2 pad)
    {
        _dock.PlayAt(ToGlobal(pad));
        DroneLight.Touched(ToGlobal(pad));
    }

    /// <summary>Each drone in the air lit and washing the air under it (<see cref="DroneLight"/>), the floor under it found in the grid; a salvage drone has no rotors to wash with.</summary>
    private void Wash(double delta)
    {
        _light.Begin();
        foreach (var f in _fliers.Values)
            if (!f.Down && !f.Salvage) _light.Drone(f.Id, f.Hover, f.Hover.Bob(Cruise, Look), FloorUnder(f.Hover.X, f.Hover.Y), delta * 1000);
        _light.End();
    }

    /// <summary>The top of the first solid cell under (<paramref name="x"/>, <paramref name="y"/>), pixels, within eight cells; none past them.</summary>
    private double? FloorUnder(double x, double y)
    {
        int cx = (int)Math.Floor(x / Cell), cy = (int)Math.Floor(y / Cell);
        if (cx < 0 || cx >= _grid.Width) return null;
        for (int row = Math.Max(0, cy + 1); row <= cy + 8 && row < _grid.Height; row++)
            if (Hover.Solid(_grid.StructureAt(row * _grid.Width + cx))) return row * Cell;
        return null;
    }

    /// <summary>
    /// Drone <paramref name="f"/> heard where it is while it is in the air: a drone's rotors, its loop's pitch and volume
    /// from its speed and its id; a salvage drone's hiss of air, as a jetpack's but quieter.
    /// </summary>
    private static void Hear(Flier f, double delta)
    {
        var loop = f.Loop;
        loop.Position = At(f);
        if (f.Salvage)
        {
            f.Thrust.Step(!f.Down, delta, S.ThrustAttackS, S.ThrustReleaseS);
            loop.OffsetDb = f.Thrust.GainDb;
            if (f.Thrust.Playing && !loop.Playing) loop.Play((float)(Random.Shared.NextDouble() * 2));
            else if (!f.Thrust.Playing && loop.Playing) loop.Stop();
            return;
        }
        f.Whir.Step(!f.Down, delta);
        // Begun somewhere along its loop, so two drones never whir in step.
        if (f.Whir.Playing && !loop.Playing) loop.Play((float)(Random.Shared.NextDouble() * 2));
        else if (!f.Whir.Playing && loop.Playing) loop.Stop();
        double speed = f.Hover.Speed(Cruise);
        loop.PitchScale = f.Whir.Pitch(f.Id, speed);
        loop.OffsetDb = f.Whir.GainDb(f.Id, speed);
    }

    /// <summary>A new drawn drone, its loop with it: rotors, or a salvage drone's hiss.</summary>
    private Flier New(int id, int bay, bool salvage)
    {
        var f = new Flier(id, salvage, Cues.Player2D(salvage ? S.SalvageThrust : S.DroneWhir, S.World)) { Bay = bay };
        AddChild(f.Loop);
        return f;
    }

    /// <summary>
    /// A drone new to the sim's list: from the first frame on, one launched from the pad of its bay that stopped holding a
    /// drone of its kind, with a thud as it lifts off (taking over the one landing there, which then never touched down,
    /// or the one that touched down there, its motor still running down); before it, one already in the air.
    /// </summary>
    private Flier Launched(Drone d)
    {
        bool salvage = d.Stack.Item == Tune.SalvageDrone;
        int at = _started ? _emptied.FindIndex(e => e.Bay == d.Bay && e.Salvage == salvage) : -1;
        if (at >= 0 && _bays.TryGetValue(d.Bay, out var bay))
        {
            int slot = _emptied[at].Slot;
            _emptied.RemoveAt(at);
            foreach (var (id, landing) in _fliers)
                if (!landing.Seen && landing.Bay == d.Bay && landing.Slot == slot && landing.Salvage == salvage)
                {
                    _fliers.Remove(id);
                    bool landed = landing.Down;
                    (landing.Slot, landing.Landed, landing.Down) = (-1, 0, false);
                    (landing.Hold, landing.From) = landed ? (L.LaunchHoldS, slot) : (0, -1);
                    if (landed) Thud(PadCentre(bay.Corner, slot, salvage));
                    return _fliers[d.Id] = landing;
                }
            var f = New(d.Id, d.Bay, salvage);
            (f.Hold, f.From) = (L.LaunchHoldS, slot);
            var pad = PadCentre(bay.Corner, slot, salvage);
            f.Hover.Place(pad.X, pad.Y);
            Thud(pad);
            return _fliers[d.Id] = f;
        }
        var flier = New(d.Id, d.Bay, salvage);
        flier.Hover.Place(d.X / Unit * Cell, d.Y / Unit * Cell - flier.Motion.Lift);
        flier.Hover.Spin = 1;
        flier.Cargo = d.Cargo is { } c ? (c.Item, c.Count) : null;
        return _fliers[d.Id] = flier;
    }

    /// <summary>A flying drone gone from the sim: landing on the pad of its bay that came to hold a drone of its kind, the nearest; whether there was one.</summary>
    private bool Lands(Flier f)
    {
        if (!_bays.TryGetValue(f.Bay, out var bay)) return false;
        int best = -1;
        float nearest = float.MaxValue;
        foreach (var (b, slot, salvage) in _filled)
        {
            if (b != f.Bay || salvage != f.Salvage) continue;
            float far = PadCentre(bay.Corner, slot, salvage).DistanceSquaredTo(At(f));
            if (far < nearest) (best, nearest) = (slot, far);
        }
        if (best < 0) return false;
        _filled.Remove((f.Bay, best, f.Salvage));
        (f.Slot, f.Landed, f.Hold, f.Cargo, f.Low, f.Held, f.HeldFrom) = (best, 0, 0, null, false, [], null);
        f.Grip.Step(0, 0);
        return true;
    }

    /// <summary>
    /// A landing drone moved on <paramref name="delta"/>: over its pad, then down onto it, touching down with a thud; whether
    /// its pad still holds it.
    /// </summary>
    private bool Landing(Flier f, double delta)
    {
        if (!_bays.TryGetValue(f.Bay, out var bay) || !bay.Docked[f.Slot]) return false;
        f.Landed += delta;
        var (above, spinTo, down) = Hover.Landing(f.Landed);
        var pad = PadCentre(bay.Corner, f.Slot, f.Salvage);
        if (f.Salvage && !Aired(f.Hover.X, f.Hover.Y)) spinTo = 0;
        f.Hover.Step(delta, pad.X, pad.Y - above, 0, 0, spinTo, Cruise, f.Motion);
        if (down)
        {
            f.Down = true;
            Thud(pad);
        }
        return true;
    }

    /// <summary>Whether pad <paramref name="slot"/> of bay <paramref name="bay"/> has a drone landing on it, drawn in the air till it touches down.</summary>
    private bool LandingOn(int bay, int slot)
    {
        foreach (var f in _fliers.Values)
            if (f.Bay == bay && f.Slot == slot && !f.Seen && !f.Down) return true;
        return false;
    }

    /// <summary>Whether pad <paramref name="slot"/> of <paramref name="shown"/> shows a docked drone: one holds it, and none is still landing there.</summary>
    private bool DockedOn(int id, BayShown shown, int slot) => shown.Docked[slot] && !LandingOn(id, slot);

    /// <summary>How bright a pad's light is: pulsing while it charges, steady else.</summary>
    private float Pulse(bool charging, float low, float high) =>
        charging ? low + (high - low) * (0.5f + 0.5f * (float)Math.Cos(_clock * L.PadPulseHz * 2 * Math.PI)) : high;

    /// <summary>The fuel slot's state on <paramref name="bay"/>: whether its valve feeds now, and the vane's step while it does.</summary>
    private (bool Feeding, int Step) Feed(BayShown bay)
    {
        double since = _clock - bay.FedAt;
        return since < SL.FeedS ? (true, (int)Math.Floor(since / SL.FlowStepS) % 2) : (false, 0);
    }

    /// <summary>
    /// The light a powered bay gives: each arm's lamps', green, red on a flat drone, pulsing while it charges; the fuel
    /// slot's lamp's, red empty, amber holding a canister, green blinking while it feeds; and its beacon's.
    /// </summary>
    private void PadLights()
    {
        int n = 0;
        foreach (var (id, bay) in _bays)
        {
            if (!bay.Powered) continue;
            Light(_padGlows, n++, bay.Corner + new Vector2(B.BeaconLight.X, B.BeaconLight.Y), Sky * L.BeaconLight);
            var (feeding, step) = Feed(bay);
            if (!feeding || step == 0)
                Light(_padGlows, n++, bay.Corner + new Vector2(B.SlotLamp.X, B.SlotLamp.Y), (feeding ? Green : bay.Fuel > 0 ? Amber : Red) * L.PadLight);
            for (int i = 0; i < B.Pads.Length; i++)
            {
                if (!DockedOn(id, bay, i)) continue;
                bool charging = bay.Charging && bay.Share[i] < 1;
                var at = bay.Corner + new Vector2(B.Pads[i].X + B.Lamps.X + 3, B.Pads[i].Y + B.Lamps.Y + 1);
                Light(_padGlows, n++, at, (bay.Share[i] < 0.25f ? Red : Green) * (L.PadLight * Pulse(charging, 0.4f, 1f)));
            }
        }
        Hide(_padGlows, n);
    }

    /// <summary>Each flying drone's lamp's light: sky while it works, amber (a salvage drone's red) blinking while it flies home low.</summary>
    private void LampLights()
    {
        int n = 0;
        foreach (var f in _fliers.Values)
        {
            if (f.Down) continue;
            var (lx, ly) = f.Salvage ? (SI.LampX - SI.AnchorX, SI.LampY - SI.AnchorY) : (L.LampX - L.AnchorX, L.LampY - L.AnchorY);
            var (x, y) = f.Hover.Body(lx, ly, f.Hover.Bob(Cruise, f.Motion));
            if (f.Low && Blink()) Light(_lampGlows, n++, new Vector2((float)x, (float)y), (f.Salvage ? Red : Amber) * L.LowLight);
            else if (!f.Low && (f.Salvage || f.Hover.Spin > 0.5)) Light(_lampGlows, n++, new Vector2((float)x, (float)y), Sky * L.WorkLight);
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
        if (_planning() is not { } planned) return;
        foreach (var bay in _grid.MachinesWith(Content.MachineTrait.Zone))
            if (bay.Id != _openBay() && _logistics.ZoneOf(bay.Id) is { } each) _outlined.Add(RectOf(each));
        var spec = Content.Machines.Get(planned.Kind);
        int x = (int)Math.Floor(planned.X), y = (int)Math.Floor(planned.Y);
        // Clipped at the grid's edge, as the sim clips a bay's.
        _outlined.Add(RectOf(new Box(Math.Max(0, x - Tune.Reach), Math.Max(0, y - Tune.Reach),
            Math.Min(_grid.Width - 1, x + spec.Width - 1 + Tune.Reach), Math.Min(_grid.Height - 1, y + spec.Height - 1 + Tune.Reach))));
    }

    private static Rect2 RectOf(Box b) => new(b.Left * Cell, b.Top * Cell, b.Width * Cell, b.Height * Cell);

    /// <summary>Each docked drone folded on its pad, lit as the bay is; not one still landing there.</summary>
    private void DrawDocks()
    {
        foreach (var (id, bay) in _bays)
            for (int i = 0; i < B.Pads.Length; i++)
            {
                if (!DockedOn(id, bay, i)) continue;
                var at = DockAt(bay.Corner, i, bay.Salvage[i]);
                if (bay.Salvage[i]) _docks.DrawTextureRect(PixelArt.Lit("salvage_dock"), new Rect2(at - new Vector2(SI.Margin, SI.Margin), SalvageDock), false);
                else _docks.DrawTextureRect(PixelArt.Lit("drone_dock"), new Rect2(at, DockSize), false);
            }
    }

    /// <summary>
    /// Over the docked drones, lit: the bay's lugs over their feet; the canister in its breech while the slot holds one or
    /// feeds, sliding in as it comes; and what of the bay is dark: lamps with no power or no drone, the fuel slot and the
    /// beacon without power.
    /// </summary>
    private void DrawFronts()
    {
        foreach (var (id, bay) in _bays)
        {
            _fronts.DrawTextureRect(PixelArt.Lit("droneBay_front"), new Rect2(bay.Corner + Front, new Vector2(32 + 2 * B.Margin, 32 + 2 * B.Margin)), false);
            if (bay.Fuel > 0 || Feed(bay).Feeding)
            {
                float k = (float)Math.Clamp((_clock - bay.CanAt) / SL.CanisterInS, 0, 1), e = 1 - (1 - k) * (1 - k);
                var slide = new Vector2(B.SlideX, B.SlideY) * (1 - e);
                _fronts.DrawTexture(PixelArt.Picture("droneBay_canister"), bay.Corner + new Vector2(B.Canister.X, B.Canister.Y) + Front + slide,
                    Colors.White with { A = Math.Min(1, k * 3) });
            }
            if (!bay.Powered)
            {
                _fronts.DrawTexture(PixelArt.Picture("droneBay_fuel_off"), bay.Corner + new Vector2(B.Fuel.X, B.Fuel.Y) + Front);
                _fronts.DrawTexture(PixelArt.Picture("droneBay_beacon_off"), bay.Corner + new Vector2(B.Beacon.X, B.Beacon.Y) + Front);
            }
            for (int i = 0; i < B.Pads.Length; i++)
                if (!bay.Powered || !DockedOn(id, bay, i))
                    _fronts.DrawTexture(PixelArt.Picture(bay.Powered ? LampsLit[0] : "droneBay_lamps_off"), LampsAt(bay, i));
        }
    }

    private static Vector2 LampsAt(BayShown bay, int slot) =>
        bay.Corner + new Vector2(B.Pads[slot].X + B.Lamps.X, B.Pads[slot].Y + B.Lamps.Y) + Front;

    /// <summary>
    /// What glows on a powered bay: its beacon; the fuel slot's lamp, valve and vane (red empty, amber holding a canister,
    /// green with the vane turning while it feeds); under each docked drone its arm's four lamps by quarters (the next one
    /// blinking in while it charges, the first red under a quarter). On any bay, each docked drone's eye unless it is flat.
    /// </summary>
    private void DrawGlowing()
    {
        foreach (var (id, bay) in _bays)
        {
            if (bay.Powered)
            {
                _glowing.DrawTexture(PixelArt.Picture("droneBay_beacon_on"), bay.Corner + new Vector2(B.Beacon.X, B.Beacon.Y) + Front);
                var (feeding, step) = Feed(bay);
                string fuel = feeding ? $"droneBay_fuel_feed_{step}" : bay.Fuel > 0 ? "droneBay_fuel_can" : "droneBay_fuel_empty";
                _glowing.DrawTexture(PixelArt.Picture(fuel), bay.Corner + new Vector2(B.Fuel.X, B.Fuel.Y) + Front);
            }
            for (int i = 0; i < B.Pads.Length; i++)
            {
                if (!DockedOn(id, bay, i)) continue;
                float share = bay.Share[i];
                if (bay.Powered)
                {
                    bool charging = bay.Charging && share < 1;
                    int lamps = (int)Math.Floor(share * 4 + 1e-4);
                    if (charging && lamps < 4 && Math.Sin(_clock * 2 * Math.PI) > 0) lamps++;
                    _glowing.DrawTexture(PixelArt.Picture(share < 0.25f ? "droneBay_lamps_low" : LampsLit[Math.Min(4, lamps)]), LampsAt(bay, i));
                }
                if (share <= 0) continue;
                var at = DockAt(bay.Corner, i, bay.Salvage[i]);
                if (bay.Salvage[i]) _glowing.DrawTextureRect(PixelArt.Picture("salvage_dock_eye"), new Rect2(at - new Vector2(SI.Margin, SI.Margin), SalvageDock), false);
                else _glowing.DrawTexture(PixelArt.Picture("drone_dock_eye"), at);
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

    /// <summary>Drone <paramref name="f"/>'s body's frame: at its middle, leaning, facing its way.</summary>
    private static Transform2D BodyOf(Flier f)
    {
        var h = f.Hover;
        return new Transform2D((float)h.Tilt, new Vector2(h.Face, 1), 0, new Vector2((float)h.X, (float)(h.Y + h.Bob(Cruise, f.Motion))));
    }

    /// <summary>
    /// Each salvage drone's puffs, lit, behind the drones; then each drone in the air: a drone's rotors' frame and under it
    /// its cargo hanging from the clamp, kept upright, swinging; a salvage drone's net by its grip, the piece in it swinging
    /// from the net's mouth, the net's near side over it once shut.
    /// </summary>
    private void DrawFlying()
    {
        foreach (var f in _fliers.Values)
        {
            if (!f.Salvage) continue;
            foreach (var p in f.Puffs.Alive)
            {
                float side = PuffSide * (float)p.Size;
                _flying.DrawSetTransform(new Vector2((float)p.X, (float)p.Y), (float)p.Spin, Vector2.One);
                _flying.DrawTextureRect(PixelArt.Picture(Puffed[p.Picture]), new Rect2(-side / 2, -side / 2, side, side), false, Colors.White with { A = (float)p.Alpha });
            }
        }
        foreach (var f in _fliers.Values)
        {
            if (f.Down) continue;
            if (f.Salvage)
            {
                DrawSalvage(f);
                continue;
            }
            _flying.DrawSetTransformMatrix(BodyOf(f));
            _flying.DrawTextureRect(PixelArt.Lit(Fly[f.Hover.Frame]), FlyRect, false);
            _flying.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
            if (f.Cargo is not var (item, _)) continue;
            double bob = f.Hover.Bob(Cruise, Look);
            var (cx, cy) = f.Hover.Body(0, L.Clamp, bob);
            var (x, y) = f.Hover.Cargo(bob);
            var icon = new Vector2(Mathf.Round((float)x), Mathf.Round((float)y));
            _flying.DrawLine(new Vector2((float)cx, (float)cy), icon + new Vector2(0, -5), Tether, 1);
            _flying.DrawTextureRect(ItemIcons.Icon(item, 8), new Rect2(icon - new Vector2(4, 4), new Vector2(8, 8)), false);
            _flying.DrawRect(new Rect2(icon + new Vector2(-3, -5), new Vector2(6, 1)), Jaw);
            _flying.DrawRect(new Rect2(icon + new Vector2(-3, -4), Vector2.One), JawShade);
            _flying.DrawRect(new Rect2(icon + new Vector2(2, -4), Vector2.One), JawShade);
        }
        _flying.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary>
    /// A salvage drone in the air, as the pick page draws it: its net by its grip, what it caught piled on the net's bottom
    /// (<see cref="NetPile"/>), swung from the pile's top.
    /// </summary>
    private void DrawSalvage(Flier f)
    {
        var body = BodyOf(f);
        var pose = f.Grip.Pose;
        _flying.DrawSetTransformMatrix(body);
        _flying.DrawTextureRect(PixelArt.Lit(pose == GripPose.Shut ? "salvage_fly_shut" : "salvage_fly_open"), SalvageFly, false);
        var held = f.Held;
        if (held.Length == 0 || pose == GripPose.Open) return;
        Span<(float W, float H)> sizes = stackalloc (float W, float H)[held.Length];
        Span<(float X, float Y, float W, float H)> laid = stackalloc (float X, float Y, float W, float H)[held.Length];
        for (int i = 0; i < held.Length; i++) sizes[i] = (held[i].GetWidth(), held[i].GetHeight());
        float tall = NetPile.Lay(sizes, laid);
        // Undoing the body's mirror, so the pile swings the world's way.
        float swing = Math.Clamp((float)f.Hover.Swing, -0.45f, 0.45f) * f.Hover.Face;
        var pivot = new Vector2(SI.HoldX - SI.AnchorX, SI.HoldBottom - tall - SI.AnchorY);
        _flying.DrawSetTransformMatrix(body * new Transform2D(swing, pivot));
        for (int i = 0; i < held.Length; i++)
            _flying.DrawTextureRect(held[i], new Rect2(laid[i].X, laid[i].Y + tall, laid[i].W, laid[i].H), false);
        _flying.DrawSetTransformMatrix(body);
        if (pose == GripPose.Shut) _flying.DrawTextureRect(PixelArt.Lit("salvage_front"), SalvageFly, false);
    }

    /// <summary>Each drone's eye and lamp over it, unlit: the lamp sky while it works (a drone's while its rotors run), amber, a salvage drone's red, blinking while it flies home low.</summary>
    private void DrawEyes()
    {
        foreach (var f in _fliers.Values)
        {
            if (f.Down) continue;
            _eyes.DrawSetTransformMatrix(BodyOf(f));
            if (f.Salvage)
            {
                _eyes.DrawTextureRect(PixelArt.Picture("salvage_eye"), SalvageFly, false);
                if (!f.Low || Blink()) _eyes.DrawTextureRect(PixelArt.Picture(f.Low ? "salvage_lamp_low" : "salvage_lamp_work"), SalvageFly, false);
                continue;
            }
            _eyes.DrawTextureRect(PixelArt.Picture("drone_eye"), FlyRect, false);
            if (f.Low ? Blink() : f.Hover.Spin > 0.5)
                _eyes.DrawTextureRect(PixelArt.Picture(f.Low ? "drone_lamp_low" : "drone_lamp_work"), FlyRect, false);
        }
        _eyes.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }
}
