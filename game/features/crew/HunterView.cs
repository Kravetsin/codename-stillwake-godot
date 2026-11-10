using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Creatures;
using S = Stillwake.Game.Sound;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The creatures whose look is a hero's (<see cref="CreatureLooks.Heroes"/>, tracker-catchers): each a <see cref="Figure"/> in its gear,
/// no tracker on its shin, flying on its jetpack's plume between the ticks' places, its gun aimed at whom it acts on; one
/// of its look's outfits by its id, its receiver blinking in its quarry's colour, its goggles lit as it hunts. Struck it flinches,
/// sparks and is heard; a blink drawn as a jump, never a slide; killed in view it tumbles down and fades. Its fight as the user picked it
/// (catcher-combat, <see cref="CatcherFx"/>): its aim a red thread, its laser «Жгут» from its muzzle, its blink a red glitch with its ghost
/// ahead, its salvo's marks columns of light; heard as its tell, gather and blink begin.
/// </summary>
public partial class HunterView : Node2D
{
    private const double FadeMs = 1400, Gravity = 600, TickMs = 1000.0 / World.TicksPerSecond;
    private const int Unit = CreaturesModule.Unit;

    private sealed class Shown(Figure figure, JetPlume plume, Glitch group)
    {
        public readonly Figure Figure = figure;
        public readonly JetPlume Plume = plume;
        public readonly Glitch Group = group;
        public int Tick = -1, BeforeX, BeforeY, FromX, FromY, ToX, ToY, Damage, Facing = 1, Blinked, Held = -1, Ghost = -1;
        /// <summary>Its missile's tell's ticks left last frame; the clock its lock last snapped shut.</summary>
        public int LaunchLeft;
        public double SnapMs = double.MinValue / 2;
        /// <summary>Its speed last tick and how much it changed (catcher-motion): its plume and its lean.</summary>
        public int VX, VY, DVX, DVY;
        /// <summary>Its gun's fine heading last tick and this, the sim's (catcher-motion; -1 for none): drawn turning between them.</summary>
        public int GunFrom = -1, GunTo = -1;
        /// <summary>Its gun as drawn, radians (none before it first aimed); the clock its charge left its hand.</summary>
        public double? AimAngle, ThrewMs;
        public bool Throwing;
        public string Species = "";
        public CreatureLooks.HeroGear Gear = null!;
        public Spec.Creatures.Hunt? Hunt;
        public Hero Drawn;
        /// <summary>Its gun's muzzle as drawn this frame, in pixels of the world: where its laser leaves.</summary>
        public Vector2 Muzzle;
        /// <summary>The clock its tell began, its last laser struck and it last blinked; where it ended and where it blinked from.</summary>
        public double TellMs, ShotMs = double.MinValue / 2, BlinkMs = double.MinValue / 2;
        public Vector2 ShotTo, ShotFrom, BlinkFrom;

        /// <summary>Blinked since its last shot: the beam still burning stays where it was fired from (catcher-motion).</summary>
        public bool Loose;
        /// <summary>How far its laser runs, in cells (its <c>Shoot.Through</c>); whether its last stopped on solid, and melts it.</summary>
        public int Through;
        public bool Melts;
        /// <summary>The drips and puffs of smoke its last laser's melt has thrown; the keys of the lights along its laser.</summary>
        public int Drips, Puffs;
        public readonly object[] Lamps = [new(), new(), new(), new(), new(), new()];
        /// <summary>Its ghost where it will be while it gathers, made the first time.</summary>
        public (Figure Figure, Glitch Group)? Ghosted;
    }

    private sealed record Falling(Figure Figure, JetPlume Plume, Hero From, Node Holder)
    {
        public double Ms, Vy;
        public double Y;
    }

    private readonly Dictionary<int, Shown> _shown = [];
    private readonly List<int> _gone = [];
    private readonly HashSet<int> _seen = [];
    private readonly List<Falling> _falling = [];
    /// <summary>The catchers' arcs drawn as lasers, by id, forgotten once they are gone.</summary>
    private readonly HashSet<int> _shots = [];
    private readonly CatcherFxView _fx = new();
    private double _clockMs;
    private int _shotsTick = -1;

