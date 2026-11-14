using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Render;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --dress-sheet=&lt;path.png&gt;</c>: a contact sheet of the hero as it may be dressed (2026-10-27-suits-are-worn), drawn
/// alone and saved, then the game quits. A row each bare, in the helmet only, in the body only and suited, across the four
/// player colours, fully lit; then in the dark, suited with its lamp and without, bare with a lamp it cannot wear, and a body
/// lying bare and suited; a jetpack on a bare back and on the air pack. Then every player helmet on every suit and every
/// catcher's outfit (helmets-fit-the-suit): looking up, ahead and down with the lamp on, its beam's way drawn as a line; walking,
/// running and climbing with the scanner; and aiming up, the near arm raised past the face.
/// </summary>
public partial class DressSheet(string path) : Node
{
    /// <summary>Each tile's side in cells, and the zoom drawn at.</summary>
    private const int TileCells = 7, Zoom = 4, Across = 7;
    private const int Tile = TileCells * (int)Rig.Cell * Zoom;
    private static readonly (float, float, float) Dark = (0.12f, 0.12f, 0.15f);
    /// <summary>The looks shown, up to down: the head's own limits (<see cref="Poses"/>).</summary>
    private static readonly double[] Looks = [-0.6, 0, 0.5];

    private readonly record struct Case(int Colour, string? Helmet, string? Body, bool Lamp = false, bool InDark = false, bool Lying = false,
        bool Jetpack = false, Pose Pose = Pose.Stand, double Look = 0.15, bool Scanner = false, bool Aimed = false);

    private readonly List<(Case Case, int Column, int Row)> _cases = [];
    private SubViewport _view = null!;
    private Node2D _world = null!;
    private Figure? _figure;
    private Line2D _beam = null!;
    private int _case = -1, _frames;
    private Image _sheet = null!;

    public override void _Ready()
    {
        var rows = new List<List<Case>>();
        string helmet = Stillwake.Content.Helmet.Basic, suit = Stillwake.Content.Suit.Basic;
        foreach (var (h, b) in new[] { (null, null), (helmet, null), (null, suit), (helmet, suit) })
            rows.Add([.. Suits.All.Select(colour => new Case(colour, h, b)), new(Suits.All[0], h, b, InDark: true)]);
        rows.Add([
            new(Suits.All[1], helmet, suit, Lamp: true, InDark: true), new(Suits.All[1], helmet, null, Lamp: true, InDark: true),
            new(Suits.All[1], null, null, Lamp: true, InDark: true), new(Suits.All[2], null, null, Lying: true), new(Suits.All[2], helmet, suit, Lying: true),
            new(Suits.All[3], null, null, Jetpack: true), new(Suits.All[3], helmet, suit, Jetpack: true),
        ]);
        string[] helmets = [helmet, Stillwake.Content.Helmet.Quilted, Stillwake.Content.Helmet.Heavy];
        foreach (string body in new[] { suit, "regenSuit", "heatSuit" })
            foreach (string h in helmets)
                rows.Add(Turns(Suits.All[0], h, body, aimed: true));
        foreach (var gear in CreatureLooks.Heroes.Values.SelectMany(look => look.Gear))
            rows.Add(Turns(Suits.All[2], gear.Helmet, gear.Body, aimed: false));
        for (int r = 0; r < rows.Count; r++)
            for (int c = 0; c < rows[r].Count; c++) _cases.Add((rows[r][c], c, r));
        _sheet = Image.CreateEmpty(Tile * Across, Tile * rows.Count, false, Image.Format.Rgba8);
        _view = new SubViewport { Size = new Vector2I(Tile, Tile), RenderTargetUpdateMode = SubViewport.UpdateMode.Always, CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        AddChild(_view);
        _world = new Node2D();
        _view.AddChild(_world);
        _world.AddChild(new Ground());
        _world.AddChild(_beam = new Line2D { Width = 0.5f, DefaultColor = new Color(1, 0.85f, 0.4f, 0.8f), ZIndex = 2, Visible = false });
        _view.AddChild(new Camera2D { Zoom = new Vector2(Zoom, Zoom), Position = new Vector2(0, (float)(-Rig.H / 2)), Enabled = true });
        Next();
    }

    /// <summary>A row of one outfit turning its head and moving: each look with the lamp on, walking, running, climbing, and aiming up if <paramref name="aimed"/>.</summary>
    private static List<Case> Turns(int colour, string helmet, string body, bool aimed)
    {
        var row = Looks.Select(look => new Case(colour, helmet, body, Lamp: true, Look: look)).ToList();
        foreach (var pose in new[] { Pose.Walk, Pose.Run, Pose.Climb }) row.Add(new(colour, helmet, body, Pose: pose, Scanner: true));
        if (aimed) row.Add(new(colour, helmet, body, Aimed: true, Look: -0.6));
        return row;
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
        var c = _cases[_case].Case;
        Lit.Ambient(c.InDark ? Dark : (1, 1, 1));
        _figure = new Figure(c.Colour, own: false);
        _figure.Wear(c.Colour, c.Helmet, c.Body);
        _figure.HeadLamp = c.Lamp;
        _figure.Scanner = c.Scanner;
        _figure.Jetpack = c.Jetpack;
        _world.AddChild(_figure);
        if (c.Aimed) (_figure.Wields, _figure.Aim) = (Stillwake.Content.Harpoon.Item, new Vector2(20, -70));
        var pose = c.Lying ? Pose.Lie : c.Pose;
        int speed = pose switch { Pose.Walk => Spec.Walk, Pose.Run => Spec.Run, _ => 0 };
        bool climbing = pose == Pose.Climb;
        _figure.Show(new Hero(0, 0, 0, 0, climbing, 1), pose, c.Look, false, c.Lying ? Poses.Faint.Ms * 2 : 1000.0 / 60);
        for (int i = 1; i < 40; i++)
        {
            var hero = new Hero(i * speed, climbing ? -i * Spec.ClimbUp : 0, speed, climbing ? -Spec.ClimbUp : 0, climbing, 1);
            _figure.Show(hero, pose, c.Look, false, 1000.0 / 60);
        }
        _figure.Position = Vector2.Zero;
        _beam.Visible = c.Lamp && _figure.HeadLamp;
        if (_beam.Visible) _beam.Points = [_figure.HeadLampAt, _figure.HeadLampAt + _figure.HeadLampToward * 14];
    }

    public override void _Process(double delta)
    {
        if (_case >= _cases.Count || ++_frames < 3) return;
        var shot = _view.GetTexture().GetImage();
        shot.Convert(Image.Format.Rgba8);
        var (_, column, row) = _cases[_case];
        _sheet.BlitRect(shot, new Rect2I(0, 0, Tile, Tile), new Vector2I(column * Tile, row * Tile));
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
