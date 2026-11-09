using System;
using System.Collections.Generic;
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
/// no tracker on its shin, flying on its jetpack's plume between the ticks' places, its gun aimed at whom it acts on. Struck it flinches,
/// sparks and is heard; its cuts through rock heard as the rock it bit or broke; killed in view it tumbles down and fades.
/// </summary>
public partial class HunterView : Node2D
{
    private const double FadeMs = 1400, Gravity = 600;
    private const int Unit = CreaturesModule.Unit;

    private sealed class Shown(Figure figure, JetPlume plume)
    {
        public readonly Figure Figure = figure;
        public readonly JetPlume Plume = plume;
        public readonly CutHeard Cuts = new();
        public int Tick = -1, FromX, FromY, ToX, ToY, Damage, Facing = 1, LastWear;
        public string Species = "";
        public Hero Drawn;
    }

    private sealed record Falling(Figure Figure, JetPlume Plume, Hero From)
    {
        public double Ms, Vy;
        public double Y;
    }

    private readonly Dictionary<int, Shown> _shown = [];
    private readonly List<int> _gone = [];
    private readonly HashSet<int> _seen = [];
    private readonly List<Falling> _falling = [];
    private readonly Voices _world = new(S.World, voices: 4), _cuts = new(S.World, voices: 4);

    public HunterView()
    {
        ZIndex = Layers.OtherHeroes;
        AddChild(_world);
        AddChild(_cuts);
    }

    /// <summary>
    /// Shows every hero-looked creature of <paramref name="m"/> at <paramref name="progress"/> through its tick, aiming at the heroes
    /// where <paramref name="heroOf"/> draws them; one gone while <paramref name="view"/> held it was killed.
    /// </summary>
    public void Frame(Registry m, int tick, double progress, double elapsedMs, Func<int, Hero?> heroOf, Rect2 view)
    {
        _seen.Clear();
        foreach (var c in m.Creatures.Creatures)
        {
            var spec = Spec.Creatures.Get(c.Species);
            if (!CreatureLooks.Heroes.TryGetValue(spec.Look, out var look)) continue;
            _seen.Add(c.Id);
            if (!_shown.TryGetValue(c.Id, out var one))
            {
                var figure = new Figure(look.Colour, own: false) { Tracked = false, Jetpack = true, Wields = look.Gun };
                figure.Wear(look.Colour, look.Helmet, look.Body);
                var plume = new JetPlume();
                AddChild(plume);
                AddChild(figure);
                _shown[c.Id] = one = new Shown(figure, plume) { Species = c.Species, FromX = c.X, FromY = c.Y, ToX = c.X, ToY = c.Y, Damage = c.Damage, LastWear = CreaturesModule.LastWear(c) };
            }
            if (one.Tick != tick) (one.Tick, one.FromX, one.FromY, one.ToX, one.ToY) = (tick, one.ToX, one.ToY, c.X, c.Y);
            Show(m, one, c, spec, progress, elapsedMs, heroOf);
        }
        _gone.Clear();
        foreach (int id in _shown.Keys)
            if (!_seen.Contains(id)) _gone.Add(id);
        foreach (int id in _gone)
        {
            var one = _shown[id];
            _shown.Remove(id);
            var feet = At(one.Drawn.X, one.Drawn.Y);
            // Nothing is taken away where a hero sees it: gone in view, it was killed.
            if (view.HasPoint(feet)) Die(one);
            else
            {
                one.Figure.QueueFree();
                one.Plume.QueueFree();
            }
        }
        Fall(elapsedMs);
    }

    private void Show(Registry m, Shown one, Creature c, Spec.Creatures.Species spec, double progress, double elapsedMs, Func<int, Hero?> heroOf)
    {
        int x = one.FromX + (int)Math.Round((one.ToX - one.FromX) * progress), y = one.FromY + (int)Math.Round((one.ToY - one.FromY) * progress);
        var target = c.Target is { Kind: RefKind.Hero, Id: var peer } ? heroOf(peer) : null;
        var move = c.Act is { } act ? spec.Moves[act.Move] : null;
        // Facing whom it acts on, else the way it flies, else as it was.
        if (move != null && target is { } t) one.Facing = t.X >= x ? 1 : -1;
        else if (c.VX != 0) one.Facing = Math.Sign(c.VX);
        // Its speed in the controller's steps, three to a tick; hovering it is drawn rising a little, legs hanging, never stood on air.
        int vx = c.VX / 3, vy = c.VY / 3;
        one.Drawn = new Hero(x, y, vx, Math.Min(vy, -1), false, one.Facing);
        var figure = one.Figure;
        figure.Aim = move is Spec.Creatures.Shoot or Spec.Creatures.Pounce && target is { } aimed
            ? At(aimed.X, aimed.Y - Hero.MiddleUp)
            : null;
        if (c.Damage > one.Damage) Struck(one);
        one.Damage = c.Damage;
        figure.Show(one.Drawn, Pose.Air, 0, false, elapsedMs);
        one.Plume.Step(figure.Nozzle, (vx, vy), elapsedMs);
        // Its cuts heard where they bit, the rock's own sound; nothing is sent, it is the sim's wear.
        int wore = CreaturesModule.LastWear(c);
        var grid = m.Grid;
        var box = spec.Body;
        int left = (int)Math.Floor((double)c.X / Unit - box.Width / 2.0), top = (int)Math.Floor((double)c.Y / Unit) - box.Height;
        var heard = one.Cuts.Look(left, top, box.Width, box.Height, grid.Width, grid.Height, cell => grid.WallAt(cell), wore != one.LastWear);
        one.LastWear = wore;
        if (heard is var (cell, material, broke) && (broke ? GroundSounds.BreakOf(material) : GroundSounds.HitOf(material)) is { } cue)
            _cuts.Play(cue, new Vector2((float)((cell % grid.Width + 0.5) * Rig.Cell), (float)((cell / grid.Width + 0.5) * Rig.Cell)));
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
        _falling.Add(new Falling(one.Figure, one.Plume, one.Drawn) { Y = one.Drawn.Y });
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
            f.Figure.QueueFree();
            f.Plume.QueueFree();
            _falling.RemoveAt(i);
        }
    }

    private static Vector2 Chest(Hero hero) => At(hero.X, hero.Y - Hero.MiddleUp);

    /// <summary>A place in thousandths of a cell, in the world's pixels.</summary>
    private static Vector2 At(int x, int y) => new((float)((double)x / Hero.Unit * Rig.Cell), (float)((double)y / Hero.Unit * Rig.Cell));
}