    /// <summary>The receiver's colour while it hunts no one: a dull grey.</summary>
    private const int Unclaimed = 0x8a8a8a;
    private readonly Voices _world = new(S.World, voices: 4), _fight = new(S.World, voices: 4);
    // Its missile is a fight in space, where the World bus is silent: its tell heard on the Suit bus, as the sentry's arc is.
    private readonly Voices _suit = new(S.Suit, voices: 2);

    /// <summary>A drop of rock its laser melts, falling off where it stops.</summary>
    private static readonly Luminous Drip = new(Glimmer.Mote, [(255, 190, 110), CatcherFx.Drip, (190, 60, 25)], (220, 330))
    { Fade = 0.8, Gravity = 600, Size = 0.45, Bounce = 0 };

    /// <summary>Smoke off the spot its laser melted, rising as it cools.</summary>
    private static readonly Luminous Smoke = new(Glimmer.Wisp, [CatcherFx.Smoke], (800, 1000))
    { Matter = true, Drag = 0.6, Turbulence = 14, Size = 1.4, Grow = 2.9, Alpha = 0.3 };

    public HunterView()
    {
        ZIndex = Layers.OtherHeroes;
        AddChild(_world);
        AddChild(_fight);
        AddChild(_suit);
        AddChild(_fx);
    }

    /// <summary>
    /// Shows every hero-looked creature of <paramref name="m"/> at <paramref name="progress"/> through its tick, aiming at the heroes
    /// where <paramref name="heroOf"/> draws them; one gone while <paramref name="view"/> held it was killed.
    /// </summary>
    public void Frame(Registry m, int tick, double progress, double elapsedMs, Func<int, Hero?> heroOf, Rect2 view)
    {
        _clockMs += elapsedMs;
        _seen.Clear();
        _fx.Begin();
        Shots(m, tick);
        foreach (var c in m.Creatures.Creatures)
        {
            var spec = Spec.Creatures.Get(c.Species);
            if (!CreatureLooks.Heroes.TryGetValue(spec.Look, out var look)) continue;
            _seen.Add(c.Id);
            var fight = m.Creatures.FightOf(c);
            if (!_shown.TryGetValue(c.Id, out var one))
            {
                var figure = new Figure(Unclaimed, own: false) { Tracked = false, Jetpack = true, Wields = look.Gun };
                var plume = new JetPlume();
                var group = new Glitch();
                group.AddChild(figure);
                AddChild(plume);
                AddChild(group);
                _shown[c.Id] = one = new Shown(figure, plume, group)
                {
                    Gear = look.Of(c.Id),
                    Species = c.Species,
                    BeforeX = c.X,
                    BeforeY = c.Y,
                    VX = c.VX,
                    VY = c.VY,
                    FromX = c.X,
                    FromY = c.Y,
                    ToX = c.X,
                    ToY = c.Y,
                    Damage = c.Damage,
                    Blinked = fight.Blinked,
                    GunFrom = fight.Gun,
                    GunTo = fight.Gun,
                    Hunt = spec.Brain.OfType<Spec.Creatures.Hunt>().FirstOrDefault(),
                    Through = spec.Moves.OfType<Spec.Creatures.Shoot>().FirstOrDefault()?.Through ?? 0,
                };
            }
            if (one.Tick != tick)
            {
                (one.Tick, one.BeforeX, one.BeforeY, one.FromX, one.FromY, one.ToX, one.ToY) = (tick, one.FromX, one.FromY, one.ToX, one.ToY, c.X, c.Y);
                (one.DVX, one.DVY, one.VX, one.VY) = (c.VX - one.VX, c.VY - one.VY, c.VX, c.VY);
                (one.GunFrom, one.GunTo) = (one.GunTo, fight.Gun);
            }
            // A blink is a jump: drawn where it landed at once, never slid across the ground between.
            if (fight.Blinked != one.Blinked)
            {
                (one.Blinked, one.BeforeX, one.BeforeY, one.FromX, one.FromY) = (fight.Blinked, c.X, c.Y, c.X, c.Y);
                // Its gun too: set on its quarry as it lands, never swept round across the jump.
                (one.GunFrom, one.GunTo, one.AimAngle, one.Loose) = (fight.Gun, fight.Gun, null, true);
                (one.BlinkMs, one.BlinkFrom) = (_clockMs, At(one.Drawn.X, one.Drawn.Y));
                _fight.Play(S.CatcherBlink, Chest(one.Drawn));
            }
            Show(m, one, c, spec, fight, progress, elapsedMs, heroOf);
        }
        _fx.End();
        _gone.Clear();
        foreach (int id in _shown.Keys)
            if (!_seen.Contains(id)) _gone.Add(id);
        foreach (int id in _gone)
        {
            var one = _shown[id];
            _shown.Remove(id);
            one.Ghosted?.Group.QueueFree();
            var feet = At(one.Drawn.X, one.Drawn.Y);
            // Nothing is taken away where a hero sees it: gone in view, it was killed.
            if (view.HasPoint(feet)) Die(one);
            else
            {
                one.Group.QueueFree();
                one.Plume.QueueFree();
            }
        }
        Fall(elapsedMs);
    }

