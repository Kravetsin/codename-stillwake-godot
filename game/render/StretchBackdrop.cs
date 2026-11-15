using System;
using System.Linq;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// A stretch of space's far picture (<see cref="StretchLooks.Backdrop"/>): a row of its pictures far behind the far wrecks, passing right
/// to left at its own steady speed from the warning's start, as if the ship sailed into it, and out past the left edge once the
/// stretch has gone. Drawn at the farthest zoom whatever the camera's, as something far off barely grows as one nears it, so its
/// pictures keep their detail. Unlit, on the stars' canvas layer; a few pictures a frame.
/// </summary>
public partial class StretchBackdrop : Node2D
{
    private static readonly double WarningSeconds = (double)Content.Stretches.Warning / Stillwake.Sim.World.TicksPerSecond;

    // The look drawn: kept past the sky's own while its row passes out.
    private StretchLooks.Look? _look;
    private Texture2D[] _pictures = [];
    private bool _ending;
    private float _grow;
    // Set when it first shows: where the row's leading edge lies on its plane and its middle's height; its seed.
    private double? _lead;
    private double _middle;
    private uint _seed;
    // The row's last place once the stretch has gone.
    private long? _last;
    private (double X, double Y) _centre;
    private float _zoom = 1;
    private Vector2 _screen;
    private (long First, long Last) _places;

    public StretchBackdrop()
    {
        TextureFilter = TextureFilterEnum.LinearWithMipmaps;
        var veil = StretchLooks.Veil;
        Modulate = new Color(veil.R, veil.G, veil.B);
    }

    /// <summary>
    /// The stretch shown (<see cref="StretchShown"/>): <paramref name="look"/>, <paramref name="grow"/> of its warning run, gone once
    /// <paramref name="melt"/> is past 0. Its row starts with its warning and passes out after it has gone.
    /// </summary>
    public void Show(StretchLooks.Look? look, float grow, float melt)
    {
        if (look != null && melt <= 0)
        {
            if (look != _look || _ending) (_look, _pictures, _lead, _last, _ending) = (look, look.Far.Pictures.Select(PixelArt.Far).ToArray(), null, null, false);
        }
        else if (_look != null) _ending = true;
        _grow = grow;
        Visible = _look != null && (_ending || grow > 0);
    }

    /// <summary>None at once, the row's passing out too: on a planet, whose sky is its own.</summary>
    public void Clear() => (_look, _pictures, _ending, Visible) = (null, [], false, false);

    /// <summary>Lays the row under a camera centred on <paramref name="camera"/>, over a <paramref name="screen"/>, <paramref name="seconds"/> of the world on.</summary>
    public void Follow(Vector2 camera, Vector2 screen, double seconds)
    {
        if (!Visible || _look == null) return;
        var far = _look.Far;
        _zoom = ShipCamera.FarthestZoom(screen);
        _screen = screen;
        _centre = Scatter.Centre(camera.X, camera.Y, StretchLooks.Parallax, far.Speed, seconds);
        double half = screen.X / 2 / _zoom, period = far.Width * (1 + far.Gap);
        if (_lead == null)
        {
            // Where it was as the warning started, its leading edge at the right edge: a peer joining later sees it where the others do.
            double since = Math.Clamp(_grow, 0, 1) * WarningSeconds;
            (_lead, _middle, _seed) = (_centre.X + half - far.Speed * since, _centre.Y - StretchLooks.Above * screen.Y / _zoom, (uint)(seconds - since));
        }
        long right = (long)Math.Floor((_centre.X + half - _lead.Value) / period);
        if (_ending) _last ??= right;
        _places = (Math.Max(0, (long)Math.Ceiling((_centre.X - half - far.Width - _lead.Value) / period)), Math.Min(right, _last ?? long.MaxValue));
        if (_ending && _places.First > _places.Last) (_look, _pictures, Visible) = (null, [], false);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_look == null || _lead is not { } lead) return;
        var far = _look.Far;
        double period = far.Width * (1 + far.Gap);
        for (long place = _places.First; place <= _places.Last; place++)
        {
            var picture = _pictures[StretchLooks.PictureAt(place, _seed, _pictures.Length)];
            float height = far.Width * picture.GetHeight() / picture.GetWidth();
            var at = Scatter.OnScreen(lead + place * period, _middle - height / 2, _centre, _zoom, _screen.X, _screen.Y);
            DrawTextureRect(picture, new Rect2((float)at.X, (float)at.Y, far.Width * _zoom, height * _zoom), false);
        }
    }
}
