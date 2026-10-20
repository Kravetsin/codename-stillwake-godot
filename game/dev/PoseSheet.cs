using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Render;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --pose-sheet=&lt;dir&gt;</c>: a contact sheet of the hero holding an item, drawn alone, fully lit, and saved as
/// PNGs in the folder, then the game quits. One sheet each standing and on a ladder, first facing right and left (it
/// turns to the pointer as the game does): eight ways round the shoulder, near and far, the cell aimed at outlined.
/// <c>--pose-item=&lt;id&gt;</c> picks the item, the drill unless it says.
/// </summary>
public partial class PoseSheet(string dir, string item) : Node
{
    /// <summary>Each tile's side in cells, the zoom drawn at, and the two distances aimed at from the shoulder, in cells.</summary>
    private const int TileCells = 7, Zoom = 3;
    private const double Near = 1.25, Far = 2.75;
    private const int Tile = TileCells * (int)Rig.Cell * Zoom;

    private readonly List<(bool Ladder, int Facing, bool Far, int Way)> _cases = [];
    private SubViewport _view = null!;
    private Node2D _world = null!;
    private Figure? _figure;
    private Backdrop _backdrop = null!;
    private int _case = -1, _frames;
    private readonly Dictionary<(bool Ladder, int Facing), Image> _sheets = new();

    public override void _Ready()
    {
        Lit.Ambient((1, 1, 1));
        foreach (bool ladder in new[] { false, true })
            foreach (int facing in new[] { 1, -1 })
                foreach (bool far in new[] { false, true })
                    for (int way = 0; way < 8; way++) _cases.Add((ladder, facing, far, way));
        _view = new SubViewport { Size = new Vector2I(Tile, Tile), RenderTargetUpdateMode = SubViewport.UpdateMode.Always, CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        AddChild(_view);
        _world = new Node2D();
        _view.AddChild(_world);
        _world.AddChild(_backdrop = new Backdrop());
        _view.AddChild(new Camera2D { Zoom = new Vector2(Zoom, Zoom), Position = Shoulder, Enabled = true });
        Next();
    }

    /// <summary>The hero stands on the floor of the cell at the origin, its shoulder here, in pixels.</summary>
    private static readonly Vector2 Feet = new((float)Rig.Cell / 2, (float)Rig.Cell);
    private static Vector2 Shoulder => Feet - new Vector2(0, (float)Rig.Shoulder);

    private void Next()
    {
        _case++;
        _frames = 0;
        _figure?.QueueFree();
        _figure = null;
        if (_case >= _cases.Count)
        {
            Save();
            GetTree().Quit();
            return;
        }
        var (ladder, facing, far, way) = _cases[_case];
        double angle = way * Math.PI / 4, cells = far ? Far : Near;
        var target = Shoulder + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * (float)(cells * Rig.Cell);
        var pointer = ((double)target.X / Rig.Cell, (double)target.Y / Rig.Cell);
        var hero = new Hero(Hero.Unit / 2, Hero.Unit, 0, 0, ladder, facing);
        hero = hero with { Facing = Poses.FaceOf(pointer, hero) ?? facing };
        _backdrop.Show(ladder, target);
        _figure = new Figure(0x3d7dd8, own: false) { Wields = item, Aim = target };
        _world.AddChild(_figure);
        double look = Poses.LookOf(hero, pointer);
        for (int i = 0; i < 40; i++) _figure.Show(hero, ladder ? Pose.Climb : Pose.Stand, look, false, 1000.0 / 60);
    }

    public override void _Process(double delta)
    {
        if (_case >= _cases.Count || ++_frames < 3) return;
        var (ladder, facing, far, way) = _cases[_case];
        if (!_sheets.TryGetValue((ladder, facing), out var sheet))
            _sheets[(ladder, facing)] = sheet = Image.CreateEmpty(Tile * 8, Tile * 2, false, Image.Format.Rgba8);
        var shot = _view.GetTexture().GetImage();
        shot.Convert(Image.Format.Rgba8);
        sheet.BlitRect(shot, new Rect2I(0, 0, Tile, Tile), new Vector2I(way * Tile, far ? Tile : 0));
        Next();
    }

    private void Save()
    {
        DirAccess.MakeDirRecursiveAbsolute(dir);
        var all = Image.CreateEmpty(Tile * 8, Tile * 8, false, Image.Format.Rgba8);
        int row = 0;
        foreach (var ((ladder, facing), sheet) in _sheets)
        {
            string name = $"{(ladder ? "ladder" : "stand")}-{(facing > 0 ? "right" : "left")}";
            sheet.SavePng($"{dir}/{name}.png");
            all.BlitRect(sheet, new Rect2I(0, 0, Tile * 8, Tile * 2), new Vector2I(0, row++ * Tile * 2));
        }
        all.SavePng($"{dir}/sheet.png");
        GD.Print($"pose sheet: saved to {dir}");
    }

    /// <summary>What the hero stands in: a dark field with its cells marked, a ladder up through its cell on a ladder, the cell aimed at outlined and the point a cross.</summary>
    private sealed partial class Backdrop : Node2D
    {
        private bool _ladder;
        private Vector2 _target;

        public void Show(bool ladder, Vector2 target)
        {
            (_ladder, _target) = (ladder, target);
            QueueRedraw();
        }

        public override void _Draw()
        {
            float c = (float)Rig.Cell, half = TileCells * c;
            DrawRect(new Rect2(-half, -half, 2 * half, 2 * half), new Color(0.13f, 0.14f, 0.17f));
            for (int i = -TileCells; i <= TileCells; i++)
            {
                DrawLine(new Vector2(i * c, -half), new Vector2(i * c, half), new Color(1, 1, 1, 0.06f), 1 / (float)Zoom);
                DrawLine(new Vector2(-half, i * c), new Vector2(half, i * c), new Color(1, 1, 1, 0.06f), 1 / (float)Zoom);
            }
            // The floor under the feet.
            DrawRect(new Rect2(-half, c, 2 * half, c), new Color(0.3f, 0.3f, 0.34f));
            if (_ladder)
            {
                var wood = new Color(0.55f, 0.4f, 0.25f);
                DrawRect(new Rect2(2, -half, 1.5f, half + c), wood);
                DrawRect(new Rect2(c - 3.5f, -half, 1.5f, half + c), wood);
                for (float y = c - 2; y > -half; y -= 4) DrawRect(new Rect2(2, y, c - 4, 1), wood);
            }
            var cell = new Vector2(Mathf.Floor(_target.X / c), Mathf.Floor(_target.Y / c)) * c;
            DrawRect(new Rect2(cell, new Vector2(c, c)), new Color(1, 0.85f, 0.3f, 0.9f), false, 1 / (float)Zoom);
            var red = new Color(1, 0.3f, 0.3f);
            DrawLine(_target - new Vector2(2, 0), _target + new Vector2(2, 0), red, 1 / (float)Zoom);
            DrawLine(_target - new Vector2(0, 2), _target + new Vector2(0, 2), red, 1 / (float)Zoom);
        }
    }
}
