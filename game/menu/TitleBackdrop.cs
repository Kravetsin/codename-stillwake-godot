using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Render;

namespace Stillwake.Game.Menu;

/// <summary>
/// What the title's world shows beyond the wreck itself (<see cref="TitleScene"/>, 0031): its hero sitting on the edge of
/// the torn deck, slumped, looking out, its head following an astronaut drifting past; debris drifting by, near and far,
/// in the game's own art; now and then an astronaut turning over slowly, its lamp out or still burning; the camera
/// wandering about the hero.
/// </summary>
public partial class TitleBackdrop : Node2D
{
    /// <summary>The debris' looks drifting past, as <c>art/debris</c> names them, and how many of each.</summary>
    private static readonly (string Look, int Count)[] Looks =
        [("hull", 12), ("scrap", 12), ("crate", 8), ("engine", 4), ("hold", 4), ("pods", 4), ("pod", 4), ("tanks", 1), ("section", 1), ("rack", 1), ("container", 1), ("slab", 1)];

    private static readonly string[] Pieces = [.. Looks.SelectMany(l => Enumerable.Range(1, l.Count).Select(n => $"res://art/debris/{l.Look}_{n:00}.png"))];

    private readonly (double X, double Y) _seat;
    private readonly Camera2D _camera;
    private readonly Figure _hero = new(Suits.All[0], own: false) { ZIndex = Layers.Hero, HeadLamp = true };
    private readonly List<(Drifter Path, double From, Node2D Holder, Figure? Figure)> _drifting = [];
    private readonly Random _rng = new();
    private double _seconds, _nextDrifter = TitleScene.FirstDrifter, _nextPiece;

    /// <param name="seat">Where the hero's hips rest, in cells (<see cref="TitleScene.Seat"/>).</param>
    public TitleBackdrop((double X, double Y) seat, Camera2D camera)
    {
        (_seat, _camera) = (seat, camera);
        AddChild(_hero);
    }

    /// <summary>The helmet lamps burning, for the light to draw, as <see cref="HeroView.HeadLamps"/> gives the heroes'.</summary>
    public IReadOnlyList<(Vector2 At, Vector2 Toward, Vector2 Head)> HeadLamps()
    {
        _lamps.Clear();
        _lamps.Add((_hero.HeadLampAt, _hero.HeadLampToward, _hero.HeadAt));
        foreach (var (_, _, _, figure) in _drifting)
            if (figure is { HeadLamp: true }) _lamps.Add((figure.HeadLampAt, figure.HeadLampToward, figure.HeadAt));
        return _lamps;
    }

    private readonly List<(Vector2 At, Vector2 Toward, Vector2 Head)> _lamps = [];

    public override void _Process(double delta)
    {
        _seconds += delta;
        var screen = GetViewportRect().Size / (TitleScene.Zoom * ShipCamera.Cell);
        var (cx, cy) = TitleScene.Camera(_seat, (screen.X, screen.Y), _seconds);
        _camera.Position = new Vector2((float)cx, (float)cy) * ShipCamera.Cell;
        var view = (cx - screen.X / 2, cy - screen.Y / 2, (double)screen.X, (double)screen.Y);
        if (_seconds >= _nextDrifter)
        {
            var path = TitleScene.NextDrifter(_rng, view);
            var figure = new Figure(Suits.All[_rng.Next(1, Suits.All.Length)], own: false) { HeadLamp = path.Lamp };
            // Turned about its middle, not its feet.
            var middle = new Node2D { Position = new Vector2(0, (float)(Rig.H / 2)) };
            middle.AddChild(figure);
            Drift(path, middle, figure);
            _nextDrifter = _seconds + TitleScene.Gap(_rng, TitleScene.DrifterGapLeast, TitleScene.DrifterGapMost);
        }
        if (_seconds >= _nextPiece)
        {
            var piece = new Sprite2D { Texture = GD.Load<Texture2D>(Pieces[_rng.Next(Pieces.Length)]), TextureFilter = TextureFilterEnum.Nearest };
            Lit.Set(piece);
            Drift(TitleScene.NextPiece(_rng, view), piece, null);
            _nextPiece = _seconds + TitleScene.Gap(_rng, TitleScene.PieceGapLeast, TitleScene.PieceGapMost);
        }
        double look = 0.3;
        for (int i = _drifting.Count - 1; i >= 0; i--)
        {
            var (path, from, holder, figure) = _drifting[i];
            if (path.At(_seconds - from) is not var (x, y, turn))
            {
                holder.QueueFree();
                _drifting.RemoveAt(i);
                continue;
            }
            holder.Position = new Vector2((float)x, (float)y) * ShipCamera.Cell;
            holder.Rotation = (float)turn;
            if (figure == null) continue;
            figure.Show(new Hero(0, 0, 0, 0, false, 1), Pose.Tumble, 0, false, delta * 1000);
            // The seated hero watches one going by before it.
            if (x < _seat.X - 2 && x > view.Item1) look = Poses.HeadTurn(_seat.X - x, y - (_seat.Y - Rig.Neck / Rig.Cell));
        }
        var hero = new Hero((int)Math.Round(_seat.X * Hero.Unit), (int)Math.Round(_seat.Y * Hero.Unit), 0, 0, false, -1);
        _hero.Show(hero, Pose.Sit, look, false, delta * 1000);
    }

    /// <summary>Sets <paramref name="drawn"/> drifting along <paramref name="path"/>: nearer ones before the wreck, farther ones behind it, dimmer.</summary>
    private void Drift(Drifter path, Node2D drawn, Figure? figure)
    {
        var holder = new Node2D { ZIndex = path.Depth >= 0.9 ? Layers.Effects : Layers.Debris, Scale = Vector2.One * (float)Math.Min(1, path.Depth) };
        holder.Modulate = (new Color(1, 1, 1) * (float)Math.Min(1, 0.35 + 0.65 * path.Depth)) with { A = 1 };
        holder.AddChild(drawn);
        AddChild(holder);
        _drifting.Add((path, _seconds, holder, figure));
    }
}
