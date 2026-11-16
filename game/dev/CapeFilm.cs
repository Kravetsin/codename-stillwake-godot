using Godot;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Render;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --cape-film=&lt;path.png&gt;</c>: the hero in its cape standing, breaking into a run, running and stopping, a step a
/// frame at 60 a second, every <see cref="Every"/>th step laid on one sheet, the camera on the hero; then the game quits.
/// What the cape does through a run, read frame by frame.
/// </summary>
public partial class CapeFilm(string path) : Node
{
    private const int Zoom = 3, TileW = 12 * (int)Rig.Cell * Zoom, TileH = 7 * (int)Rig.Cell * Zoom, Across = 8, Every = 3;
    /// <summary>The steps standing, running and stopped, and the first step filmed.</summary>
    private const int Stand = 20, Run = 150, Stop = 70, From = 12;

    private SubViewport _view = null!;
    private Figure _figure = null!;
    private Camera2D _camera = null!;
    private Image _sheet = null!;
    private int _step, _shot;
    private int _x;

    public override void _Ready()
    {
        int shots = (Stand + Run + Stop - From) / Every + 1;
        _sheet = Image.CreateEmpty(TileW * Across, TileH * ((shots + Across - 1) / Across), false, Image.Format.Rgba8);
        _view = new SubViewport { Size = new Vector2I(TileW, TileH), RenderTargetUpdateMode = SubViewport.UpdateMode.Always, CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        AddChild(_view);
        var world = new Node2D();
        _view.AddChild(world);
        world.AddChild(new Floor());
        Lit.Ambient((1, 1, 1));
        int colour = Suits.All[0];
        _figure = new Figure(colour, own: false) { Tracked = true };
        _figure.Wear(colour, Stillwake.Content.Helmet.Basic, Stillwake.Content.Suit.Basic);
        _figure.CapeWorn = Stillwake.Content.Cape.Item;
        world.AddChild(_figure);
        _view.AddChild(_camera = new Camera2D { Zoom = new Vector2(Zoom, Zoom), Enabled = true });
    }

    public override void _Process(double delta)
    {
        // What was rendered for the last step, laid down before the next step moves on.
        if (_step > From && (_step - From - 1) % Every == 0)
        {
            var shot = _view.GetTexture().GetImage();
            shot.Convert(Image.Format.Rgba8);
            _sheet.BlitRect(shot, new Rect2I(0, 0, TileW, TileH), new Vector2I(_shot % Across * TileW, _shot / Across * TileH));
            _shot++;
        }
        if (_step >= Stand + Run + Stop)
        {
            _sheet.SavePng(path);
            GD.Print($"cape film: {_shot} frames saved to {path}");
            GetTree().Quit();
            return;
        }
        bool running = _step >= Stand && _step < Stand + Run;
        int speed = running ? Spec.Run : 0;
        _x += speed;
        _figure.Show(new Hero(_x, 0, speed, 0, false, 1), running ? Pose.Run : Pose.Stand, 0, false, 1000.0 / 60);
        float feet = (float)((double)_x / Hero.Unit * Rig.Cell);
        _figure.Position = new Vector2(feet, 0);
        _camera.Position = new Vector2(feet - (float)(2 * Rig.Cell), (float)(-Rig.H / 2));
        _step++;
    }

    /// <summary>A dark field and a floor marked every other cell, so the run shows against it.</summary>
    private sealed partial class Floor : Node2D
    {
        public override void _Draw()
        {
            DrawRect(new Rect2(-200, -400, 4000, 400), new Color(0.13f, 0.14f, 0.17f));
            DrawRect(new Rect2(-200, 0, 4000, (float)Rig.Cell), new Color(0.3f, 0.3f, 0.34f));
            for (float x = -200; x < 3800; x += (float)Rig.Cell * 2) DrawRect(new Rect2(x, 0, 1, (float)Rig.Cell), new Color(0.22f, 0.22f, 0.26f));
        }
    }
}
