using System;
using System.Linq;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// A stretch of space's far picture (<see cref="StretchLooks.Backdrop"/>): a row of its pictures far behind the far wrecks that sails in
/// from the right edge as the warning runs and out past the left as it eases back, as if the ship passed into it. Laid where the
/// camera looks when it first shows, it follows <see cref="StretchLooks.Parallax"/> of the camera and drifts left. Unlit, on the stars'
/// canvas layer; a few pictures a frame.
/// </summary>
public partial class StretchBackdrop : Node2D
{
    private StretchLooks.Look? _look;
    private Texture2D[] _pictures = [];
    private float _grow, _melt;
    // Set when it first shows: where the row's leading edge comes to rest on its plane, how far it sails in and out, its seed.
    private (double X, double Y)? _rest;
    private double _in, _out;
    private uint _seed;
    // Its last place once it starts leaving: the row ends there, so its trailing edge sails out too.
    private long? _last;
    private (double X, double Y) _centre;
    private float _zoom = 1;
    private Vector2 _screen;

    public StretchBackdrop()
    {
        TextureFilter = TextureFilterEnum.LinearWithMipmaps;
        var veil = StretchLooks.Veil;
        Modulate = new Color(veil.R, veil.G, veil.B);
    }

    /// <summary>Shows <paramref name="look"/>'s far picture <paramref name="grow"/> of its warning in and <paramref name="melt"/> of its easing back out.</summary>
    public void Show(StretchLooks.Look? look, float grow, float melt)
    {
        if (look != _look)
        {
            _look = look;
            _pictures = look?.Far.Pictures.Select(PixelArt.Far).ToArray() ?? [];
            _rest = null;
        }
        if (melt <= 0) _last = null;
        (_grow, _melt) = (grow, melt);
        Visible = look != null && grow > 0 && melt < 1;
    }

    /// <summary>Lays the row under a camera centred on <paramref name="camera"/> at <paramref name="zoom"/>, <paramref name="seconds"/> of the world on.</summary>
    public void Follow(Vector2 camera, float zoom, Vector2 screen, double seconds)
    {
        if (!Visible || _look == null) return;
        zoom = Mathf.Max(zoom, ShipCamera.FarthestZoom(screen));
        var far = _look.Far;
        _centre = Scatter.Centre(camera.X, camera.Y, StretchLooks.Parallax, far.Drift, seconds);
        if (_rest == null)
        {
            double width = screen.X / zoom;
            _rest = (_centre.X + far.Rest * width, _centre.Y - StretchLooks.Above * screen.Y / zoom);
            // In from just past the right edge; out till the trailing edge, at most a picture past the right edge, is past the left.
            (_in, _out, _seed) = ((0.5 - far.Rest) * width, (0.5 - far.Rest) * width + width / 2 + far.Width, (uint)seconds);
        }
        (_zoom, _screen) = (zoom, screen);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_look == null || _rest is not { } rest || _pictures.Length == 0) return;
        var far = _look.Far;
        var (sailedIn, sailedOut) = StretchLooks.Sailed(_grow, _melt);
        double lead = rest.X + (1 - sailedIn) * _in - sailedOut * _out;
        double period = far.Width * (1 + far.Gap), half = _screen.X / 2 / _zoom;
        long right = (long)Math.Floor((_centre.X + half - lead) / period);
        if (_melt > 0) _last ??= right;
        long first = Math.Max(0, (long)Math.Floor((_centre.X - half - far.Width - lead) / period));
        long last = Math.Min(right, _last ?? long.MaxValue);
        for (long place = first; place <= last; place++)
        {
            var picture = _pictures[StretchLooks.PictureAt(place, _seed, _pictures.Length)];
            float height = far.Width * picture.GetHeight() / picture.GetWidth();
            var at = Scatter.OnScreen(lead + place * period, rest.Y - height / 2, _centre, _zoom, _screen.X, _screen.Y);
            DrawTextureRect(picture, new Rect2((float)at.X, (float)at.Y, far.Width * _zoom, height * _zoom), false);
        }
    }
}