    private void Show(Registry m, Shown one, Creature c, Spec.Creatures.Species spec, CreaturesModule.Fight fight, double progress, double elapsedMs,
        Func<int, Hero?> heroOf)
    {
        // On ground it runs as a hero does, its jetpack off (catcher-paths); in the air along a curve through the ticks' places (catcher-motion).
        bool afoot = m.Creatures.Afoot(c), afloat = !afoot && m.Rules.Floats;
        int x = afoot ? one.FromX + (int)Math.Round((one.ToX - one.FromX) * progress) : (int)Math.Round(CatcherMotion.Along(one.BeforeX, one.FromX, one.ToX, progress));
        int y = afoot ? one.FromY + (int)Math.Round((one.ToY - one.FromY) * progress) : (int)Math.Round(CatcherMotion.Along(one.BeforeY, one.FromY, one.ToY, progress));
        var target = c.Target is { Kind: RefKind.Hero, Id: var peer } ? heroOf(peer) : null;
        var move = c.Act is { } act ? spec.Moves[act.Move] : null;
        // Its gun raised on whom it acts on, on its quarry while it aims on the run, held on the cell its shot will strike in the tell (catcher-combat).
        Vector2? held = fight.Held >= 0 ? Middle(fight.Held, m.Grid.Width) : null;
        // Along its laser while it burns and rests: played, a gun turned back on its quarry left the laser running off from it.
        Vector2? fired = !one.Loose && _clockMs - one.ShotMs < CatcherFx.BeamMs + CatcherFx.RestMs ? one.ShotTo : null;
        Vector2? quarryAt = fight.Aims && target is { } sighted ? Chest(sighted) : null;
        var acted = move is Spec.Creatures.Shoot or Spec.Creatures.Pounce ? target : null;
        // Pointed where the sim turned it, between the ticks' headings (catcher-motion: played, its beam left where its gun did not
        // point); on whom it acts on; along its laser. Its drawn gun chases a change of these, never jumps.
        var chest = Chest(new Hero(x, y, 0, 0, false, one.Facing));
        double? wants = acted is { } a ? Angle(chest, Chest(a)) : fired is { } f ? Angle(chest, f)
            : one.GunTo >= 0 ? CatcherMotion.GunAngle(one.GunFrom, one.GunTo, progress) : null;
        if (wants is double w) one.AimAngle = one.AimAngle is double was ? CatcherMotion.Chase(was, w, elapsedMs) : w;
        bool raised = acted != null || (held ?? fired ?? quarryAt) != null;
        // Facing whom it acts on, or afloat its raised gun, turning round only past upright; else the way it flies, past a dead band afloat.
        if (move != null && target is { } t) one.Facing = At(t.X, t.Y).X >= At(x, y).X ? 1 : -1;
        else if (afloat && raised && one.AimAngle is double aim) one.Facing = CatcherMotion.FacingOf(aim, one.Facing);
        else if (Math.Abs(c.VX) > (afloat ? CatcherMotion.FaceBand : 0)) one.Facing = Math.Sign(c.VX);
        // Its speed in the controller's steps, three to a tick; hovering on a planet it is drawn rising a little, legs hanging, never stood on air.
        int vx = c.VX / 3, vy = c.VY / 3;
        // Stepping straight up a step it still strides, as fast as it rises.
        int stride = afoot && vx == 0 && vy != 0 ? one.Facing * Math.Abs(vy) : vx;
        one.Drawn = new Hero(x, y, afoot ? stride : vx, afoot ? 0 : afloat ? vy : Math.Min(vy, -1), false, one.Facing);
        var figure = one.Figure;
        // Its receiver in its quarry's colour, blinking as the quarry's bracelet; its goggles lit while it hunts.
        int? quarry = CreaturesModule.Hunting(c);
        figure.Wear(quarry is int q ? Suits.Of(m.Players, q) : Unclaimed, one.Gear.Helmet, one.Gear.Body);
        figure.GearLights = (quarry != null ? TrackerBlink.LevelOf(TrackerBlink.Hunted, _clockMs) : 0, quarry != null ? 1 : 0, one.Gear.Eyes);
        // Its tell begun and its gather, heard.
        if (fight.Held >= 0 && one.Held < 0)
        {
            one.TellMs = _clockMs;
            _fight.Play(S.CatcherAim, Chest(one.Drawn));
        }
        if (fight.Ghost >= 0 && one.Ghost < 0) _fight.Play(S.CatcherGather, Chest(one.Drawn));
        (one.Held, one.Ghost) = (fight.Held, fight.Ghost);
        Lock(one, fight, target, progress);
        figure.Aim = raised && one.AimAngle is double pointing
            ? Chest(one.Drawn) + new Vector2((float)Math.Cos(pointing), (float)Math.Sin(pointing)) * (float)AimFar
            : null;
        Throw(one, fight, progress, afloat);
        if (c.Damage > one.Damage) Struck(one);
        one.Damage = c.Damage;
        figure.Show(one.Drawn, afoot ? Poses.PoseOf(one.Drawn, false, false) : afloat ? Pose.Float : Pose.Air, 0, false, elapsedMs);
        // Its plume blown against how it speeds up or brakes, else against its way; long as it thrusts hard, short coasting.
        bool thrusts = one.DVX != 0 || one.DVY != 0;
        one.Plume.Thrust = afloat ? CatcherMotion.Thrust(one.DVX, one.DVY, one.Hunt?.Accel ?? 0) : 1;
        if (afoot) one.Plume.Step(null, null, elapsedMs);
        else one.Plume.Step(figure.Nozzle, afloat && thrusts ? (one.DVX, one.DVY) : (vx, vy), elapsedMs);
        one.Muzzle = figure.Muzzle ?? Chest(one.Drawn);
        // The charge in its hand, its fuse lit, glowing warm.
        if (figure.OffHeldAt is { } lit && figure.OffHand is { Fuse: { } fuse })
            CatcherFxView.Glow(lit, 10, fuse.Light, 0.22 + 0.1 * fuse.Flicker[(int)(_clockMs / 1000 * fuse.Fps) % fuse.Flicker.Length]);
        Aim(m, one, held, quarryAt, target);
        Laser(one);
        Blink(m, one, fight, progress, target, elapsedMs);
        foreach (var mark in m.Creatures.MarksOf(c))
            _fx.Pillar(new Vector2((float)((mark.Cell % m.Grid.Width + 0.5) * Rig.Cell), (float)((mark.Cell / m.Grid.Width + 1) * Rig.Cell)),
                Math.Clamp((mark.Flight - mark.Left + progress) / mark.Flight, 0, 1));
    }

