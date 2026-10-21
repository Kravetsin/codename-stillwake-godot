using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using static Stillwake.Game.IconLooks;

namespace Stillwake.Game.Shared;

/// <summary>
/// A glowing line icon (<see cref="IconLooks"/>): a module technology's (<see cref="TechLooks"/>, 0086) or a station's mark
/// (<see cref="MarkLooks"/>, 0097), a square of pixels painted by <see cref="IconCanvas"/>: a slot's mark, a socket's or
/// slot's ghost or glow, a card's. Lit, it moves its own way while seen, twelve frames a second; a ghost and a still look
/// stand still. A motion that comes round (<see cref="Cycle"/>) is painted once a frame for every icon of its look, size and
/// show, so a bag full of flames costs a texture swap each; a crackle is painted afresh. Each icon starts at a frame of its
/// own, so marks never move in step.
/// </summary>
public partial class GlowIcon : TextureRect
{
    private static readonly Random Starts = new();
    private static readonly Dictionary<(Look, IconShow, int Side, int Frame), ImageTexture> Frames = [];
    private static IconCanvas? _shared;

    private readonly int _side;
    private Look _look;
    private IconShow _show;
    private double _clock;
    private int _frame = Starts.Next(0, 1 << 20);
    /// <summary>A crackle's own canvas and texture, painted afresh; made when first needed.</summary>
    private (IconCanvas Canvas, Image Image, ImageTexture Texture)? _own;

    public GlowIcon(Look look, int side, IconShow show)
    {
        ExpandMode = ExpandModeEnum.IgnoreSize;
        StretchMode = StretchModeEnum.Scale;
        MouseFilter = MouseFilterEnum.Ignore;
        CustomMinimumSize = Size = new Vector2(side, side);
        (_side, _look, _show) = (side, look, show);
        Paint();
    }

    /// <summary>Shows <paramref name="look"/> as <paramref name="show"/> asks.</summary>
    public void Set(Look look, IconShow show)
    {
        if (look == _look && show == _show) return;
        (_look, _show) = (look, show);
        Paint();
        Animate();
    }

    public override void _Notification(int what)
    {
        if (what is (int)NotificationVisibilityChanged or (int)NotificationEnterTree) Animate();
    }

    private bool Moving => _show != IconShow.Ghost && _look.Motion != IconMotion.Still;

    private void Animate() => SetProcess(Moving && IsInsideTree() && IsVisibleInTree());

    public override void _Process(double delta)
    {
        _clock += delta;
        const double Every = 1.0 / FramesPerSecond;
        if (_clock < Every) return;
        _clock %= Every;
        _frame = (_frame + 1) % (1 << 20);
        Paint();
    }

    private void Paint()
    {
        if (!Moving)
        {
            Texture = Shared(0);
            return;
        }
        if (Cycle(_look) is int cycle)
        {
            Texture = Shared(1 + _frame % cycle);
            return;
        }
        var (canvas, image, texture) = _own ??= Own();
        PaintFrame(canvas, 1 + _frame);
        image.SetData(_side, _side, false, Image.Format.Rgba8, canvas.Rgba);
        texture.Update(image);
        Texture = texture;
    }

    /// <summary>Frame <paramref name="frame"/> of this icon's look, size and show, painted the first time any icon asks for it.</summary>
    private ImageTexture Shared(int frame)
    {
        var key = (_look, _show, _side, frame);
        if (Frames.TryGetValue(key, out var texture)) return texture;
        var canvas = _shared?.Side == _side ? _shared : _shared = new IconCanvas(_side);
        PaintFrame(canvas, frame);
        texture = ImageTexture.CreateFromImage(Image.CreateFromData(_side, _side, false, Image.Format.Rgba8, canvas.Rgba));
        Frames[key] = texture;
        return texture;
    }

    private (IconCanvas, Image, ImageTexture) Own()
    {
        var image = Image.CreateEmpty(_side, _side, false, Image.Format.Rgba8);
        return (new IconCanvas(_side), image, ImageTexture.CreateFromImage(image));
    }

    private void PaintFrame(IconCanvas canvas, int frame)
    {
        var pose = Lines(_look, frame);
        canvas.Paint(_look, pose.Lines, _show, pose.Intensity);
    }
}
