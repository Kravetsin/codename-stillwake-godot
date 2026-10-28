using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Render;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --dress-sheet=&lt;path.png&gt;</c>: a contact sheet of the hero as it may be dressed (2026-10-27-suits-are-worn), drawn
/// alone and saved, then the game quits. A row each bare, in the helmet only, in the body only and suited, across the four
/// player colours, fully lit; then in the dark, suited with its lamp and without, bare with a lamp it cannot wear, and a body
/// lying bare and suited; a jetpack on a bare back and on the air pack.
/// </summary>
public partial class DressSheet(string path) : Node
{
    /// <summary>Each tile's side in cells, and the zoom drawn at.</summary>
    private const int TileCells = 7, Zoom = 4, Across = 5;
    private const int Tile = TileCells * (int)Rig.Cell * Zoom;
    private static readonly (float, float, float) Dark = (0.12f, 0.12f, 0.15f);

    private readonly record struct Case(int Colour, bool Helmet, bool Body, bool Lamp = false, bool InDark = false, bool Lying = false, bool Jetpack = false);

    private readonly List<Case> _cases = [];
    private SubViewport _view = null!;
    private Node2D _world = null!;
    private Figure? _figure;
    private int _case = -1, _frames;
    private Image _sheet = null!;

    public override void _Ready()
    {
        foreach (var (helmet, body) in new[] { (false, false), (true, false), (false, true), (true, true) })
        {
            foreach (int colour in Suits.All) _cases.Add(new(colour, helmet, body));
            _cases.Add(new(Suits.All[0], helmet, body, InDark: true));
        }
        _cases.Add(new(Suits.All[1], true, true, Lamp: true, InDark: true));
        _cases.Add(new(Suits.All[1], true, false, Lamp: true, InDark: true));
        _cases.Add(new(Suits.All[1], false, false, Lamp: true, InDark: true));
        _cases.Add(new(Suits.All[2], false, false, Lying: true));
        _cases.Add(new(Suits.All[2], true, true, Lying: true));
        _cases.Add(new(Suits.All[3], false, false, Jetpack: true));
        _cases.Add(new(Suits.All[3], true, true, Jetpack: true));
        _sheet = Image.CreateEmpty(Tile * Across, Tile * ((_cases.Count + Across - 1) / Across), false, Image.Format.Rgba8);
        _view = new SubViewport { Size = new Vector2I(Tile, Tile), RenderTargetUpdateMode = SubViewport.UpdateMode.Always, CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        AddChild(_view);
        _world = new Node2D();
        _view.AddChild(_world);
        _world.AddChild(new Ground());
        _view.AddChild(new Camera2D { Zoom = new Vector2(Zoom, Zoom), Position = new Vector2(0, (float)(-Rig.H / 2)), Enabled = true });
        Next();
    }

    private void Next()
    {
        _case++;
        _frames = 0;
        _figure?.QueueFree();
        _figure = null;
        if (_case >= _cases.Count)
        {
            _sheet.SavePng(path);
            GD.Print($"dress sheet: saved to {path}");
            GetTree().Quit();
            return;
        }
        var c = _cases[_case];
        Lit.Ambient(c.InDark ? Dark : (1, 1, 1));
        _figure = new Figure(c.Colour, own: false);
        _figure.Wear(c.Colour, c.Helmet, c.Body);
        _figure.HeadLamp = c.Lamp;
        _figure.Jetpack = c.Jetpack;
        _world.AddChild(_figure);
        var hero = new Hero(0, 0, 0, 0, false, 1);
        _figure.Show(hero, c.Lying ? Pose.Lie : Pose.Stand, 0, false, c.Lying ? Poses.Faint.Ms * 2 : 1000.0 / 60);
        for (int i = 0; i < 40; i++) _figure.Show(hero, c.Lying ? Pose.Lie : Pose.Stand, 0.15, false, 1000.0 / 60);
    }

    public override void _Process(double delta)
    {
        if (_case >= _cases.Count || ++_frames < 3) return;
        var shot = _view.GetTexture().GetImage();
        shot.Convert(Image.Format.Rgba8);
        _sheet.BlitRect(shot, new Rect2I(0, 0, Tile, Tile), new Vector2I(_case % Across * Tile, _case / Across * Tile));
        Next();
    }

    /// <summary>A dark field and the floor under the feet.</summary>
    private sealed partial class Ground : Node2D
    {
        public override void _Draw()
        {
            float half = TileCells * (float)Rig.Cell;
            DrawRect(new Rect2(-half, -2 * half, 2 * half, 3 * half), new Color(0.13f, 0.14f, 0.17f));
            DrawRect(new Rect2(-half, 0, 2 * half, (float)Rig.Cell), new Color(0.3f, 0.3f, 0.34f));
        }
    }
}