    /// <summary>
    /// Its missile's tell «Захват» (catcher-missile): amber corners closing on its quarry, heard as it begins; snapped shut on it as it
    /// ends, the missile away.
    /// </summary>
    private void Lock(Shown one, CreaturesModule.Fight fight, Hero? quarry, double progress)
    {
        if (fight.LaunchLeft > 0 && one.LaunchLeft == 0) _suit.Play(S.CatcherLock, Chest(one.Drawn));
        if (fight.LaunchLeft == 0 && one.LaunchLeft > 0) one.SnapMs = _clockMs;
        one.LaunchLeft = fight.LaunchLeft;
        if (quarry is not { } locked) return;
        double tell = one.Hunt?.Missile.Tell ?? 1;
        if (fight.LaunchLeft > 0) _fx.Lock(Chest(locked), (tell - fight.LaunchLeft + progress) / tell, _clockMs);
        else _fx.Snap(Chest(locked), _clockMs - one.SnapMs);
    }

    /// <summary>The angle, radians, from <paramref name="from"/> to <paramref name="to"/> as the screen shows it, y down.</summary>
    private static double Angle(Vector2 from, Vector2 to) => Math.Atan2(to.Y - from.Y, to.X - from.X);

    /// <summary>How far off along its aim its gun is pointed at, pixels: far enough that the arm's turn follows the gun's.</summary>
    private const double AimFar = 240;

