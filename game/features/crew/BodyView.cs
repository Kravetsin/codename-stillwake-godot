using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Rooms;
using FloorSpec = Stillwake.Content.Floor;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// What each hero left where it died (0060), drawn on its stacks (<see cref="Bodies"/>): its figure lying limp in
/// its suit, on its back aboard and afloat alike, and a beacon on its chest blinking in the suit's
/// colour, lighting what is round it (a <see cref="Glow"/>: glowing things give light). The hero's own sprites
/// stand in for a picture of its own till one is picked. The newest body of a hero still passed out is not drawn:
/// that hero lies there itself.
/// </summary>
public partial class BodyView : Node2D
{
    /// <summary>The beacon blinks once in this many seconds, on for the first part of it.</summary>
    private const double BlinkS = 1.2, OnS = 0.22;
    /// <summary>How much of the suit's colour the beacon's light gives while on.</summary>
    private const float BeaconLight = 0.85f;
    /// <summary>Where the beacon sits above the point the body lies on: atop the chest of a figure on its back, in cells.</summary>
    private const float BeaconUp = 0.9f;

    private sealed class Shown(Figure figure, Glow glow)
    {
        public readonly Figure Figure = figure;
        public readonly Glow Glow = glow;
        public bool Seen;
    }

    private readonly Dictionary<(int Peer, int Died), Shown> _shown = new();
    private readonly List<Pile> _piles = [];
    private readonly List<(int Peer, int Died)> _gone = [];
    /// <summary>Each beacon drawn this frame: where, its suit's colour, how bright.</summary>
    private readonly List<(Vector2 At, Color Colour, float On)> _beacons = [];
    private double _clock;

    public BodyView()
    {
        // The beacons are drawn here, unlit over everything lying on the floor; the figures under the stacks.
        ZIndex = Layers.Glows;
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
    }

    /// <summary>
    /// Every body of the world <paramref name="m"/> drawn where its stacks lie, carried on between ticks by
    /// <paramref name="progress"/> as the floor's stacks are; its beacon blinking on by <paramref name="elapsedMs"/>.
    /// </summary>
    public void Frame(Registry m, double progress, double elapsedMs)
    {
        _clock += elapsedMs / 1000;
        var rooms = m.Rooms;
        Bodies.Of(m.Items.Floor, _piles, s =>
        {
            bool space = rooms.RoomAt(s.Cell) == RoomsModule.Space;
            int drift = space ? FloorSpec.SpaceDrift : 0;
            return (s.X + (s.Vx - drift) * progress, s.Y + (space || s.Vy != 0 ? s.Vy * progress : 0));
        });
        foreach (var shown in _shown.Values) shown.Seen = false;
        _beacons.Clear();
        for (int i = 0; i < _piles.Count; i++)
        {
            var pile = _piles[i];
            // Its hero lies there itself till it wakes: the newest body of one still out.
            if ((m.Crew.HeroOf(pile.Peer)?.Out ?? 0) > 0 && Newest(i)) continue;
            var key = (pile.Peer, pile.Died);
            bool made = !_shown.TryGetValue(key, out var shown);
            if (made)
            {
                shown = _shown[key] = new Shown(new Figure(Suits.Of(m.Crew, pile.Peer), own: false) { ZIndex = Layers.Bodies, ZAsRelative = false }, new Glow());
                AddChild(shown.Figure);
                AddChild(shown.Glow);
            }
            shown!.Seen = true;
            int colour = Suits.Of(m.Crew, pile.Peer);
            var hero = new Hero((int)Math.Round(pile.X), (int)Math.Round(pile.Y), 0, 0, false, pile.Peer % 2 == 0 ? 1 : -1);
            shown.Figure.Wear(colour);
            // On its back aboard and afloat alike, found already lying: long past the fall.
            shown.Figure.Show(hero, Pose.Lie, 0, false, made ? Poses.Faint.Ms * 2 : elapsedMs);
            var beacon = new Vector2((float)(pile.X / Hero.Unit), (float)(pile.Y / Hero.Unit) - BeaconUp) * ShipCamera.Cell;
            float on = Blink(pile.Peer);
            shown.Glow.Position = beacon;
            shown.Glow.Visible = on > 0;
            var light = new Color((uint)colour << 8 | 0xff);
            shown.Glow.Colour = light * (BeaconLight * on);
            _beacons.Add((beacon, light, on));
        }
        _gone.Clear();
        foreach (var (key, shown) in _shown)
            if (!shown.Seen) _gone.Add(key);
        foreach (var key in _gone)
        {
            _shown[key].Figure.QueueFree();
            _shown[key].Glow.QueueFree();
            _shown.Remove(key);
        }
        QueueRedraw();
    }

    /// <summary>Whether pile <paramref name="i"/> is its peer's newest: the piles run oldest first.</summary>
    private bool Newest(int i)
    {
        for (int j = i + 1; j < _piles.Count; j++)
            if (_piles[j].Peer == _piles[i].Peer) return false;
        return true;
    }

    /// <summary>How bright a peer's beacon is now: a short flash fading out, then dark, each peer's out of step with the others'.</summary>
    private float Blink(int peer)
    {
        double t = (_clock + peer * 0.37) % BlinkS;
        return t < OnS ? (float)(1 - 0.5 * t / OnS) : 0;
    }

    /// <summary>The beacons' lamps in a dark housing: dim while off; lit, a white core in the suit's colour and its halo.</summary>
    public override void _Draw()
    {
        foreach (var (at, colour, on) in _beacons)
        {
            if (on > 0)
            {
                DrawCircle(at, 9f, colour with { A = 0.22f * on });
                DrawCircle(at, 4.5f, colour with { A = 0.5f * on });
            }
            DrawCircle(at, 2.5f, Style.Slate950);
            DrawCircle(at, 1.6f, on > 0 ? Colors.White.Lerp(colour, 0.3f) : colour.Darkened(0.6f));
        }
    }
}
