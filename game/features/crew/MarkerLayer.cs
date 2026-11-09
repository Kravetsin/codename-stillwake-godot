using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Creatures;
using Stillwake.Sim.Debris;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// Arrows at the screen's edge to what this player may want to find again (<see cref="Markers"/>): home, the
/// lifepod's hatch where a hero is made again, the body this player left last, and each friend in its suit's
/// colour (0061). A chevron round a small
/// badge, turned toward its target (and the catcher hunting this player, in red), fading in once the target leaves the screen and gone the moment it is back;
/// kept out of the HUD's panels, and sized by the slots' size. How far, in cells, only by the pointer.
/// </summary>
public partial class MarkerLayer : CanvasLayer
{
    /// <summary>The badge's radius and the chevron's length beyond it at the normal slot size, in pixels; they grow with the slots.</summary>
    private const float Radius = 10, Chevron = 7;
    /// <summary>How long a lost delivery's marker stays, red, fading.</summary>
    private const double LostS = 2.5;

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
    /// <summary>The deliveries lost as last read: one more, and a delivery marker gone this frame was lost.</summary>
    private int _lost;

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
        var image = HiRes.Load("machine_lifepod")!;
        for (int y = 0; y < image.GetHeight(); y++)
            for (int x = 0; x < image.GetWidth(); x++)
                image.SetPixel(x, y, new Color(1, 1, 1, image.GetPixel(x, y).A));
        return HiRes.Texture(image);
    }

    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouse mouse) _mouse = mouse.Position;
    }

    public override void _Process(double delta)
    {
        _seen.Clear();
        bool lost = false;
        if (_read() is var (hero, m, peer))
        {
            var friends = hero.Others.Select(o => (o.Id, o.Hero, Suits.Of(m.Players, o.Id))).ToList();
            var body = Bodies.Latest(m.Items.Floor, peer) is { } pile ? (pile, Suits.Of(m.Players, peer)) : ((Pile, int)?)null;
            Show(Markers.Of(m.Crew.Spawn, m.Grid.Width, friends, body, DeliveriesOf(m), HunterOf(m, peer)), hero.Current.Middle, hero.PromptCovers, delta);
            lost = m.Knowledge.Lost > _lost;
            _lost = m.Knowledge.Lost;
        }
        // Gone with its target: a friend who left, a body taken or gone, or the world. A delivery that drifted past instead
        // turns red where it was pointed at and fades (2026-11-08-mafia-deliveries).
        foreach (var key in _badges.Keys.Where(k => !_seen.Contains(k)).ToList())
        {
            var badge = _badges[key];
            if (key.Item1 == MarkerIcon.Delivery && badge.Visible && (badge.Lost > 0 || lost))
            {
                badge.Lost += delta;
                badge.Modulate = Colors.White with { A = (float)Math.Max(0, 1 - badge.Lost / LostS) };
                badge.QueueRedraw();
                if (badge.Lost < LostS) continue;
            }
            badge.QueueFree();
            _badges.Remove(key);
        }
        _tips.Frame(_ui.MenuOpen.Get() ? null : _mouse);
    }

    /// <summary>The catcher hunting <paramref name="peer"/>, its middle in cells (tracker-catchers); none while none hunts it.</summary>
    private static (int Id, double X, double Y)? HunterOf(Registry m, int peer)
    {
        if (m.Creatures.HunterOf(peer) is not { } c) return null;
        double unit = CreaturesModule.Unit, height = Stillwake.Content.Creatures.Get(c.Species).Body.Height;
        return (c.Id, c.X / unit, c.Y / unit - height / 2);
    }

    /// <summary>The mafia's deliveries out, where each piece is (cells): the pieces walked only while one is on its way.</summary>
    private static List<(int Id, string Good, double X, double Y)>? DeliveriesOf(Registry m)
    {
        var deliveries = m.Knowledge.Deliveries;
        if (deliveries.Count == 0) return null;
        var list = new List<(int, string, double, double)>(deliveries.Count);
        foreach (var piece in m.Debris.Pieces)
            if (piece.Delivery != 0 && deliveries.FirstOrDefault(d => d.Id == piece.Delivery) is { } delivery)
                list.Add((piece.Id, delivery.Good, (double)piece.X / DebrisModule.Unit, (double)piece.Y / DebrisModule.Unit));
        return list;
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
        ? new Tip(T(marker.Icon switch { MarkerIcon.Home => "markers.home", MarkerIcon.Body => "markers.body", MarkerIcon.Delivery => badge.Lost > 0 ? "markers.lost" : "markers.delivery", MarkerIcon.Hunter => "markers.hunter", _ => "markers.friend" }), new TipLine(T("markers.cells", ("cells", badge.Cells)), Tone.Dim))
        : null;

    /// <summary>One marker: a dark badge ringed in its colour holding its icon, and a chevron outside the ring toward the target.</summary>
    private sealed partial class Badge(Texture2D pod) : Control
    {
        /// <summary>The side a tool's icon in a delivery's badge is baked at: the badge at its largest.</summary>
        private const int IconBake = 48;

        private static readonly Color Back = Style.Slate950 with { A = 0.8f };

        public Marker? Marker { get; set; }
        public int Cells { get; set; }
        public float Angle { get; set; }
        public float Grown { get; set; } = 1;
        public double Shown { get; set; }
        /// <summary>Seconds since its delivery drifted past: drawn red while it fades; 0 while it is out.</summary>
        public double Lost { get; set; }

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            TextureFilter = TextureFilterEnum.LinearWithMipmaps;
        }

        public override void _Draw()
        {
            if (Marker is not { } marker) return;
            var colour = Lost > 0 ? Style.Red300 : new Color((uint)marker.Colour << 8 | 0xff);
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
            else if (marker.Icon == MarkerIcon.Delivery && marker.Good is { } good)
            {
                float icon = 1.2f * r;
                DrawTextureRect(GoodIcons.Of(good, IconBake), new Rect2(middle - Vector2.One * icon / 2, Vector2.One * icon), false);
            }
            else if (marker.Icon == MarkerIcon.Body)
            {
                // A figure lying on its back, its head to the left: the body, at a glance.
                DrawLine(middle + new Vector2(-0.25f, 0.18f) * r, middle + new Vector2(0.62f, 0.18f) * r, colour, 0.34f * r, antialiased: true);
                DrawCircle(middle + new Vector2(-0.48f, 0.08f) * r, 0.24f * r, colour);
                DrawCircle(middle + new Vector2(0.12f, -0.36f) * r, 0.13f * r, colour);
            }
            else if (marker.Icon == MarkerIcon.Hunter)
            {
                // A reticle: a ring and four ticks in toward its middle, what hunts this player.
                DrawArc(middle, 0.42f * r, 0, Mathf.Tau, 24, colour, 0.14f * r, antialiased: true);
                foreach (var way in (Vector2[])[Vector2.Up, Vector2.Down, Vector2.Left, Vector2.Right])
                    DrawLine(middle + way * 0.18f * r, middle + way * 0.7f * r, colour, 0.14f * r, antialiased: true);
            }
            else DrawCircle(middle, 0.45f * r, colour);
        }
    }
}
