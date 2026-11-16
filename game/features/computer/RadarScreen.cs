using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Content;
using Stillwake.Game.Features.Research;
using Stillwake.Game.Shared;
using Stillwake.Sim.Places;
using static Stillwake.Game.I18n.En;
using LandingData = Stillwake.Content.Landing;

namespace Stillwake.Game.Features.Computer;

/// <summary>
/// The radar program, over the computer's whole screen (solar-system, its picked look): the system from above in the tablet's ink,
/// the sun in the middle and its planets on their orbits, zoomed by the wheel down to the ship's planet, where the ship's orbit
/// and the contacts on it show (<see cref="NavMap"/>). The pointer names what it is over; a click opens a callout beside the thing,
/// held to it by a line of ink, with its facts and its action: a course to a planet, a jump to a contact. Esc or a click on empty
/// map puts it away. The map moves by real time and the player's hand only: a look, never the sim's.
/// </summary>
public partial class RadarScreen : Control
{
    private static readonly Color GlassTop = new("111a2c"), GlassBottom = new("0c1322"), Ink = new("d8e0ec"), Drawn = new("b4bfd0"),
        Faint = new(0.59f, 0.65f, 0.75f, 0.22f), Mute = new("8c97ab"), DimText = new("5b6579"), Text = new("dfe5ee"), Loop = new("f2b84b"),
        ShipColour = new("a9dcec"), Amber = new("ffb02e"), Warn = new("d79a6a"), Card = new(0.035f, 0.055f, 0.094f, 0.95f), Hull = new("1b2333");

    /// <summary>A contact with fewer ticks than this left blinks: going soon (2 min).</summary>
    private const int Going = 2 * 60 * Sim.World.TicksPerSecond;
    /// <summary>Above this scale the ship's orbit and its contacts show.</summary>
    private const float NearFrom = 4;
    private const int Small = 13, Tiny = 11, Title = 15;

    private readonly Ui _ui;
    private readonly List<Hit> _hits = [], _drawn = [];
    private float _zoom, _zoomTo, _along, _time, _opened;
    private int _focus;
    private Vector2 _pan;
    private Vector2? _press;
    private bool _dragged;
    private int? _planet;

    /// <summary>The system planet the ship is at (<c>LandingModule.At</c>).</summary>
    public int At { get; set; }

    /// <summary>The ship's course, where to and its ticks left; null standing.</summary>
    public (int To, int TicksLeft)? Course { get; set; }

    /// <summary>The contacts up, as the panel last read them.</summary>
    public IReadOnlyList<Contact> Contacts { get; set; } = [];

    /// <summary>Why no course can be set to each system planet, as the panel last read it: null where one can.</summary>
    public IReadOnlyList<string?> Problems { get; set; } = [];

    /// <summary>Sets a course to a system planet: <c>landing</c>'s <c>SetCourse</c>.</summary>
    public Action<int>? SetCourse { get; set; }

    /// <summary>Jumps to a contact by id: <c>places</c>' <c>Jump</c>.</summary>
    public Action<int>? Jump { get; set; }

    public RadarScreen(Ui ui)
    {
        _ui = ui;
        MouseFilter = MouseFilterEnum.Stop;
        ClipContents = true;
    }

    /// <summary>Opens on the whole system, nothing selected.</summary>
    public void Reset()
    {
        (_zoom, _zoomTo, _pan, _planet, _focus) = (0, 0, Vector2.Zero, null, At);
        _ui.RadarContact.Set(null);
    }

    /// <summary>Puts an open callout away: Esc's first step. Whether there was one.</summary>
    public bool LetGo()
    {
        if (_planet == null && _ui.RadarContact.Get() == null) return false;
        Select(null, null);
        return true;
    }

