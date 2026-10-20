using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Grid;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --feet-sheet=&lt;dir&gt;</c>: a contact sheet of the hero's feet on slopes (<see cref="Footing"/>), drawn on the
/// grid as the game draws it (<c>--feet-ground t</c> of stone, <c>s</c> of soil; the hull without), fully lit, saved as
/// <c>feet.png</c> in the folder, then the game quits. Each row a walk, a run or a stand moved by the controller over a ramp up
/// and down, stairs of slopes and a ledge, a tile every few steps.
/// </summary>
public partial class FeetSheet(string dir, char? ground = null) : Node
{
    private const int TileCells = 4, Zoom = 6, Columns = 10;
    private const int Tile = TileCells * (int)Rig.Cell * Zoom;

    // Flat, a ramp up three (/ is ◢), a plateau, down three (\ is ◣), flat, stairs of slopes up to a plateau, a cliff.
    private static readonly string[] MapRows =
    [
        "########################################",
        "#......................................#",
        "#......................................#",
        "#......................................#",
        "#......................................#",
        @"#......../##\........../######.........#",
        @"#......./####\......./########.........#",
        @"#S...../######\..../##########.........#",
        "########################################",
        "########################################",
    ];

    private static readonly Layout Map = Layout.Parse(MapRows);

    /// <summary>
    /// A row of the sheet: from each of <see cref="Starts"/> (a column's middle, in cells, and the way it faces), the keys held
    /// for <see cref="Every"/> steps before each tile, <see cref="Tiles"/> tiles a start.
    /// </summary>
    private sealed record Strip((double X, int Face)[] Starts, HeroInput Keys, int Every, int Tiles);

    private static readonly HeroInput Still = new(0, 0, false);

    private static readonly Strip[] Strips =
    [
        // Walking up and down the ramp facing right, then facing left; a tile every three steps, about the page's four pixels.
        new([(6.2, 1)], new HeroInput(1, 0, false, Face: 1), 3, Columns),
        new([(11.2, 1)], new HeroInput(1, 0, false, Face: 1), 3, Columns),
        new([(15.0, -1)], new HeroInput(-1, 0, false, Face: -1), 3, Columns),
        new([(9.8, -1)], new HeroInput(-1, 0, false, Face: -1), 3, Columns),
        // Walking backwards up the ramp, facing down it.
        new([(6.2, -1)], new HeroInput(1, 0, false, Face: -1), 3, Columns),
        // Running up and down.
        new([(5.8, 1)], new HeroInput(1, 0, false, Face: 1, Sprint: true), 2, Columns),
        new([(10.6, 1)], new HeroInput(1, 0, false, Face: 1, Sprint: true), 2, Columns),
        // Up the stairs of slopes and off the cliff's edge.
        new([(17.5, 1)], new HeroInput(1, 0, false, Face: 1), 5, Columns),
        // Standing: on the ramp both ways, at its foot, its top's corner, on the stairs, at the cliff's edge.
        new([(7.5, 1), (7.5, -1), (6.2, 1), (8.9, 1), (12.1, -1), (20.5, 1), (21.5, -1), (29.9, 1), (30.1, -1), (2.5, 1)], new HeroInput(0, 0, false), 45, 1),
    ];

    private readonly List<(int Row, int Column, int Strip, int Start)> _tiles = [];
    private SubViewport _view = null!;
    private Camera2D _camera = null!;
    private Node2D _world = null!;
    private Ship _ship = null!;
    private Figure? _figure;
    private Hero _hero;
    private int _tile = -1, _frames;
    private Image _sheet = null!;

    public override void _Ready()
    {
        Lit.Ambient((1, 1, 1));
        for (int s = 0; s < Strips.Length; s++)
            for (int start = 0, column = 0; start < Strips[s].Starts.Length; start++)
                for (int t = 0; t < Strips[s].Tiles; t++) _tiles.Add((s, column++, s, start));
        var m = new Registry();
        // Ground of a natural material (Legend.Materials), its walls drawn as a planet's, or the hull's.
        // On ground, the open cells under open sky (no back wall), as a planet's: its living edge grows there.
        var map = ground is char g
            ? Layout.Parse([.. MapRows.Select(r => new string([.. r.Select(c => c == '.' ? ' ' : c)]))])
                .WithMaterials(string.Join('\n', MapRows.Select(r => new string([.. r.Select(c => c is '#' or '/' or '\\' ? g : '.')]))))
            : Map;
        World.Create(1, map, m.All);
        _ship = Ship.Of(m.Grid, m.Rooms, rules: ground != null ? Content.Surfaces.Planet : null);
        _view = new SubViewport { Size = new Vector2I(Tile, Tile), RenderTargetUpdateMode = SubViewport.UpdateMode.Always, CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        AddChild(_view);
        _view.AddChild(_world = new Node2D());
        var grid = new GridView(m.Grid);
        _world.AddChild(grid);
        grid.Sync(new ShownGrid("", null, null));
        _view.AddChild(_camera = new Camera2D { Zoom = new Vector2(Zoom, Zoom), Enabled = true });
        _sheet = Image.CreateEmpty(Tile * Columns, Tile * Strips.Length, false, Image.Format.Rgba8);
        Next();
    }

    private void Next()
    {
        _tile++;
        _frames = 0;
        if (_tile >= _tiles.Count)
        {
            DirAccess.MakeDirRecursiveAbsolute(dir);
            _sheet.SavePng($"{dir}/feet.png");
            GD.Print($"feet sheet: saved to {dir}");
            GetTree().Quit();
            return;
        }
        var (_, column, s, start) = _tiles[_tile];
        var strip = Strips[s];
        bool fresh = _tile == 0 || _tiles[_tile - 1].Strip != s || _tiles[_tile - 1].Start != start;
        if (fresh)
        {
            _figure?.QueueFree();
            var (x, face) = strip.Starts[start];
            int at = (int)(x * Hero.Unit);
            _hero = new Hero(at, Hero.GroundAt(_ship, at, Hero.Unit + 1, 0, 9 * Hero.Unit) ?? Hero.Unit, 0, 0, false, face);
            _figure = new Figure(0x3d7dd8, own: false) { Ground = _ship, ZIndex = Layers.Hero };
            _world.AddChild(_figure);
            for (int i = 0; i < 20; i++) Step(Still with { Face = face });
        }
        for (int i = 0; i < strip.Every; i++) Step(strip.Keys);
        _camera.Position = new Vector2((float)(_hero.X * Rig.Cell / Hero.Unit), (float)(_hero.Y * Rig.Cell / Hero.Unit - Rig.Hip));
    }

    /// <summary>One controller step and its frame of the figure.</summary>
    private void Step(HeroInput keys)
    {
        _hero = _hero.Step(keys, _ship);
        _figure!.Show(_hero, Poses.PoseOf(_hero, false, false), 0, false, Hero.StepMs);
    }

    public override void _Process(double delta)
    {
        if (_tile >= _tiles.Count || ++_frames < 3) return;
        var (row, column, _, _) = _tiles[_tile];
        var shot = _view.GetTexture().GetImage();
        shot.Convert(Image.Format.Rgba8);
        _sheet.BlitRect(shot, new Rect2I(0, 0, Tile, Tile), new Vector2I(column * Tile, row * Tile));
        Next();
    }
}
