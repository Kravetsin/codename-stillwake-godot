using System;
using System.Linq;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// A stretch of space's far picture (<see cref="StretchLooks.Backdrop"/>): a row of its pictures far behind the far wrecks, faded in
/// as its warning runs and out as it eases back. The row is laid where the camera looks when it first shows, then follows
/// <see cref="StretchLooks.Parallax"/> of the camera and drifts left. Unlit, on the stars' canvas layer; a few pictures a frame.
/// </summary>
public partial class StretchBackdrop : Node2D
{
    private StretchLooks.Look? _look;
    private Texture2D[] _pictures = [];
    // Where the row's first place lies on its plane, and its seed: set when it first shows, so the row comes into sight.
    private (double X, double Y)? _origin;
    private uint _seed;
    private (double X, double Y) _centre;
    private float _zoom = 1;
    private Vector2 _screen;

    public StretchBackdrop() => TextureFilter = TextureFilterEnum.LinearWithMipmaps;

    /// <summary>Shows <paramref name="look"/>'s far picture at <paramref name="share"/> of its full; none for none or 0.</summary>
    public void Show(StretchLooks.Look? look, float share)
    {
        if (look != _look)
        {
            _look = look;
            _pictures = look?.Far.Pictures.Select(PixelArt.Far).ToArray() ?? [];
            _origin = null;
        }
        var veil = StretchLooks.Veil;
        var colour = new Color(veil.R, veil.G, veil.B, Mathf.Clamp(share, 0, 1));
        if (colour != Modulate) Modulate = colour;
        Visible = look != null && share > 0;
    }

    /// <summary>Lays the row under a camera centred on <paramref name="camera"/> at <paramref name="zoom"/>, <paramref name="seconds"/> of the world on.</summary>
    public void Follow(Vector2 camera, float zoom, Vector2 screen, double seconds)
    {
        if (!Visible || _look == null) return;
        zoom = Mathf.Max(zoom, ShipCamera.FarthestZoom(screen));
        var far = _look.Far;
        _centre = Scatter.Centre(camera.X, camera.Y, StretchLooks.Parallax, far.Drift, seconds);
        if (_origin == null)
        {
            _origin = (_centre.X + StretchLooks.Right * screen.X / zoom - far.Width / 2, _centre.Y - StretchLooks.Above * screen.Y / zoom);
            _seed = (uint)seconds;
        }
        (_zoom, _screen) = (zoom, screen);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_look == null || _origin is not { } origin || _pictures.Length == 0) return;
        var far = _look.Far;
        double period = far.Width * (1 + far.Gap), half = _screen.X / 2 / _zoom;
        long first = (long)Math.Floor((_centre.X - half - far.Width - origin.X) / period);
        long last = (long)Math.Floor((_centre.X + half - origin.X) / period);
        for (long place = first; place <= last; place++)
        {
            var picture = _pictures[StretchLooks.PictureAt(place, _seed, _pictures.Length)];
            float height = far.Width * picture.GetHeight() / picture.GetWidth();
            var at = Scatter.OnScreen(origin.X + place * period, origin.Y - height / 2, _centre, _zoom, _screen.X, _screen.Y);
            DrawTextureRect(picture, new Rect2((float)at.X, (float)at.Y, far.Width * _zoom, height * _zoom), false);
        }
    }
}