    /// <summary>The tooltip for what lies under <paramref name="global"/>, a point on the screen; none elsewhere.</summary>
    public Tip? TipAt(Vector2 global) => HitAt(GetGlobalTransform().AffineInverse() * global)?.Tip;

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree()) return;
        float dt = Mathf.Min((float)delta, 0.05f), k = Mathf.Min(1, dt * 7);
        _time += dt;
        _zoom = Mathf.Abs(_zoomTo - _zoom) < 0.001f ? _zoomTo : _zoom + (_zoomTo - _zoom) * k;
        if (_zoom == 0 && Course == null) _focus = At;
        float along = Course is { } c ? 1 - c.TicksLeft / (float)LandingData.FlightTicks : 0;
        // The mark glides between reads rather than stepping each quarter second.
        _along = Course == null ? 0 : _along + (along - _along) * Mathf.Min(1, dt * 4);
        // A contact selected elsewhere (a demo) brings the map down to it.
        if (_ui.RadarContact.Get() != null && _zoomTo < 1 && Course == null) (_zoomTo, _focus) = (1, At);
        if (_planet != null && Course != null && _planet != Course.Value.To && _planet == At) _planet = null;
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp }:
                Zoom(0.2f);
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelDown }:
                Zoom(-0.2f);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press:
                (_press, _dragged) = (press.Position, false);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } release:
                if (!_dragged) Click(release.Position);
                _press = null;
                break;
            case InputEventMouseMotion motion when _press is { } from && (motion.ButtonMask & MouseButtonMask.Left) != 0:
                if (!_dragged && from.DistanceTo(motion.Position) > 5) _dragged = true;
                if (_dragged) _pan = (_pan + motion.Relative).Clamp(-Size, Size);
                break;
            default:
                return;
        }
        AcceptEvent();
    }

    private void Click(Vector2 at)
    {
        if (HitAt(at) is { } hit) hit.Click?.Invoke();
        else Select(null, null);
    }

    private void Zoom(float by)
    {
        // Between planets there is nothing near to look at.
        if (by > 0 && Course != null) return;
        _zoomTo = Mathf.Clamp(_zoomTo + by, 0, 1);
    }

    private void ZoomTo(int planet) => (_focus, _zoomTo, _pan) = (planet, 1, Vector2.Zero);

    private void Select(int? planet, int? contact)
    {
        if (planet != _planet || contact != _ui.RadarContact.Get()) _opened = _time;
        _planet = planet;
        _ui.RadarContact.Set(contact);
    }

    private Hit? HitAt(Vector2 at)
    {
        for (int i = _drawn.Count - 1; i >= 0; i--)
            if (_drawn[i].Holds(at)) return _drawn[i];
        return null;
    }

    private float ScaleNow => NavMap.Scale(_zoom);
    private float NearShown => Course == null && _focus == At ? Mathf.Clamp((ScaleNow - NearFrom) / 4, 0, 1) : 0;

    private Vector2 Screen((float X, float Y) at)
    {
        var (x, y) = NavMap.ToScreen(at, NavMap.Planet(_focus), _zoom, Size.X, Size.Y, (_pan.X, _pan.Y));
        return new Vector2(x, y);
    }

    private float Px(float units) => units * NavMap.Fit(Size.X, Size.Y) * ScaleNow;

    public override void _Draw()
    {
        _hits.Clear();
        Glass();
        var sun = Screen((0, 0));
        DrawOrbits(sun);
        DrawSun(sun);
        DrawCourse(sun);
        DrawPlanets();
        DrawNear();
        DrawShip(sun);
        DrawChrome();
        DrawCallout();
        _drawn.Clear();
        _drawn.AddRange(_hits);
    }

    private void Glass()
    {
        var size = Size;
        DrawPolygon([Vector2.Zero, new(size.X, 0), size, new(0, size.Y)], [GlassTop, GlassTop, GlassBottom, GlassBottom]);
        var glare = new Color(0.69f, 0.78f, 0.93f, 0.035f);
        DrawColoredPolygon([new(190, 0), new(262, 0), new(0, 350), new(0, 254)], glare);
        DrawColoredPolygon([new(282, 0), new(300, 0), new(0, 402), new(0, 378)], glare with { A = 0.025f });
    }

    private void DrawOrbits(Vector2 sun)
    {
        for (int i = 0; i < LandingData.Planets.Count; i++) Dotted(sun, Px(NavMap.Orbit(i)), Faint);
    }

    private void DrawSun(Vector2 at)
    {
        if (!new Rect2(Vector2.Zero, Size).Grow(200).HasPoint(at)) return;
        float r = 15 * Mathf.Min(ScaleNow, 3), pulse = 1 + 0.04f * Mathf.Sin(_time * 1.4f);
        for (int i = 6; i >= 1; i--) DrawCircle(at, r * (1 + 0.37f * i) * pulse, new Color(0.89f, 0.69f, 0.38f, 0.05f));
        DrawCircle(at, r, new Color("c79a56"));
        DrawCircle(at - new Vector2(r, r) * 0.25f, r * 0.6f, new Color("f1dfae") with { A = 0.6f });
        DrawArc(at, r, 0, Mathf.Tau, 48, new Color(1, 0.94f, 0.82f, 0.55f), 1);
        _hits.Add(Hit.Round(at, 18, null, new Tip(T("computer.radar.sun"), [])));
    }

    private void DrawCourse(Vector2 sun)
    {
        int from, to;
        if (Course is { } c) (from, to) = (At, c.To);
        else if (_planet is int p && p != At) (from, to) = (At, p);
        else return;
        var points = CoursePoints(sun, from, to, 1);
        Dashed(points, Course == null ? Ink with { A = 0.86f } : Drawn with { A = 0.6f });
        if (Course != null && _along > 0) DrawPolyline(CoursePoints(sun, from, to, _along), ShipColour with { A = 0.65f }, 1.6f, true);
        else if (ScaleNow < 3)
        {
            var mid = points[points.Length / 2];
            Words(T("computer.radar.seconds", ("s", LandingData.FlightTicks / Sim.World.TicksPerSecond)), mid + new Vector2(6, -6), Tiny, Text);
        }
    }

    private Vector2[] CoursePoints(Vector2 sun, int from, int to, float upTo)
    {
        var (a, b) = (Screen(NavMap.Planet(from)), Screen(NavMap.Planet(to)));
        var points = new Vector2[31];
        for (int i = 0; i <= 30; i++)
        {
            var (x, y) = NavMap.Course((a.X, a.Y), (b.X, b.Y), (sun.X, sun.Y), upTo * i / 30);
            points[i] = new Vector2(x, y);
        }
        return points;
    }

    private void DrawPlanets()
    {
        for (int i = 0; i < LandingData.Planets.Count; i++)
        {
            var at = Screen(NavMap.Planet(i));
            float r = Px(NavMap.PlanetRadius);
            if (!new Rect2(Vector2.Zero, Size).Grow(r + 60).HasPoint(at)) continue;
            int planet = i;
            var tip = _planet == i ? null : new Tip(PlanetName(i), [new TipLine(T($"computer.radar.type.{LandingData.Planets[i].Type}")),
                new TipLine(i == At && Course == null ? T("computer.radar.here") : T("computer.radar.flight", ("s", LandingData.FlightTicks / Sim.World.TicksPerSecond)))]);
            var hit = Hit.Round(at, Mathf.Max(14, r + 4), () => Select(planet, null), tip);
            if (ScaleNow < 3 && Hovered(hit)) DrawArc(at, r + 6, 0, Mathf.Tau, 48, Faint with { A = 0.38f }, 1);
            DrawPlanet(at, r, LandingData.Planets[i].Type);
            if (ScaleNow < 3) Words((i + 1).ToString(), at + new Vector2(r + 3, -r - 1), 10, Mute);
            _hits.Add(hit);
            if (_planet == i) HandLoop(at, r + 10, r + 9, i + 3);
        }
    }

    private void DrawNear()
    {
        float shown = NearShown;
        if (shown <= 0.01f) return;
        var planet = Screen(NavMap.Planet(At));
        float orbit = Px(NavMap.PlanetRadius + NavMap.OrbitGap);
        Dotted(planet, orbit, Faint with { A = 0.38f * shown });
        Words(T("computer.radar.shipOrbit"), planet + new Vector2(orbit * 0.72f + 6, -orbit * 0.72f), 10, DimText with { A = shown });
        foreach (var c in Contacts)
        {
            var at = Screen(NavMap.Contact(At, c.Offset));
            bool going = c.Left < Going, blink = going && Mathf.Sin(_time * 4.4f) > 0;
            var cls = Vessels.Class(c.Class);
            float wide = cls?.Length >= 150 ? 34 : 20;
            var hit = Hit.Round(at, 20, () => Select(null, c.Id), _ui.RadarContact.Get() == c.Id ? null
                : new Tip(ClassName(c.Class), [new TipLine(T("computer.radar.km", ("n", RadarOrbit.Km(c.Offset)))), new TipLine(Leaves(c.Left), going ? Tone.Bad : Tone.Plain)]));
            Silhouette(at, wide, c.Class, blink, shown);
            Words(T("computer.radar.km", ("n", RadarOrbit.Km(c.Offset))), at + new Vector2(0, 22), Tiny, (Hovered(hit) ? Colors.White : Text) with { A = shown }, true);
            if (going) Words(Clock(c.Left), at + new Vector2(0, 36), Tiny, Amber with { A = shown }, true);
            if (shown > 0.5f) _hits.Add(hit);
            if (_ui.RadarContact.Get() == c.Id) HandLoop(at, 27, 22, c.Id);
        }
    }

    private void DrawShip(Vector2 sun)
    {
        Vector2 at;
        float angle = 0;
        if (Course is { } c)
        {
            var points = CoursePoints(sun, At, c.To, Mathf.Max(_along, 0.001f));
            at = points[^1];
            angle = (points[^1] - points[^2]).Angle();
        }
        else at = Screen(NavMap.Ship(At));
        var shape = new[] { new Vector2(8, 0), new Vector2(-6, -5), new Vector2(-3, 0), new Vector2(-6, 5) };
        for (int i = 0; i < shape.Length; i++) shape[i] = at + shape[i].Rotated(angle);
        DrawColoredPolygon(shape, ShipColour);
        DrawPolyline([.. shape, shape[0]], new Color(0.03f, 0.05f, 0.09f, 0.9f), 1, true);
        _hits.Add(Hit.Round(at, 8, null, new Tip(T("computer.radar.ship"), [new TipLine(Course != null ? T("computer.radar.inFlight") : T("computer.radar.atPlanet", ("n", At + 1)))])));
        // While the whole system shows, a tag under the ship's planet says what is near and zooms down to it.
        float far = 1 - Mathf.Clamp((ScaleNow - 1.5f) / 1.5f, 0, 1);
        if (Course != null || far < 0.05f) return;
        var planet = Screen(NavMap.Planet(At));
        float r = Px(NavMap.PlanetRadius);
        string words = T("computer.radar.near", ("n", Contacts.Count));
        float wide = Width(words, Tiny) + 20;
        var box = new Rect2(planet.X < sun.X ? planet.X - r - 16 - wide : planet.X + r + 16, planet.Y + r + 4, wide, 20);
        var tag = Hit.Box(box, () => ZoomTo(At), new Tip(T("computer.radar.nearTitle"), [new TipLine(T("computer.radar.nearHow"))]));
        DrawRect(box, Card with { A = 0.8f * far });
        SketchBox(box, 62, (Hovered(tag) ? Ink : Faint with { A = 0.5f }) with { A = far }, 1.2f);
        Words(words, box.GetCenter() + new Vector2(0, 4), Tiny, (Hovered(tag) ? Text : Mute) with { A = far }, true);
        _hits.Add(tag);
    }

    /// <summary>Where the map is and how to move it: the breadcrumb, the zoom's buttons and gauge, a hint.</summary>
    private void DrawChrome()
    {
        bool deep = _zoom >= 0.5f;
        var system = Hit.Box(new Rect2(18, 14, 76, 26), () => (_zoomTo, _pan) = (0, Vector2.Zero), null);
        Words(T("computer.radar.system"), new Vector2(24, 32), Small, deep ? (Hovered(system) ? Colors.White : Mute) : Text);
        _hits.Add(system);
        float alpha = 0.35f + 0.65f * _zoom;
        Words("›", new Vector2(98, 32), Small, DimText with { A = alpha });
        var planet = Hit.Box(new Rect2(112, 14, 110, 26), () => { if (Course == null) ZoomTo(_focus); }, null);
        Words(PlanetName(_focus), new Vector2(116, 32), Small, (deep ? Text : Hovered(planet) ? Colors.White : Mute) with { A = alpha });
        _hits.Add(planet);
        ZoomButton("zin", 50, "+", () => Zoom(0.34f));
        ZoomButton("zout", 154, "−", () => Zoom(-0.34f));
        DrawLine(new Vector2(32, 82), new Vector2(32, 148), Faint with { A = 0.38f }, 1);
        DrawRect(new Rect2(27, 144 - _zoom * 62, 10, 4), ShipColour);
        Words(T("computer.radar.orbitLevel"), new Vector2(54, 90), 10, _zoom > 0.5f ? Text : DimText);
        Words(T("computer.radar.systemLevel"), new Vector2(54, 148), 10, _zoom <= 0.5f ? Text : DimText);
        string hint = T("computer.radar.hint");
        Words(hint, new Vector2(Size.X - 16 - Width(hint, 10), 30), 10, DimText);
    }

    private void ZoomButton(string id, float y, string label, Action press)
    {
        var box = new Rect2(18, y, 28, 26);
        var hit = Hit.Box(box, press, null);
        DrawRect(box, Card with { A = 0.8f });
        SketchBox(box, BoardInk.Seed(id), Hovered(hit) ? Ink : Faint with { A = 0.5f }, 1.2f);
        Words(label, box.GetCenter() + new Vector2(0, 6), 15, Hovered(hit) ? Colors.White : Text, true);
        _hits.Add(hit);
    }

    /// <summary>The selected thing's callout: beside it, tied by a line of ink, its picture, name and facts, and its action.</summary>
    private void DrawCallout()
    {
        if (CardNow() is not { } card) return;
        var (anchor, radius) = card.Anchor;
        float open = Mathf.Clamp((_time - _opened) / 0.2f, 0, 1), wide = 270;
        bool reason = card.Button is { Lit: false, Reason: not null };
        float tall = 76 + card.Rows.Length * 20 + (card.Button != null ? 50 : 6) + (reason ? 16 : 0);
        var (x, y) = NavMap.Callout(anchor.X, anchor.Y, radius, wide, tall, Size.X, Size.Y);
        y += (1 - open) * 8;
        var box = new Rect2(x, y, wide, tall);
        bool right = x > anchor.X;
        var end = new Vector2(anchor.X + (right ? radius : -radius), anchor.Y);
        var start = new Vector2(right ? x : x + wide, Mathf.Clamp(anchor.Y, y + 16, y + tall - 16));
        InkDraw.Stroke(this, BoardInk.Stroke(end.X, end.Y, start.X, start.Y, 8.1f), Ink with { A = open * 0.86f }, 1.2f);
        DrawCircle(end, 2, Ink with { A = open });
        DrawRect(box, Card with { A = 0.95f * open });
        SketchBox(box, 8.3f, Ink with { A = open * 0.86f }, 1.4f);
        _hits.Add(Hit.Box(box, null, null));
        var frame = new Rect2(x + 14, y + 14, 60, 48);
        SketchBox(frame, 8.5f, Faint with { A = open * 0.4f }, 1);
        card.Picture(frame.GetCenter());
        Words(card.Title, new Vector2(x + 86, y + 32), Title, Text with { A = open });
        Words(card.Sub, new Vector2(x + 86, y + 52), Tiny, Mute with { A = open });
        for (int i = 0; i < card.Rows.Length; i++)
            Words(card.Rows[i].Text, new Vector2(x + 16, y + 88 + i * 20), Small, (card.Rows[i].Colour ?? Text) with { A = open });
        Close(new Vector2(x + wide - 26, y + 6));
        if (card.Button is { } button) Button(new Rect2(x + 14, y + 78 + card.Rows.Length * 20, wide - 28, 34), button, open);
    }

    private void Close(Vector2 at)
    {
        var hit = Hit.Box(new Rect2(at, new Vector2(20, 20)), () => Select(null, null), new Tip(T("computer.radar.close"), [new TipLine(T("computer.radar.closeHow"))]));
        var colour = Hovered(hit) ? Colors.White : Mute;
        DrawLine(at + new Vector2(5, 5), at + new Vector2(15, 15), colour, 1.3f, true);
        DrawLine(at + new Vector2(15, 5), at + new Vector2(5, 15), colour, 1.3f, true);
        _hits.Add(hit);
    }

    private void Button(Rect2 box, ButtonSpec button, float open)
    {
        if (button.Busy is float busy)
        {
            DrawRect(new Rect2(box.Position, new Vector2(box.Size.X * busy, box.Size.Y)), ShipColour with { A = 0.12f * open });
            SketchBox(box, 90.1f, Drawn with { A = 0.6f * open }, 1.2f);
            Words(button.Label, box.GetCenter() + new Vector2(0, 5), Small, ShipColour with { A = open }, true);
            return;
        }
        if (!button.Lit)
        {
            SketchBox(box, 90.1f, new Color(0.55f, 0.59f, 0.67f, 0.3f * open), 1.2f);
            Words(button.Label, box.GetCenter() + new Vector2(0, 5), Small, new Color("566074") with { A = open }, true);
            if (button.Reason is { } reason) Words(reason, new Vector2(box.GetCenter().X, box.End.Y + 16), Tiny, Warn with { A = open }, true);
            return;
        }
        var hit = Hit.Box(box, button.Press, null);
        bool over = Hovered(hit);
        DrawRect(box, Loop with { A = (over ? 0.22f : 0.1f) * open });
        SketchBox(box, 90.1f, (over ? Colors.White : Ink) with { A = open }, 1.4f);
        Words(button.Label, box.GetCenter() + new Vector2(0, 5), Small, new Color("f4f6fa") with { A = open }, true);
        _hits.Add(hit);
    }

    /// <summary>What the callout shows for the selection now, and where it is tied; none for no selection or one out of view.</summary>
    private CardView? CardNow()
    {
        if (_planet is int p)
        {
            var spec = LandingData.Planets[p];
            bool here = p == At && Course == null;
            var rows = new List<Row> { new($"{T("computer.radar.ruins")}: {T($"computer.radar.tech.{spec.Technology}")}") };
            if (here) rows.Add(new Row(T("computer.radar.here"), ShipColour));
            else
            {
                rows.Add(new Row(T("computer.radar.flight", ("s", LandingData.FlightTicks / Sim.World.TicksPerSecond))));
                rows.Add(new Row(T("computer.radar.fuel", ("n", LandingData.JumpFuel))));
            }
            var anchor = Screen(NavMap.Planet(p));
            return new CardView((anchor, Px(NavMap.PlanetRadius) + 6), PlanetName(p), T($"computer.radar.type.{spec.Type}"), [.. rows],
                at => DrawPlanet(at, 20, spec.Type), PlanetButton(p, here));
        }
        if (_ui.RadarContact.Get() is not int id || NearShown < 0.4f) return null;
        var c = ((List<Contact>)[.. Contacts]).Find(k => k.Id == id);
        if (c == null) return null;
        var cls = Vessels.Class(c.Class);
        bool going = c.Left < Going;
        Row[] facts =
        [
            new($"{T("computer.radar.length")}: {T("computer.radar.cells", ("n", cls?.Length ?? 0))}"),
            new($"{T("computer.radar.decks")}: {Decks(cls)}"),
            new($"{T("computer.radar.distance")}: {T("computer.radar.km", ("n", RadarOrbit.Km(c.Offset)))}"),
            new(Leaves(c.Left), going ? (Mathf.Sin(_time * 4.4f) > 0 ? Amber : Amber with { A = 0.5f }) : null),
        ];
        return new CardView((Screen(NavMap.Contact(At, c.Offset)), 18), ClassName(c.Class), T("computer.radar.vessel"), facts,
            at => Silhouette(at, 40 * (cls?.Length ?? 100) / 200f + 6, c.Class, false, 1),
            new ButtonSpec(T("computer.radar.setOff"), true, null, () => Jump?.Invoke(id), null));
    }

    private ButtonSpec? PlanetButton(int p, bool here)
    {
        if (Course is { } c)
        {
            if (p != c.To) return new ButtonSpec(T("computer.radar.setCourse"), false, T("landing.inFlight"), null, null);
            return new ButtonSpec(T("computer.radar.flying", ("s", (c.TicksLeft + Sim.World.TicksPerSecond - 1) / Sim.World.TicksPerSecond)), false, null, null, _along);
        }
        if (here) return _zoomTo >= 1 && _focus == At ? null : new ButtonSpec(T("computer.radar.whatsNear"), true, null, () => ZoomTo(At), null);
        string? problem = p < Problems.Count ? Problems[p] : null;
        // The crew away is the call home: the press goes, and the sim refuses it to every peer's screen.
        bool lit = problem is null or "landing.crewAway";
        return new ButtonSpec(T("computer.radar.setCourse"), lit, lit ? null : T(problem!, ("fuel", LandingData.JumpFuel)), () => SetCourse?.Invoke(p), null);
    }

    private static string PlanetName(int i) => T("computer.radar.planetN", ("n", i + 1));
    private static string ClassName(string cls) => T($"computer.radar.class.{cls}");
    private static string Decks(VesselClass? cls) => cls == null ? "?" : cls.Decks.Min == cls.Decks.Max ? $"{cls.Decks.Min}" : $"{cls.Decks.Min}–{cls.Decks.Max}";
    private static string Leaves(int ticks) => T("computer.radar.leaves", ("t", Clock(ticks)));

    private static string Clock(int ticks)
    {
        int s = ticks / Sim.World.TicksPerSecond;
        return $"{s / 60}:{s % 60:00}";
    }

    private bool Hovered(Hit hit) => GetLocalMousePosition() is var at && new Rect2(Vector2.Zero, Size).HasPoint(at) && hit.Holds(at) && HitAt(at) is { } top && top.Same(hit);

    private void DrawPlanet(Vector2 at, float r, string type)
    {
        var (light, mid, dark, spot) = Looks(type);
        DrawCircle(at, r, dark);
        DrawCircle(at - new Vector2(r, r) * 0.08f, r * 0.9f, mid);
        DrawCircle(at - new Vector2(r, r) * 0.35f, r * 0.45f, light with { A = 0.5f });
        for (int i = 0; i < 5; i++)
        {
            float angle = i * 2.4f + 0.7f, d = r * (0.2f + 0.12f * i);
            DrawCircle(at + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * d, r * (0.12f + 0.03f * (i % 3)), spot with { A = 0.32f });
        }
        DrawArc(at + new Vector2(r, r) * 0.12f, r * 0.95f, -0.4f, 2.0f, 24, new Color(0.02f, 0.03f, 0.05f, 0.35f), r * 0.18f);
        DrawArc(at, r, 0, Mathf.Tau, Mathf.Clamp((int)r, 32, 256), new Color(0.88f, 0.91f, 0.95f, 0.5f), 1, true);
    }

    /// <summary>A planet's colours by its type: light, middle, dark, and its spots'.</summary>
    private static (Color, Color, Color, Color) Looks(string type) => type switch
    {
        _ => (new Color("8eaa72"), new Color("5d7a4c"), new Color("25331f"), new Color("7b6c47")),
    };

    /// <summary>A derelict from the side, <paramref name="wide"/> long: a shuttle stubby with two decks, a freighter long and low.</summary>
    private void Silhouette(Vector2 at, float wide, string cls, bool hot, float alpha)
    {
        bool big = cls == "freighter";
        float tall = big ? wide * 0.2f : wide * 0.38f, x0 = at.X - wide / 2, x1 = at.X + wide / 2, y0 = at.Y - tall / 2, y1 = at.Y + tall / 2;
        if (hot) DrawCircle(at, wide * 0.6f, Amber with { A = 0.18f * alpha });
        Vector2[] hull = big
            ? [new(x0 + wide * 0.04f, y0), new(x1 - wide * 0.1f, y0), new(x1 - wide * 0.07f, y0 - tall * 0.35f), new(x1 - wide * 0.015f, y0 - tall * 0.35f),
                new(x1, y0 + tall * 0.3f), new(x1, y1), new(x0 + wide * 0.06f, y1), new(x0 + wide * 0.02f, y1 - tall * 0.25f), new(x0, at.Y - tall * 0.12f)]
            : [new(x0, y0 + tall * 0.15f), new(x1 - wide * 0.3f, y0), new(x1 - wide * 0.08f, y0 + tall * 0.1f), new(x1, at.Y + tall * 0.15f),
                new(x1 - wide * 0.08f, y1), new(x0 + wide * 0.06f, y1), new(x0, y1 - tall * 0.2f)];
        DrawColoredPolygon(hull, Hull with { A = alpha });
        DrawPolyline([.. hull, hull[0]], (hot ? Amber : Ink) with { A = 0.86f * alpha }, 1, true);
        int decks = big ? 3 : 2;
        for (int k = 1; k < decks; k++)
        {
            float y = y0 + tall * k / decks;
            DrawLine(new Vector2(x0 + wide * 0.06f, y), new Vector2(x1 - wide * (big ? 0.04f : 0.14f), y), Mute with { A = 0.45f * alpha }, 1);
        }
    }

    /// <summary>The orange loop the board marks a thing with, drawn round as it is selected.</summary>
    private void HandLoop(Vector2 at, float wide, float tall, float seed)
    {
        var loop = BoardInk.Loop(seed, wide, tall);
        int shown = Mathf.Clamp((int)(loop.Length * Mathf.Clamp((_time - _opened) / 0.28f, 0, 1)), 2, loop.Length);
        var points = new Vector2[shown];
        for (int i = 0; i < shown; i++) points[i] = at + new Vector2(loop[i].X, loop[i].Y);
        DrawPolyline(points, Loop, 1.8f, true);
    }

    private void SketchBox(Rect2 box, float seed, Color colour, float width) =>
        InkDraw.Strokes(this, BoardInk.Box(box.Position.X, box.Position.Y, box.Size.X, box.Size.Y, seed, 3, 12), colour, width);

    private void Dotted(Vector2 centre, float radius, Color colour)
    {
        if (radius < 2) return;
        int dots = Mathf.Clamp((int)(Mathf.Tau * radius / 7), 12, 720);
        var points = new Vector2[dots * 2];
        for (int i = 0; i < dots; i++)
        {
            float a = Mathf.Tau * i / dots, b = a + Mathf.Tau / dots * 0.3f;
            (points[2 * i], points[2 * i + 1]) = (centre + Vector2.FromAngle(a) * radius, centre + Vector2.FromAngle(b) * radius);
        }
        DrawMultiline(points, colour, 1);
    }

    private void Dashed(Vector2[] path, Color colour)
    {
        var dashes = new List<Vector2>();
        float run = -_time * 20 % 11;
        for (int i = 1; i < path.Length; i++)
        {
            var (a, b) = (path[i - 1], path[i]);
            float length = a.DistanceTo(b);
            for (float t = 0; t < length; t += 1)
                if (Mathf.PosMod(run + t, 11) < 5 && t + 1 <= length) dashes.AddRange([a.Lerp(b, t / length), a.Lerp(b, (t + 1) / length)]);
            run += length;
        }
        if (dashes.Count > 1) DrawMultiline([.. dashes], colour, 1.2f);
    }

    private void Words(string text, Vector2 at, int size, Color colour, bool centred = false)
    {
        var font = GetThemeDefaultFont();
        if (centred) at.X -= font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X / 2;
        DrawString(font, at, text, HorizontalAlignment.Left, -1, size, colour);
    }

    private float Width(string text, int size) => GetThemeDefaultFont().GetStringSize(text, HorizontalAlignment.Left, -1, size).X;

    private sealed record Row(string Text, Color? Colour = null);

    /// <summary>A callout's button: its words, whether it can be pressed, why not, what a press does, how far a flight it counts is.</summary>
    private sealed record ButtonSpec(string Label, bool Lit, string? Reason, System.Action? Press, float? Busy);

    private sealed record CardView((Vector2 At, float Radius) Anchor, string Title, string Sub, Row[] Rows, Action<Vector2> Picture, ButtonSpec? Button);

    /// <summary>Something the pointer finds on the map: a circle or a box, what a click on it does, its tooltip.</summary>
    private sealed record Hit(Vector2 Centre, float Radius, Rect2? Area, System.Action? Click, Tip? Tip)
    {
        public static Hit Round(Vector2 centre, float radius, System.Action? click, Tip? tip) => new(centre, radius, null, click, tip);
        public static Hit Box(Rect2 area, System.Action? click, Tip? tip) => new(area.GetCenter(), 0, area, click, tip);
        public bool Holds(Vector2 at) => Area is { } area ? area.HasPoint(at) : at.DistanceTo(Centre) <= Radius;
        public bool Same(Hit other) => Centre == other.Centre && Radius == other.Radius && Area == other.Area;
    }
}
