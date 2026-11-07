using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Render;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --haul-sheet=&lt;dir&gt;</c>: the hero hauled in along its tether (tether-haul), drawn alone, fully lit, for each look the
/// candidates offer (<see cref="Poses.HaulLook"/>): a strip a look, a row each way it is hauled (along, up, down, up and along), a
/// column each thirtieth of a second, the tether a line toward where it goes. Saved as PNGs in the folder, then the game quits.
/// </summary>
public partial class HaulSheetDemo(string dir) : Node
{
    private const int TileCells = 9, Zoom = 3, Frames = 36;
    private const double FrameMs = 1000.0 / 30;
    private const int Tile = TileCells * (int)Rig.Cell * Zoom;
    private static readonly char[] Looks = ['a', 'b', 'c'];
    private static readonly (int X, int Y)[] Ways = [(1, 0), (0, -1), (0, 1), (1, -1)];

    private SubViewport _view = null!;
    private Node2D _world = null!;
    private Figure? _figure;
    private Line _line = null!;
    private int _look, _way, _frame = -1, _wait;
    private Image? _strip;
    private Hero _hero;

    public override void _Ready()
    {
        Lit.Ambient((1, 1, 1));
        _view = new SubViewport { Size = new Vector2I(Tile, Tile), RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
        AddChild(_view);
        _world = new Node2D();
        _view.AddChild(_world);
        _world.AddChild(_line = new Line());
        var middle = new Vector2((float)Rig.Cell / 2, (float)(Rig.Cell - Hero.MiddleUp * Rig.Cell / Hero.Unit));
        _view.AddChild(new Camera2D { Zoom = new Vector2(Zoom, Zoom), Position = middle, Enabled = true });
        Begin();
    }

    /// <summary>A new row: the figure made afresh and settled into the haul, hauled the row's way.</summary>
    private void Begin()
    {
        _figure?.QueueFree();
        Poses.HaulLook = Looks[_look];
        var (wx, wy) = Ways[_way];
        double far = Math.Sqrt(wx * wx + wy * wy);
        int vx = (int)Math.Round(wx * Spec.Haul / far), vy = (int)Math.Round(wy * Spec.Haul / far);
        _hero = new Hero(Hero.Unit / 2, Hero.Unit, vx, vy, false, 1, new Point(Hero.Unit / 2 + wx * 20 * Hero.Unit, Hero.Unit + wy * 20 * Hero.Unit));
        _figure = new Figure(0x3d7dd8, own: false);
        _world.AddChild(_figure);
        for (int i = 0; i < 40; i++) _figure.Show(_hero, Pose.Float, 0, false, 1000.0 / 60);
        var clip = Tether.ClipOf(_hero.X, _hero.Y);
        _line.Show(new Vector2((float)(clip.X * Rig.Cell / Hero.Unit), (float)(clip.Y * Rig.Cell / Hero.Unit)), new Vector2(wx, wy).Normalized());
        _frame = 0;
        _wait = 0;
    }

    public override void _Process(double delta)
    {
        if (_figure == null || ++_wait < 2) return;
        _strip ??= Image.CreateEmpty(Tile * Frames, Tile * Ways.Length, false, Image.Format.Rgba8);
        var shot = _view.GetTexture().GetImage();
        shot.Convert(Image.Format.Rgba8);
        _strip.BlitRect(shot, new Rect2I(0, 0, Tile, Tile), new Vector2I(_frame * Tile, _way * Tile));
        _wait = 0;
        if (++_frame < Frames)
        {
            _figure.Show(_hero, Pose.Float, 0, false, FrameMs);
            return;
        }
        if (++_way < Ways.Length)
        {
            Begin();
            return;
        }
        DirAccess.MakeDirRecursiveAbsolute(dir);
        _strip.SavePng($"{dir}/haul-{Looks[_look]}.png");
        _strip = null;
        _way = 0;
        if (++_look < Looks.Length)
        {
            Begin();
            return;
        }
        GD.Print($"haul sheet: saved to {dir}, {Frames} frames of {Tile} px, {FrameMs:0.#} ms each, rows {string.Join(" ", Ways)}");
        GetTree().Quit();
    }

    /// <summary>A dark field with its cells marked and the tether, from the clip on out the way the hero is hauled.</summary>
    private sealed partial class Line : Node2D
    {
        private Vector2 _from, _way;

        public void Show(Vector2 from, Vector2 way)
        {
            (_from, _way) = (from, way);
            QueueRedraw();
        }

        public override void _Draw()
        {
            float c = (float)Rig.Cell, half = TileCells * c;
            DrawRect(new Rect2(-half, -half * 2, 2 * half, 4 * half), new Color(0.07f, 0.08f, 0.11f));
            for (int i = -2 * TileCells; i <= 2 * TileCells; i++)
            {
                DrawLine(new Vector2(i * c, -2 * half), new Vector2(i * c, 2 * half), new Color(1, 1, 1, 0.04f), 1 / (float)Zoom);
                DrawLine(new Vector2(-2 * half, i * c), new Vector2(2 * half, i * c), new Color(1, 1, 1, 0.04f), 1 / (float)Zoom);
            }
            DrawLine(_from, _from + _way * 4 * half, new Color(0.85f, 0.8f, 0.65f), 1.2f / Zoom * 2);
        }
    }
}
