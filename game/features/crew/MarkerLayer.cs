using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// Arrows at the screen's edge to what this player may want to find again (<see cref="Markers"/>): home, the
/// lifepod's hatch where a hero is made again, the body this player left last, and each friend in its suit's
/// colour (0060). A chevron round a small
/// badge, turned toward its target, fading in once the target leaves the screen and gone the moment it is back;
/// kept out of the HUD's panels, and sized by the slots' size. How far, in cells, only by the pointer.
/// </summary>
public partial class MarkerLayer : CanvasLayer
{
    /// <summary>The badge's radius and the chevron's length beyond it at the normal slot size, in pixels; they grow with the slots.</summary>
    private const float Radius = 10, Chevron = 7;

    private readonly Ui _ui;
    private readonly ShipCamera _camera;
    private readonly Func<(HeroView Hero, Registry M, int Peer)?> _read;
    private readonly Func<IEnumerable<Rect2>> _keepOut;
    private readonly TipSpots _tips;
    private readonly Dictionary<(MarkerIcon, int?), Badge> _badges = new();
    private readonly HashSet<(MarkerIcon, int?)> _seen = [];
    private readonly List<Box> _boxes = [];
    private readonly Texture2D _pod = PodSilhouette();
    private Vector2? _mouse;

    /// <param name="ui">The slots' size, the menu open and the one tooltip.</param>
    /// <param name="camera">Where the world is on the screen, read as the hero has just moved it this frame.</param>
    /// <param name="read">This player's hero, placed, the world it is in and its peer; none outside a session.</param>
    /// <param name="keepOut">The HUD's panels on the screen this frame: no marker is drawn over them.</param>
    public MarkerLayer(Ui ui, ShipCamera camera, Func<(HeroView Hero, Registry M, int Peer)?> read, Func<IEnumerable<Rect2>> keepOut)
    {
        _ui = ui;
        _camera = camera;
        _read = read;
        _keepOut = keepOut;
        _tips = new TipSpots(ui.Tip);
        Layer = ScreenLayers.Hud;
    }