    /// <summary>
    /// Its charge «С пояса, толчком» (catcher-motion): hung at its belt; in the tell unhooked, its fuse flaring, drawn to its chest and pushed
    /// off ahead as it is thrown, the hand back as it floats a while after; its body leaning into the push, and afloat against its thrust.
    /// </summary>
    private void Throw(Shown one, CreaturesModule.Fight fight, double progress, bool afloat)
    {
        double? throwMs = null;
        if (fight.ThrowLeft > 0 && one.Hunt is { } hunt)
        {
            throwMs = (hunt.Lob.Tell - fight.ThrowLeft + progress) * TickMs;
            (one.Throwing, one.ThrewMs) = (true, null);
        }
        else if (one.Throwing) (one.Throwing, one.ThrewMs) = (false, _clockMs);
        if (throwMs == null && one.ThrewMs is double threw) throwMs = CatcherMotion.ReleaseMs + _clockMs - threw;
        var thrown = CatcherMotion.ThrowOf(throwMs);
        var figure = one.Figure;
        bool lobs = one.Hunt != null;
        figure.Belt = lobs && thrown.OnBelt ? HeldLook.BlastCharge : null;
        figure.OffHand = lobs && (thrown.Share > 0 || thrown.InHand)
            ? (thrown.Arm, thrown.Share, thrown.Hold, thrown.InHand ? HeldLook.BlastCharge : null, thrown.InHand ? ShotLooks.Charge.Picture : null)
            : null;
        figure.Lean = thrown.Lean + (afloat ? CatcherMotion.Leaning(one.DVX, one.Facing, one.Hunt?.Accel ?? 0) : 0);
        if (thrown.Flare > 0)
        {
            var belt = At(one.Drawn.X, one.Drawn.Y) + new Vector2((float)(CatcherMotion.BeltAt.X * one.Facing), (float)CatcherMotion.BeltAt.Y);
            CatcherFxView.Glow(belt, 5, CatcherMotion.Flare, 0.8 * thrown.Flare, CatcherMotion.FlareCore);
        }
    }

