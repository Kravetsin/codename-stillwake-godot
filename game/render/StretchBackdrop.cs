using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// A stretch of space's far picture (<see cref="StretchLooks.Backdrop"/>): a row of places far behind the far wrecks, each a picture or
/// a scatter of pieces, passing right to left at its own steady speed from the warning's start, as if the ship sailed into it, and out
/// past the left edge once the stretch has gone. Drawn once in its plane's pixels and carried by this node's transform, like a
/// <see cref="DriftLayer"/>: it draws again only when a place comes into sight or leaves it, or the zoom changes. Unlit, on the stars'
/// canvas layer.
/// </summary>
public partial class StretchBackdrop : Node2D
{

    // The look drawn: kept past the sky's own while its row passes out.
    private StretchLooks.Look? _look;
    private Texture2D[] _pictures = [];
    private Texture2D? _atlas;
    private bool _ending;
    private int _since;
    // Set when it first shows: where the row's leading edge lies on its plane and its middle's height; its seed.
    private double? _lead;
    private double _middle;
    private uint _seed;
    // The row's last place once the stretch has gone.
    private long? _last;
    private (long First, long Last) _places = (0, -1);
    private readonly List<StretchLooks.Spot> _spots = [];

    public StretchBackdrop()
    {
        TextureFilter = TextureFilterEnum.LinearWithMipmaps;
    }

    /// <summary>
    /// The stretch shown (<see cref="StretchShown"/>): <paramref name="look"/>, <paramref name="grow"/> of its warning run and
    /// <paramref name="since"/> ticks since it began, gone once <paramref name="melt"/> is past 0. Its row starts with its warning, the
    /// same picked and in the same place for every peer and after a load, and passes out after it has gone.
    /// </summary>
    public void Show(StretchLooks.Look? look, float grow, float melt, int since)
    {
        if (look != null && melt <= 0)
        {
            if (look != _look || _ending) Start(look);
        }
        else if (_look != null) _ending = true;
        if (!_ending) _since = since;
        Visible = _look != null && (_ending || grow > 0);
    }

    /// <summary>None at once, the row's passing out too: on a planet, whose sky is its own.</summary>
    public void Clear() => (_look, _pictures, _atlas, _ending, Visible) = (null, [], null, false, false);

    private void Start(StretchLooks.Look look)
    {
        (_look, _lead, _last, _ending, _places) = (look, null, null, false, (0, -1));
        _pictures = look.Far.Pictures.Select(PixelArt.Far).ToArray();
        _atlas = look.Far.Scattered is { } field ? ImageTexture.CreateFromImage(HiRes.Mipped(HiRes.Load(field.Atlas)!)) : null;
        var veil = look.Far.Scattered?.Veil ?? StretchLooks.Veil;
        Modulate = new Color(veil.R, veil.G, veil.B);
    }

    /// <summary>Lays the row under a camera centred on <paramref name="camera"/> at <paramref name="zoom"/>, over a <paramref name="screen"/>, <paramref name="seconds"/> of the world on.</summary>
    public void Follow(Vector2 camera, float zoom, Vector2 screen, double seconds)
    {
        if (!Visible || _look == null) return;
        var far = _look.Far;
        zoom = Mathf.Max(zoom, ShipCamera.FarthestZoom(screen));
        var centre = Scatter.Centre(camera.X, camera.Y, StretchLooks.Parallax, far.Speed, seconds);
        double half = screen.X / 2 / zoom, period = far.Width * (1 + far.Gap);
        if (_lead == null)
        {
            // Where it was as the warning started, its leading edge at the right edge: a peer joining later sees it where the others do.
            double since = (double)_since / Stillwake.Sim.World.TicksPerSecond;
            (_lead, _middle, _seed) = (centre.X + half - far.Speed * since, centre.Y - far.Above * screen.Y / zoom,
                (uint)(Math.Floor(seconds * Stillwake.Sim.World.TicksPerSecond + 1e-6) - _since));
        }
        long right = (long)Math.Floor((centre.X + half - _lead.Value) / period);
        if (_ending) _last ??= right;
        var places = (Math.Max(0, (long)Math.Ceiling((centre.X - half - far.Width - _lead.Value) / period)), Math.Min(right, _last ?? long.MaxValue));
        if (_ending && places.Item1 > places.Item2)
        {
            Clear();
            return;
        }
        if (places != _places || zoom != Scale.X)
        {
            _places = places;
            QueueRedraw();
        }
        var at = Scatter.OnScreen(_lead.Value, _middle, centre, zoom, screen.X, screen.Y);
        Position = new Vector2((float)at.X, (float)at.Y);
        Scale = new Vector2(zoom, zoom);
    }

    // In the plane's pixels from the row's leading edge at its middle.
    public override void _Draw()
    {
        if (_look == null) return;
        var far = _look.Far;
        double period = far.Width * (1 + far.Gap);
        for (long place = _places.First; place <= _places.Last; place++)
        {
            float x = (float)(place * period);
            if (far.Scattered is { } field && _atlas != null) DrawField(field, place, x);
            else if (_pictures.Length > 0)
            {
                var picture = _pictures[StretchLooks.PictureAt(place, _seed, _pictures.Length)];
                float height = far.Width * picture.GetHeight() / picture.GetWidth();
                DrawTextureRect(picture, new Rect2(x, -height / 2, far.Width, height), false);
            }
        }
        DrawSetTransformMatrix(Transform2D.Identity);
    }

    private void DrawField(StretchLooks.Field field, long place, float x)
    {
        _spots.Clear();
        StretchLooks.PiecesAt(field, place, _seed, _look!.Far.Width, _spots);
        foreach (var spot in _spots)
        {
            var (px, py, w, h) = field.Pieces[spot.Piece];
            var size = new Vector2(w, h) * spot.Scale;
            DrawSetTransform(new Vector2(x + spot.X, spot.Y), spot.Turn, new Vector2(spot.Mirror ? -1 : 1, 1));
            DrawTextureRectRegion(_atlas!, new Rect2(-size / 2, size), new Rect2(px, py, w, h));
        }
    }
}
