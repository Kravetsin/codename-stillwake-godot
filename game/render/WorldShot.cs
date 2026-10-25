using System;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// A small picture of the world as the camera sees it, for the title's world cards (0031): drawn on request in a viewport of
/// its own that shares the world's canvas, so none of the HUD, the menu or a panel is in it and nothing on screen flickers.
/// The stars are a screen layer of their own, so the picture's space is plain dark.
/// </summary>
public partial class WorldShot : SubViewport
{
    /// <summary>The picture's size, in pixels: 16 by 9, a card's picture twice over.</summary>
    public static readonly Vector2I Shot = new(256, 144);

    /// <summary>How much closer than the camera the picture looks: a card is small, the ship must fill it.</summary>
    private const float Closer = 5;

    private readonly Camera2D _camera = new() { Enabled = true };
    private Action<Image>? _then;
    private int _frames;

    public WorldShot()
    {
        Size = Shot;
        Disable3D = true;
        RenderTargetUpdateMode = UpdateMode.Disabled;
        CanvasItemDefaultTextureFilter = DefaultCanvasItemTextureFilter.Nearest;
        AddChild(_camera);
    }

    public override void _Ready() => World2D = GetParent().GetViewport().World2D;

    /// <summary>Draws the world round <paramref name="at"/>, in its pixels, as a camera at <paramref name="zoom"/> on a screen <paramref name="screen"/> wide shows it, and hands the picture over.</summary>
    public void Take(Vector2 at, float zoom, float screen, Action<Image> then)
    {
        _camera.Position = at;
        _camera.Zoom = Vector2.One * zoom * Closer * Shot.X / screen;
        (_then, _frames) = (then, 0);
        RenderTargetUpdateMode = UpdateMode.Always;
    }

    public override void _Process(double delta)
    {
        // A few frames in: the camera set first, then the world drawn with it.
        if (_then == null || ++_frames < 3) return;
        var image = GetTexture().GetImage();
        RenderTargetUpdateMode = UpdateMode.Disabled;
        var then = _then;
        _then = null;
        then(image);
    }
}