    /// <summary>
    /// Its aim «Красная нить» as «Поиск» draws it (catcher-motion): from its muzzle along its gun, marking its quarry where it crosses it, on
    /// past it fading to where its laser would stop; in the tell a brightening line held on the cell it holds, back a while after a shot.
    /// </summary>
    private void Aim(Registry m, Shown one, Vector2? held, Vector2? quarryAt, Hero? quarry)
    {
        double since = _clockMs - one.ShotMs;
        if ((held ?? quarryAt) == null || one.Figure.Aim is not { } pointed) return;
        double tellMs = (one.Hunt?.Gunfire.Tell ?? 1) * TickMs;
        var thread = CatcherFx.AimOf(held != null ? (_clockMs - one.TellMs) / tellMs : null, CatcherFx.BackOf(since), _clockMs);
        if (thread.Alpha <= 0.004) return;
        var muzzle = one.Muzzle;
        var way = (pointed - muzzle).Normalized();
        // Its quarry's place along the line, the line's end wobbling there, turning on the muzzle.
        var chest = quarry is { } hero ? Chest(hero) : held ?? pointed;
        float along = Math.Max((float)Rig.Cell, (chest - muzzle).Dot(way));
        var (wx, wy) = CatcherFx.Wobble(_clockMs, thread.Sway);
        way = (way * along + new Vector2((float)wx, (float)wy)).Normalized();
        // Where its laser would stop: its whole Through, or where it meets solid.
        double reach = one.Through * Rig.Cell, k = Unit / Rig.Cell;
        var (sx, sy) = m.Creatures.BeamStop((int)(muzzle.X * k), (int)(muzzle.Y * k), (long)(way.X * reach * k), (long)(way.Y * reach * k));
        var stop = new Vector2((float)(sx / k), (float)(sy / k));
        float stops = muzzle.DistanceTo(stop);
        var end = muzzle + way * Math.Min(along, stops);
        bool marks = along < stops && quarry != null && (chest - (muzzle + way * along)).Length() < CatcherMotion.Marks;
        _fx.Aim(muzzle, end, stop, marks, stops < reach - 1, thread);
    }

    /// <summary>
    /// Its blink «Красная помеха»: gathering, its figure cut into jittering bands under a red copy, red specks and a red glow round it,
    /// its ghost flickering where it will be; blinked, a tear where it was, a line falling where it came, its bands twitching a while.
    /// </summary>
    private void Blink(Registry m, Shown one, CreaturesModule.Fight fight, double progress, Hero? quarry, double elapsedMs)
    {
        int frame = CatcherFx.FrameOf(_clockMs);
        double spentMs = _clockMs - one.BlinkMs;
        double? spent = spentMs < CatcherFx.TwitchMs ? spentMs : null;
        bool gathers = fight.Ghost >= 0;
        double gather = gathers && one.Hunt is { } hunt ? Math.Clamp((hunt.Blink.Gather - fight.GatherLeft + progress) / hunt.Blink.Gather, 0, 1) : 0;
        var feet = one.Figure.Position;
        var group = one.Group;
        if (gathers || spent != null)
        {
            if (group.Needs(frame))
            {
                CatcherFx.Body(frame, gather, gathers ? null : spent, group.Shift);
                CatcherFx.RedCopy(frame, gather, gathers ? null : spent, group.RedShift, group.RedAlpha);
            }
            group.Cut(feet, CatcherFx.Bands, frame, 1);
        }
        else group.Calm();
        if (gathers)
        {
            _fx.Specks(feet, frame, CatcherFx.SpecksGathering(gather), 0.9);
            CatcherFxView.Glow(feet + new Vector2(0, -1), 13, CatcherFx.Red, 0.18 * gather);
            Ghost(m, one, fight.Ghost, frame, gather, quarry, elapsedMs);
        }
        else if (one.Ghosted is (_, var ghostGroup)) ghostGroup.Visible = false;
        if (spentMs >= (one.Hunt?.Blink.Spent ?? 0) * TickMs) return;
        _fx.Tear(one.BlinkFrom, spentMs);
        if (CatcherFx.SpecksLeft(spentMs) is var left and > 0) _fx.Specks(one.BlinkFrom, frame, left, 1 - spentMs / CatcherFx.SpeckMs);
        _fx.Arrival(feet, spentMs);
        // Spent, now and then a few specks still fly off it.
        double spentFor = (one.Hunt?.Blink.Spent ?? 1) * TickMs;
        if (CatcherFx.Hash((int)Math.Floor(_clockMs / 70)) > 0.55) _fx.Specks(feet, frame, 3, 0.6 * (1 - spentMs / spentFor));
    }

