using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Render;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --gesture-sheet=&lt;folder&gt;</c>: every gesture candidate (<see cref="GestureCandidates"/>) played through by the hero standing,
/// suited above and bare below, <see cref="Frames"/> frames evenly over its length, written as <c>&lt;id&gt;.png</c>, with
/// <c>gestures.json</c> listing them; then the game quits. A pointing gesture points up ahead.
/// </summary>
public partial class GestureSheet(string folder) : Node
{
    public const int Frames = 24;
    private const int TileCells = 7, Zoom = 3;
    private const int Tile = TileCells * (int)Rig.Cell * Zoom;
    private static readonly Vector2 PointAt = new(40, -32);

    private readonly List<(GestureCandidates.Candidate Candidate, int Body, int Frame)> _shots = [];
    private SubViewport _view = null!;
    private Node2D _world = null!;
    private Figure? _figure;
    private int _shot = -1, _frames;
    private Image? _strip;

    public override void _Ready()
    {
        Directory.CreateDirectory(folder);
        foreach (var candidate in GestureCandidates.All)
            for (int body = 0; body < 2; body++)
                for (int frame = 0; frame < Frames; frame++) _shots.Add((candidate, body, frame));
        File.WriteAllText(Path.Combine(folder, "gestures.json"), JsonSerializer.Serialize(GestureCandidates.All.Select(c => new
        {
            id = c.Id,
            gesture = c.Gesture.ToString().ToLowerInvariant(),
            name = c.Name,
            ms = c.Motion.Ms,
            frames = Frames,
            tile = Tile,
        })));
        _view = new SubViewport { Size = new Vector2I(Tile, Tile), RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
        AddChild(_view);
        _world = new Node2D();
        _view.AddChild(_world);
        _world.AddChild(new Ground());
        _view.AddChild(new Camera2D { Zoom = new Vector2(Zoom, Zoom), Position = new Vector2(8, (float)(-Rig.H / 2 - 4)), Enabled = true });
        Lit.Ambient((1, 1, 1));
        Next();
    }

    private void Next()
    {
        _shot++;
        _frames = 0;
        if (_shot >= _shots.Count)
        {
            GD.Print($"gesture sheet: {GestureCandidates.All.Count} candidates written to {folder}");
            GetTree().Quit();
            return;
        }
        var (candidate, body, frame) = _shots[_shot];
        if (frame == 0)
        {
            _figure?.QueueFree();
            int colour = Suits.All[body == 0 ? 0 : 1];
            _figure = new Figure(colour, own: false);
            _figure.Wear(colour, body == 0 ? Stillwake.Content.Helmet.Basic : null, body == 0 ? Stillwake.Content.Suit.Basic : null);
            _world.AddChild(_figure);
            if (body == 0) _strip = Image.CreateEmpty(Tile * Frames, Tile * 2, false, Image.Format.Rgba8);
        }
        double ms = candidate.Motion.Ms * (frame + 0.5) / Frames;
        _figure!.Gesture = (candidate.Motion, ms, PointAt);
        for (int i = 0; i < 3; i++) _figure.Show(new Hero(0, 0, 0, 0, false, 1), Pose.Stand, 0.15, false, 1000.0 / 60);
        _figure.Position = Vector2.Zero;
    }

    public override void _Process(double delta)
    {
        if (_shot >= _shots.Count || ++_frames < 3) return;
        var (candidate, body, frame) = _shots[_shot];
        var shot = _view.GetTexture().GetImage();
        shot.Convert(Image.Format.Rgba8);
        _strip!.BlitRect(shot, new Rect2I(0, 0, Tile, Tile), new Vector2I(frame * Tile, body * Tile));
        if (body == 1 && frame == Frames - 1) _strip.SavePng(Path.Combine(folder, candidate.Id + ".png"));
        Next();
    }

    /// <summary>A dark field and the floor under the feet.</summary>
    private sealed partial class Ground : Node2D
    {
        public override void _Draw()
        {
            float half = TileCells * (float)Rig.Cell;
            DrawRect(new Rect2(-2 * half, -2 * half, 4 * half, 3 * half), new Color(0.13f, 0.14f, 0.17f));
            DrawRect(new Rect2(-2 * half, 0, 4 * half, (float)Rig.Cell), new Color(0.3f, 0.3f, 0.34f));
        }
    }
}
