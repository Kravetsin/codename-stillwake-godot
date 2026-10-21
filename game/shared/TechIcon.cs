using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// A module technology's icon (<see cref="TechLooks"/>, 0086), a square of pixels painted by <see cref="TechCanvas"/>:
/// a slot's mark, a socket's ghost or glow, a card's. Lit, it crackles while seen, repainted twelve times a second with
/// fresh kinks and brightness; a ghost stands still.
/// </summary>
public partial class TechIcon : TextureRect
{
    private readonly TechCanvas _canvas;
    private readonly Image _image;
    private readonly ImageTexture _texture;
    private readonly Random _random = new();
    private TechLooks.Look _look;
    private TechShow _show;
    private double _clock;

    public TechIcon(string tech, int side, TechShow show)
    {
        ExpandMode = ExpandModeEnum.IgnoreSize;
        StretchMode = StretchModeEnum.Scale;
        MouseFilter = MouseFilterEnum.Ignore;
        CustomMinimumSize = Size = new Vector2(side, side);
        _canvas = new TechCanvas(side);
        _image = Image.CreateEmpty(side, side, false, Image.Format.Rgba8);
        _texture = ImageTexture.CreateFromImage(_image);
        Texture = _texture;
        (_look, _show) = (TechLooks.Of(tech), show);
        Paint();
    }

    /// <summary>Shows technology <paramref name="tech"/>'s icon as <paramref name="show"/> asks.</summary>
    public void Set(string tech, TechShow show)
    {
        var look = TechLooks.Of(tech);
        if (look == _look && show == _show) return;
        (_look, _show) = (look, show);
        Paint();
        Animate();
    }

    public override void _Notification(int what)
    {
        if (what is (int)NotificationVisibilityChanged or (int)NotificationEnterTree) Animate();
    }

    private void Animate() => SetProcess(_show != TechShow.Ghost && IsInsideTree() && IsVisibleInTree());

    public override void _Process(double delta)
    {
        _clock += delta;
        const double Every = 1.0 / TechLooks.CracklesPerSecond;
        if (_clock < Every) return;
        _clock %= Every;
        Paint();
    }

    private void Paint()
    {
        bool still = _show == TechShow.Ghost;
        int seed = still ? 0 : _random.Next(1, int.MaxValue);
        float intensity = still ? 1 : TechLooks.Dimmest + (float)_random.NextDouble() * (TechLooks.Brightest - TechLooks.Dimmest);
        _canvas.Paint(_look, TechLooks.Strokes(_look, seed), _show, intensity);
        _image.SetData(_canvas.Side, _canvas.Side, false, Image.Format.Rgba8, _canvas.Rgba);
        _texture.Update(_image);
    }
}