    /// <summary>Its ghost standing on <paramref name="cell"/>'s floor facing its quarry, red, in bands, some gone; specks round it.</summary>
    private void Ghost(Registry m, Shown one, int cell, int frame, double gather, Hero? quarry, double elapsedMs)
    {
        if (one.Ghosted is not (var figure, var group))
        {
            figure = new Figure(Unclaimed, own: false) { Tracked = false, Jetpack = true, Wields = one.Figure.Wields };
            figure.Wear(Unclaimed, one.Gear.Helmet, one.Gear.Body);
            group = new Glitch();
            group.AddChild(figure);
            AddChild(group);
            one.Ghosted = (figure, group);
        }
        int width = m.Grid.Width, x = cell % width * Unit + Unit / 2, y = (cell / width + 1) * Unit;
        int facing = quarry is { } q ? (q.X >= x ? 1 : -1) : one.Facing;
        group.Visible = true;
        figure.Show(new Hero(x, y, 0, 0, false, facing), Pose.Stand, 0, false, elapsedMs);
        if (group.Needs(frame))
        {
            CatcherFx.Ghosting(frame, gather, group.RedShift, group.RedAlpha);
            group.Shift.Clear();
        }
        group.Cut(figure.Position, CatcherFx.GhostBands, frame, 0);
        _fx.Specks(At(x, y), frame, CatcherFx.SpecksGhost(gather), 0.5 * gather);
    }

    /// <summary>
    /// Each arc a catcher drawn here struck, new since last seen: its laser, from its gun's muzzle as drawn to where the arc ends, whether
    /// solid stopped it there; heard where it leaves.
    /// </summary>
    private void Shots(Registry m, int tick)
    {
        var arcs = m.Creatures.Arcs;
        if (tick != _shotsTick)
        {
            _shotsTick = tick;
            _shots.RemoveWhere(id => !Lingers(arcs, id));
        }
        float k = (float)(Rig.Cell / Unit);
        foreach (var arc in arcs)
        {
            if (!_shown.TryGetValue(arc.By, out var one) || !_shots.Add(arc.Id)) continue;
            // Run on its whole Through, it stopped on nothing; short of it, on solid.
            long far = Math.Max(Math.Abs((long)arc.X1 - arc.X0), Math.Abs((long)arc.Y1 - arc.Y0));
            (one.ShotMs, one.ShotTo, one.Melts, one.Drips, one.Puffs) = (_clockMs, new Vector2(arc.X1, arc.Y1) * k, far < (long)one.Through * Unit - 1, 0, 0);
            (one.ShotFrom, one.Loose) = (one.Muzzle, false);
            _fight.Play(S.CatcherLaser, one.Muzzle);
        }
    }

    /// <summary>
    /// Its laser «Жгут» while it burns: drawn from its muzzle as it runs on to where it ended, the ground along it lit; where solid stopped
    /// it, melting, dripping while it burns, then glowing as it cools and smoking.
    /// </summary>
    private void Laser(Shown one)
    {
        double since = _clockMs - one.ShotMs;
        if (since >= CatcherFx.BeamMs + CatcherFx.CoolMs) return;
        var stage = Stages.Of(Layers.Effects);
        double swell = CatcherFx.Swell(since);
        var from = one.Loose ? one.ShotFrom : one.Muzzle;
        if (swell > 0)
        {
            _fx.Beam(from, one.ShotTo, swell, _clockMs, one.Melts);
            for (int i = 0; i < one.Lamps.Length; i++)
            {
                var at = from.Lerp(one.ShotTo, (i + 0.5f) / one.Lamps.Length);
                stage.Flares.Hold(one.Lamps[i], at.X, at.Y, CatcherFx.BeamMuzzle, 0.35 * swell * Fx.Glow, 4, CreatureLooks.LightKeep, 0.5);
            }
        }
        if (!one.Melts) return;
        _fx.Cooling(one.ShotTo, Math.Max(0, (since - CatcherFx.BeamMs) / CatcherFx.CoolMs));
        for (; one.Drips < Math.Min(since, CatcherFx.BeamMs) / CatcherFx.DripEveryMs; one.Drips++)
            stage.Glims.Add(Drip, one.ShotTo.X, one.ShotTo.Y, Between(-8, 8), Between(0, 20));
        for (; one.Puffs < since / CatcherFx.SmokeEveryMs; one.Puffs++)
            stage.Glims.Add(Smoke, one.ShotTo.X + Between(-1.5, 1.5), one.ShotTo.Y - 2, Between(-4, 4), -20);
    }