    /// <summary>The pod's picture, every pixel white and its clear ones clear, mipmapped so it shrinks to a badge cleanly.</summary>
    private static Texture2D PodSilhouette()
    {
        var image = GD.Load<Texture2D>("res://art/machine_lifepod.png").GetImage();
        if (image.IsCompressed()) image.Decompress();
        image.Convert(Image.Format.Rgba8);
        for (int y = 0; y < image.GetHeight(); y++)
            for (int x = 0; x < image.GetWidth(); x++)
                image.SetPixel(x, y, new Color(1, 1, 1, image.GetPixel(x, y).A));
        image.GenerateMipmaps();
        return ImageTexture.CreateFromImage(image);
    }

    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouse mouse) _mouse = mouse.Position;
    }

    public override void _Process(double delta)
    {
        _seen.Clear();
        if (_read() is var (hero, m, peer))
        {
            var friends = hero.Others.Select(o => (o.Id, o.Hero, Suits.Of(m.Crew, o.Id))).ToList();
            var body = Bodies.Latest(m.Items.Floor, peer) is { } pile ? (pile, Suits.Of(m.Crew, peer)) : ((Pile, int)?)null;
            Show(Markers.Of(m.Crew.Spawn, m.Grid.Width, friends, body), hero.Current.Middle, hero.PromptCovers, delta);
        }
        // Gone with its target: a friend who left, a body taken or gone, or the world.
        foreach (var key in _badges.Keys.Where(k => !_seen.Contains(k)).ToList())
        {
            _badges[key].QueueFree();
            _badges.Remove(key);
        }
        _tips.Frame(_ui.MenuOpen.Get() ? null : _mouse);
    }

    private void Show(List<Marker> markers, Point from, Rect2? prompt, double delta)
    {
        var screen = GetViewport().GetVisibleRect().Size;
        float scale = _ui.Slot / (float)Slots.SizeOf(SlotScale.Normal);
        float reach = (Radius + Chevron) * scale;
        // The marker's middle stays its reach and the HUD's margin in from the edge.
        float margin = Slots.Margin + reach;
        var inner = new Box(margin, margin, screen.X - margin, screen.Y - margin);
        _boxes.Clear();
        foreach (var r in prompt is { } p ? _keepOut().Append(p) : _keepOut()) _boxes.Add(new Box(r.Position.X, r.Position.Y, r.End.X, r.End.Y));
        double perCell = ShipCamera.Cell * _camera.CurrentZoom;
        double cameraX = _camera.Position.X / ShipCamera.Cell, cameraY = _camera.Position.Y / ShipCamera.Cell;
        double fromX = (double)from.X / Hero.Unit, fromY = (double)from.Y / Hero.Unit;
        foreach (var marker in markers)
        {
            var key = (marker.Icon, marker.Peer);
            _seen.Add(key);
            if (!_badges.TryGetValue(key, out var badge))
            {
                AddChild(badge = _badges[key] = new Badge(_pod));
                var made = badge;
                _tips.Add(badge, () => TipOf(made));
            }
            var (x, y) = Markers.ToScreen(marker.X, marker.Y, cameraX, cameraY, perCell, screen.X, screen.Y);
            var placed = Markers.Place(x, y, screen.X, screen.Y, inner, reach, _boxes);
            badge.Shown = Markers.Fade(badge.Shown, placed.OnScreen, delta);
            badge.Visible = badge.Shown > 0;
            if (!badge.Visible) continue;
            badge.Marker = marker;
            badge.Cells = Markers.CellsBetween(fromX, fromY, marker.X, marker.Y);
            badge.Angle = (float)placed.Angle;
            badge.Grown = scale;
            badge.Size = Vector2.One * 2 * reach;
            badge.Position = new Vector2((float)placed.X, (float)placed.Y) - badge.Size / 2;
            badge.Modulate = Colors.White with { A = (float)badge.Shown };
            badge.QueueRedraw();
        }
    }

    /// <summary>What the pointer says over a marker: what it points at and how many cells away.</summary>
    private static Tip? TipOf(Badge badge) => badge.Marker is { } marker
        ? new Tip(T(marker.Icon switch { MarkerIcon.Home => "markers.home", MarkerIcon.Body => "markers.body", _ => "markers.friend" }), new TipLine(T("markers.cells", ("cells", badge.Cells)), Tone.Dim))
        : null;

    /// <summary>One marker: a dark badge ringed in its colour holding its icon, and a chevron outside the ring toward the target.</summary>
    private sealed partial class Badge(Texture2D pod) : Control
    {
        private static readonly Color Back = Style.Slate950 with { A = 0.8f };

        public Marker? Marker { get; set; }
        public int Cells { get; set; }
        public float Angle { get; set; }
        public float Grown { get; set; } = 1;
        public double Shown { get; set; }

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            TextureFilter = TextureFilterEnum.LinearWithMipmaps;
        }

        public override void _Draw()
        {
            if (Marker is not { } marker) return;
            var colour = new Color((uint)marker.Colour << 8 | 0xff);
            var middle = Size / 2;
            float r = Radius * Grown, s = Grown;
            var toward = Vector2.FromAngle(Angle);
            var side = toward.Orthogonal();
            // An arrowhead notched at its back, its tip at the reach toward the target.
            var tip = middle + toward * (r + Chevron * s);
            var back = middle + toward * (r + 1.5f * s);
            DrawColoredPolygon([tip, back + side * 5.5f * s, back + toward * 2 * s, back - side * 5.5f * s], colour);
            DrawCircle(middle, r, Back);
            DrawArc(middle, r - 0.75f * s, 0, Mathf.Tau, 32, colour, 1.5f * s, antialiased: true);
            if (marker.Icon == MarkerIcon.Home)
            {
                float icon = 1.15f * r;
                DrawTextureRect(pod, new Rect2(middle - Vector2.One * icon / 2, Vector2.One * icon), false, colour);
            }
            else if (marker.Icon == MarkerIcon.Body)
            {
                // A figure lying on its back, its head to the left: the body, at a glance.
                DrawLine(middle + new Vector2(-0.25f, 0.18f) * r, middle + new Vector2(0.62f, 0.18f) * r, colour, 0.34f * r, antialiased: true);
                DrawCircle(middle + new Vector2(-0.48f, 0.08f) * r, 0.24f * r, colour);
                DrawCircle(middle + new Vector2(0.12f, -0.36f) * r, 0.13f * r, colour);
            }
            else DrawCircle(middle, 0.45f * r, colour);
        }
    }
}
