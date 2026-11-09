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
/// (catcher-combat, <see cref="CatcherFx"/>): its aim a red thread, its bolt red from its muzzle, its blink a red glitch with its ghost
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
        public int Tick = -1, FromX, FromY, ToX, ToY, Damage, Facing = 1, Blinked, Held = -1, Ghost = -1;
        public string Species = "";
        public CreatureLooks.HeroGear Gear = null!;
        public Spec.Creatures.Hunt? Hunt;
        public Hero Drawn;
        /// <summary>Its gun's muzzle as drawn this frame, in pixels of the world: where its bolt leaves.</summary>
        public Vector2 Muzzle;
        /// <summary>The clock its tell began, its last bolt struck and it last blinked; where it struck and where it blinked from.</summary>
        public double TellMs, BoltMs = double.MinValue / 2, BlinkMs = double.MinValue / 2;
        public Vector2 BoltTo, BlinkFrom;
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
    /// <summary>The catchers' arcs drawn as bolts, by id, forgotten once they are gone.</summary>
    private readonly HashSet<int> _bolts = [];
    private readonly CatcherFxView _fx = new();
    private double _clockMs;
    private int _boltsTick = -1;

    /// <summary>The receiver's colour while it hunts no one: a dull grey.</summary>
    private const int Unclaimed = 0x8a8a8a;
    private readonly Voices _world = new(S.World, voices: 4), _fight = new(S.World, voices: 4);

    /// <summary>The sparks its bolt throws where it strikes: the sentry's arc's, red.</summary>
    private static readonly Luminous Spark = Luminous.HitSpark with
    {
        Ramp = [(255, 225, 219), (255, 120, 100), (255, 90, 70), (190, 40, 40)],
        LifeMs = (200, 450),
        Halo = 2,
    };

    private static readonly (int R, int G, int B) BoltLight = (255, 90, 70);

    public HunterView()
    {
        ZIndex = Layers.OtherHeroes;
        AddChild(_world);
        AddChild(_fight);
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
                    FromX = c.X,
                    FromY = c.Y,
                    ToX = c.X,
                    ToY = c.Y,
                    Damage = c.Damage,
                    Blinked = fight.Blinked,
                    Hunt = spec.Brain.OfType<Spec.Creatures.Hunt>().FirstOrDefault(),
                };
            }
            if (one.Tick != tick) (one.Tick, one.FromX, one.FromY, one.ToX, one.ToY) = (tick, one.ToX, one.ToY, c.X, c.Y);
            // A blink is a jump: drawn where it landed at once, never slid across the ground between.
            if (fight.Blinked != one.Blinked)
            {
                (one.Blinked, one.FromX, one.FromY) = (fight.Blinked, c.X, c.Y);
                (one.BlinkMs, one.BlinkFrom) = (_clockMs, At(one.Drawn.X, one.Drawn.Y));
                _fight.Play(S.CatcherBlink, Chest(one.Drawn));
            }
            Show(m, one, c, spec, fight, progress, elapsedMs, heroOf);
        }
        Bolts(m, tick);
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
        int x = one.FromX + (int)Math.Round((one.ToX - one.FromX) * progress), y = one.FromY + (int)Math.Round((one.ToY - one.FromY) * progress);
        var target = c.Target is { Kind: RefKind.Hero, Id: var peer } ? heroOf(peer) : null;
        var move = c.Act is { } act ? spec.Moves[act.Move] : null;
        // Facing whom it acts on, else the way it flies, else as it was.
        if (move != null && target is { } t) one.Facing = t.X >= x ? 1 : -1;
        else if (c.VX != 0) one.Facing = Math.Sign(c.VX);
        // Its speed in the controller's steps, three to a tick; hovering it is drawn rising a little, legs hanging, never stood on air.
        int vx = c.VX / 3, vy = c.VY / 3;
        // On ground it runs as a hero does, its jetpack off (catcher-paths).
        bool afoot = m.Creatures.Afoot(c);
        // Stepping straight up a step it still strides, as fast as it rises.
        int stride = afoot && vx == 0 && vy != 0 ? one.Facing * Math.Abs(vy) : vx;
        one.Drawn = new Hero(x, y, afoot ? stride : vx, afoot ? 0 : Math.Min(vy, -1), false, one.Facing);
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
        // Its gun on whom it acts on, on its quarry while it aims on the run, and held on the cell its shot will strike in the tell (catcher-combat).
        Vector2? held = fight.Held >= 0 ? Middle(fight.Held, m.Grid.Width) : null;
        bool aims = move is Spec.Creatures.Shoot or Spec.Creatures.Pounce || fight.Aims;
        figure.Aim = held ?? (aims && target is { } aimed ? Chest(aimed) : null);
        if (c.Damage > one.Damage) Struck(one);
        one.Damage = c.Damage;
        figure.Show(one.Drawn, afoot ? Poses.PoseOf(one.Drawn, false, false) : Pose.Air, 0, false, elapsedMs);
        if (afoot) one.Plume.Step(null, null, elapsedMs);
        else one.Plume.Step(figure.Nozzle, (vx, vy), elapsedMs);
        one.Muzzle = figure.Muzzle ?? Chest(one.Drawn);
        Aim(one, held, fight.Aims && target is { } quarryAt ? Chest(quarryAt) : null);
        Blink(m, one, fight, progress, target, elapsedMs);
        foreach (var mark in m.Creatures.MarksOf(c))
            _fx.Pillar(new Vector2((float)((mark.Cell % m.Grid.Width + 0.5) * Rig.Cell), (float)((mark.Cell / m.Grid.Width + 1) * Rig.Cell)),
                Math.Clamp((mark.Flight - mark.Left + progress) / mark.Flight, 0, 1));
    }

    /// <summary>
    /// Its aim «Красная нить»: in the tell a still line brightening to the cell it holds, else, aiming, a dim one wobbling on its quarry,
    /// back a while after a bolt; its last bolt's faint ghost.
    /// </summary>
    private void Aim(Shown one, Vector2? held, Vector2? quarry)
    {
        double since = _clockMs - one.BoltMs;
        if (since >= CatcherFx.BoltMs && since < CatcherFx.BoltMs + CatcherFx.AfterMs)
            _fx.After(one.Muzzle, one.BoltTo, (since - CatcherFx.BoltMs) / CatcherFx.AfterMs);
        if ((held ?? quarry) is not { } end) return;
        double tellMs = (one.Hunt?.Gunfire.Tell ?? 1) * TickMs;
        var thread = CatcherFx.AimOf(held != null ? (_clockMs - one.TellMs) / tellMs : null, CatcherFx.BackOf(since), _clockMs);
        if (thread.Alpha <= 0.004) return;
        var (wx, wy) = CatcherFx.Wobble(_clockMs, thread.Sway);
        _fx.Aim(one.Muzzle, end + new Vector2((float)wx, (float)wy), thread);
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
    /// Each arc a catcher drawn here struck, new since last seen: a red bolt from its gun's muzzle as drawn, following it while it burns,
    /// to where it struck; a red flash at both ends, red sparks and a glint where it struck; heard where it left and where it struck.
    /// </summary>
    private void Bolts(Registry m, int tick)
    {
        var arcs = m.Creatures.Arcs;
        if (tick != _boltsTick)
        {
            _boltsTick = tick;
            _bolts.RemoveWhere(id => !Lingers(arcs, id));
        }
        float k = (float)(Rig.Cell / Unit);
        foreach (var arc in arcs)
        {
            if (!_shown.TryGetValue(arc.By, out var one) || !_bolts.Add(arc.Id)) continue;
            var to = new Vector2(arc.X1, arc.Y1) * k;
            (one.BoltMs, one.BoltTo) = (_clockMs, to);
            Chain(one, to);
            var stage = Stages.Of(Layers.Effects);
            stage.Flares.Flash(to.X, to.Y, BoltLight, 1.0 * Fx.Glow, 10, CreatureLooks.LightKeep, 0.8);
            stage.Flares.Flash(one.Muzzle.X, one.Muzzle.Y, BoltLight, 0.6 * Fx.Glow, 6, CreatureLooks.LightKeep, 0.8);
            stage.Flares.Glint(to.X, to.Y, 5, (255, 236, 230));
            for (int i = 0; i < 10; i++)
            {
                // Thrown up and out, as the page's sparks: off the ground they fall.
                double a = Between(-Math.PI, 0), s = Between(30, 100);
                stage.Glims.Add(Spark, to.X, to.Y, Math.Cos(a) * s, Math.Sin(a) * s);
            }
            _fight.Play(S.ArcStruck, one.Muzzle);
            _fight.Play(S.ArcHit, to);
        }
    }

    /// <summary>The longest link of a bolt's chain, in pixels of the world: one long arc's folds would stray by a share of its length.</summary>
    private const float Link = 40;

    /// <summary>A bolt as a chain of short arcs from <paramref name="one"/>'s muzzle to <paramref name="to"/>, their joints a little off the line, carried along as its muzzle moves.</summary>
    private static void Chain(Shown one, Vector2 to)
    {
        int links = Math.Max(1, (int)Math.Ceiling((to - one.Muzzle).Length() / Link));
        var off = new float[links + 1];
        for (int i = 1; i < links; i++) off[i] = (float)Between(-3, 3);
        Vector2 Joint(int i)
        {
            var way = to - one.Muzzle;
            var across = way.Length() > 0 ? new Vector2(-way.Y, way.X).Normalized() : Vector2.Zero;
            return i == 0 ? one.Muzzle : i == links ? to : one.Muzzle + way * i / links + across * off[i];
        }
        for (int i = 1; i <= links; i++)
        {
            int a = i - 1, b = i;
            Stages.Of(Layers.Effects).Bolts.Add(() =>
            {
                var (p, q) = (Joint(a), Joint(b));
                return (p.X, p.Y, q.X, q.Y, 0);
            }, 9, 1.2, 1, branchy: true, tint: BoltTint.Red);
        }
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