    private static bool Lingers(IReadOnlyList<Arc> arcs, int id)
    {
        foreach (var arc in arcs)
            if (arc.Id == id) return true;
        return false;
    }

    /// <summary>A blow on it: it flinches, throws its suit's sparks, and is heard.</summary>
    private void Struck(Shown one)
    {
        var chest = Chest(one.Drawn);
        one.Figure.Flinch(-one.Facing);
        var impact = HitLooks.Creatures.GetValueOrDefault(one.Species, Impact.Alloy);
        Stages.Of(Layers.Effects).Hits.Throw(HitLooks.Of[impact], chest.X, chest.Y, -one.Facing, -0.25, 1);
        if (S.Creatures.TryGetValue(one.Species, out var cues))
        {
            _world.Play(cues.Hit, chest);
            _world.Play(cues.Thump, chest);
        }
    }

    /// <summary>Killed where it was drawn: heard, a last throw of sparks, and its body tumbling down as it fades.</summary>
    private void Die(Shown one)
    {
        var chest = Chest(one.Drawn);
        var impact = HitLooks.Creatures.GetValueOrDefault(one.Species, Impact.Alloy);
        Stages.Of(Layers.Effects).Hits.Throw(HitLooks.Of[impact], chest.X, chest.Y, 0, -1, 1);
        if (S.Creatures.TryGetValue(one.Species, out var cues)) _world.Play(cues.Death, chest);
        one.Figure.Aim = null;
        one.Figure.GearLights = (0, 0, one.Gear.Eyes);
        one.Group.Calm();
        _falling.Add(new Falling(one.Figure, one.Plume, one.Drawn, one.Group) { Y = one.Drawn.Y });
    }

    private void Fall(double elapsedMs)
    {
        for (int i = _falling.Count - 1; i >= 0; i--)
        {
            var f = _falling[i];
            f.Ms += elapsedMs;
            double s = elapsedMs / 1000;
            f.Vy += Gravity * s;
            f.Y += f.Vy * s * Hero.Unit / Rig.Cell;
            var hero = f.From with { Y = (int)f.Y, Vx = 0, Vy = 0 };
            f.Figure.Show(hero, Pose.Tumble, 0, false, elapsedMs);
            f.Figure.Modulate = new Color(1, 1, 1, (float)Math.Max(0, 1 - f.Ms / FadeMs));
            f.Plume.Step(null, null, elapsedMs);
            if (f.Ms < FadeMs) continue;
            f.Holder.QueueFree();
            f.Plume.QueueFree();
            _falling.RemoveAt(i);
        }
    }

    private static Vector2 Chest(Hero hero) => At(hero.X, hero.Y - Hero.MiddleUp);

    /// <summary>The middle of <paramref name="cell"/> of a grid <paramref name="width"/> wide, in pixels of the world.</summary>
    private static Vector2 Middle(int cell, int width) => new((float)((cell % width + 0.5) * Rig.Cell), (float)((cell / width + 0.5) * Rig.Cell));

    /// <summary>A place in thousandths of a cell, in the world's pixels.</summary>
    private static Vector2 At(int x, int y) => new((float)((double)x / Hero.Unit * Rig.Cell), (float)((double)y / Hero.Unit * Rig.Cell));

    private static double Between(double from, double to) => from + (to - from) * Random.Shared.NextDouble();
}
