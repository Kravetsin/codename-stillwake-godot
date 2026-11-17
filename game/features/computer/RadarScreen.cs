using System;
using System.Collections.Generic;
using System.Linq;
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
/// held to it by a line of ink, with its facts and its action: the ship's course to a planet, the shuttle's route to a contact
/// (shuttle-flight), a line to it and its words on top while it is set; a stretch of space coming is a band on the ship's orbit. Esc or a click on empty
/// map puts it away. It redraws only while something on it moves or changes, and its ink is written once and kept: strokes
/// rebuilt every frame cost the board milliseconds and the collector its pauses.
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
    /// <summary>Seconds the selection's loop takes to be drawn round.</summary>
    private const float LoopS = 0.28f;
    private const int Small = 13, Tiny = 11, Title = 15;

    /// <summary>Dotted circles of a unit radius, by their dots, scaled to an orbit as they are drawn: built once.</summary>
    private static readonly Dictionary<int, Vector2[]> Dots = [];
    /// <summary>Sketched boxes by size and seed, at the origin, moved where they are drawn: written once.</summary>
    private static readonly Dictionary<(int, int, float), Vector2[][]> Boxes = [];
    private static readonly string[] Numbers = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10"];

    private readonly Ui _ui;
    private readonly List<Hit> _hits = [], _drawn = [];
    private readonly Vector2[] _course = new Vector2[41], _dashes = new Vector2[40], _flown = new Vector2[21];
    private float _zoom, _zoomTo, _along, _time, _opened;
    private int _focus;
    private Vector2 _pan, _mouse;
    private Vector2? _press;
    private bool _dragged, _dirty = true, _blink;
    private int? _planet;
    private string? _tipKey;
    private Tip? _tip;
    private (Vector2, Vector2) _leaderAt;
    private Vector2[] _leader = [];
    private int _at;
    private (int To, int TicksLeft)? _courseNow;
    private IReadOnlyList<Contact> _contacts = [];
    private IReadOnlyList<string?> _problems = [];
    private int? _route;
    private (Color Colour, float Grow, float Shown)? _stretch;
    private const int BandSteps = 24;
    private readonly Vector2[] _band = new Vector2[2 * (BandSteps + 1)];

    /// <summary>The system planet the ship is at (<c>LandingModule.At</c>).</summary>
    public int At { get => _at; set => Changed(ref _at, value); }

    /// <summary>The ship's course, where to and its ticks left; null standing.</summary>
    public (int To, int TicksLeft)? Course { get => _courseNow; set => Changed(ref _courseNow, value); }

    /// <summary>The contacts up, as the panel last read them.</summary>
    public IReadOnlyList<Contact> Contacts { get => _contacts; set => Changed(ref _contacts, value); }

    /// <summary>Why no course can be set to each system planet, as the panel last read it: null where one can.</summary>
    public IReadOnlyList<string?> Problems { get => _problems; set => Changed(ref _problems, value); }

    /// <summary>
    /// Each system planet's ores, item ids (planet-variety), as the panel last read them: empty till dealt. Set only when they
    /// change, so the screen redraws for nothing else.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<string>> Ores
    {
        get => _ores;
        set
        {
            if (value.Count == _ores.Count && value.Zip(_ores).All(z => z.First.SequenceEqual(z.Second))) return;
            (_ores, _dirty, _tipKey) = (value, true, null);
        }
    }

    private IReadOnlyList<IReadOnlyList<string>> _ores = [];

    /// <summary>The shuttle's route, its contact by id, as the panel last read it: every player's; null for none.</summary>
    public int? Route { get => _route; set => Changed(ref _route, value); }

    /// <summary>
    /// A stretch of space coming or running (<see cref="RadarBand"/>): its band's colour, how far its warning has run, and how much of
    /// it shows, eased out after it has gone; none for none.
    /// </summary>
    public (Color Colour, float Grow, float Shown)? Stretch { get => _stretch; set => Changed(ref _stretch, value); }

    /// <summary>Sets the ship's course to a system planet: <c>landing</c>'s <c>PlotCourse</c>.</summary>
    public Action<int>? PlotCourse { get; set; }

    /// <summary>Sets the shuttle's route to a contact by id: <c>places</c>' <c>SetCourse</c>.</summary>
    public Action<int>? SetRoute { get; set; }

    /// <summary>Clears the shuttle's route: <c>places</c>' <c>ClearCourse</c>.</summary>
    public Action? ClearRoute { get; set; }

    /// <summary>Takes a contact by id off the radar for everyone: <c>places</c>' <c>Forget</c>.</summary>
    public Action<int>? Forget { get; set; }

    public RadarScreen(Ui ui)
    {
        _ui = ui;
        MouseFilter = MouseFilterEnum.Stop;
        ClipContents = true;
        Resized += () => _dirty = true;
    }

    private void Changed<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        (_dirty, _tipKey) = (true, null);
    }

    /// <summary>Opens on the whole system, nothing selected.</summary>
    public void Reset()
    {
        (_zoom, _zoomTo, _pan, _planet, _focus, _dirty) = (0, 0, Vector2.Zero, null, At, true);
        _ui.RadarContact.Set(null);
    }

    /// <summary>Puts an open callout away: Esc's first step. Whether there was one.</summary>
    public bool LetGo()
    {
        if (_planet == null && _ui.RadarContact.Get() == null) return false;
        Select(null, null);
        return true;
    }

    /// <summary>The tooltip for what lies under <paramref name="global"/>, a point on the screen; none elsewhere. The same one while it stays.</summary>
    public Tip? TipAt(Vector2 global)
    {
        var hit = HitAt(GetGlobalTransform().AffineInverse() * global);
        if (hit?.Key != _tipKey) (_tipKey, _tip) = (hit?.Key, hit?.Tip?.Invoke());
        return _tip;
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree()) return;
        float dt = Mathf.Min((float)delta, 0.05f), k = Mathf.Min(1, dt * 7);
        _time += dt;
        if (_zoom != _zoomTo)
        {
            _zoom = Mathf.Abs(_zoomTo - _zoom) < 0.001f ? _zoomTo : _zoom + (_zoomTo - _zoom) * k;
            _dirty = true;
        }
        if (_zoom == 0 && Course == null && _focus != At) (_focus, _dirty) = (At, true);
        float along = Course is { } c ? 1 - c.TicksLeft / (float)LandingData.FlightTicks : 0;
        // The mark glides between reads rather than stepping each quarter second.
        float glide = Course == null ? 0 : _along + (along - _along) * Mathf.Min(1, dt * 4);
        if (Mathf.Abs(glide - _along) > 0.0001f || (Course == null && _along != 0)) (_along, _dirty) = (glide, true);
        // A contact selected elsewhere (a demo) brings the map down to it.
        if (_ui.RadarContact.Get() != null && _zoomTo < 1 && Course == null) (_zoomTo, _focus, _dirty) = (1, At, true);
        if (_time - _opened < LoopS + 0.25f) _dirty = true;
        // A contact going blinks: a redraw at each turn of its blink, not every frame.
        bool blink = Mathf.Sin(_time * 4.4f) > 0;
        if (blink != _blink && NearShown > 0 && AnyGoing()) _dirty = true;
        _blink = blink;
        var mouse = GetLocalMousePosition();
        if (mouse != _mouse && new Rect2(Vector2.Zero, Size).Grow(4).HasPoint(mouse)) _dirty = true;
        _mouse = mouse;
        if (!_dirty) return;
        _dirty = false;
        QueueRedraw();
    }

    private bool AnyGoing()
    {
        foreach (var c in Contacts)
            if (c.Left < Going) return true;
        return false;
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
                if (_dragged) (_pan, _dirty) = ((_pan + motion.Relative).Clamp(-Size, Size), true);
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
        _dirty = true;
    }

    private void Zoom(float by)
    {
        // Between planets there is nothing near to look at.
        if (by > 0 && Course != null) return;
        _zoomTo = Mathf.Clamp(_zoomTo + by, 0, 1);
    }

    private void ZoomTo(int planet) => (_focus, _zoomTo, _pan, _dirty) = (planet, 1, Vector2.Zero, true);

    private void Select(int? planet, int? contact)
    {
        if (planet != _planet || contact != _ui.RadarContact.Get()) _opened = _time;
        _planet = planet;
        _ui.RadarContact.Set(contact);
        (_dirty, _tipKey) = (true, null);
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
        for (int i = 0; i < LandingData.Planets.Count; i++) Dotted(sun, Px(NavMap.Orbit(i)), Faint);
        DrawSun(sun);
        DrawStretch();
        DrawCourse(sun);
        DrawPlanets();
        DrawNear();
        DrawShip(sun);
        DrawChrome();
        DrawRoute();
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

    private void DrawSun(Vector2 at)
    {
        if (!new Rect2(Vector2.Zero, Size).Grow(200).HasPoint(at)) return;
        float r = 15 * Mathf.Min(ScaleNow, 3);
        for (int i = 6; i >= 1; i--) DrawCircle(at, r * (1 + 0.37f * i), new Color(0.89f, 0.69f, 0.38f, 0.05f));
        DrawCircle(at, r, new Color("c79a56"));
        DrawCircle(at - new Vector2(r, r) * 0.25f, r * 0.6f, new Color("f1dfae") with { A = 0.6f });
        DrawArc(at, r, 0, Mathf.Tau, 48, new Color(1, 0.94f, 0.82f, 0.55f), 1);
        _hits.Add(Hit.Round("sun", at, 18, null, () => new Tip(T("computer.radar.sun"), [])));
    }

    private void DrawCourse(Vector2 sun)
    {
        int from, to;
        if (Course is { } c) (from, to) = (At, c.To);
        else if (_planet is int p && p != At) (from, to) = (At, p);
        else return;
        CoursePoints(sun, from, to, 1, _course);
        // Every other piece of the path: a dashed line in one call.
        for (int i = 0; i < _dashes.Length / 2; i++) (_dashes[2 * i], _dashes[2 * i + 1]) = (_course[2 * i], _course[2 * i + 1]);
        DrawMultiline(_dashes, Course == null ? Ink with { A = 0.86f } : Drawn with { A = 0.6f }, 1.2f);
        if (Course != null && _along > 0)
        {
            CoursePoints(sun, from, to, _along, _flown);
            DrawPolyline(_flown, ShipColour with { A = 0.65f }, 1.6f, true);
        }
        else if (ScaleNow < 3)
            Words(T("computer.radar.seconds", ("s", LandingData.FlightTicks / Sim.World.TicksPerSecond)), _course[20] + new Vector2(6, -6), Tiny, Text);
    }

    private void CoursePoints(Vector2 sun, int from, int to, float upTo, Vector2[] points)
    {
        var (a, b) = (Screen(NavMap.Planet(from)), Screen(NavMap.Planet(to)));
        int last = points.Length - 1;
        for (int i = 0; i <= last; i++)
        {
            var (x, y) = NavMap.Course((a.X, a.Y), (b.X, b.Y), (sun.X, sun.Y), upTo * i / last);
            points[i] = new Vector2(x, y);
        }
    }

    private void DrawPlanets()
    {
        for (int i = 0; i < LandingData.Planets.Count; i++)
        {
            var at = Screen(NavMap.Planet(i));
            float r = Px(NavMap.PlanetRadius);
            if (!new Rect2(Vector2.Zero, Size).Grow(r + 60).HasPoint(at)) continue;
            int planet = i;
            var hit = Hit.Round($"planet{i}", at, Mathf.Max(14, r + 4), () => Select(planet, null), _planet == i ? null : () => PlanetTip(planet));
            if (ScaleNow < 3 && Hovered(hit)) DrawArc(at, r + 6, 0, Mathf.Tau, 48, Faint with { A = 0.38f }, 1);
            DrawPlanet(at, r, LandingData.Planets[i].Type);
            if (ScaleNow < 3) Words(Numbers[i % Numbers.Length], at + new Vector2(r + 3, -r - 1), 10, Mute);
            _hits.Add(hit);
            if (_planet == i) HandLoop(at, r + 10, r + 9, i + 3);
        }
    }

    private Tip PlanetTip(int i) => new(PlanetName(i), [new TipLine(T($"computer.radar.type.{LandingData.Planets[i].Type}")),
        new TipLine(i == At && Course == null ? T("computer.radar.here") : T("computer.radar.flight", ("s", LandingData.FlightTicks / Sim.World.TicksPerSecond)))]);

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
            bool going = c.Left < Going;
            var contact = c;
            float wide = RadarModels.Long(c.Class, RadarModels.MapPerCell, RadarModels.MapLeast, RadarModels.MapMost);
            var hit = Hit.Round($"contact{c.Id}", at, Mathf.Max(20, wide / 2), () => Select(null, contact.Id), _ui.RadarContact.Get() == c.Id ? null
                : () => new Tip(ClassName(contact.Class), [new TipLine(T("computer.radar.km", ("n", RadarOrbit.Km(contact.Offset)))),
                    new TipLine(Leaves(contact.Left), contact.Left < Going ? Tone.Bad : Tone.Plain)]));
            if (Route == c.Id) DrawDashedLine(Screen(NavMap.Ship(At)), at, Loop with { A = 0.8f * shown }, 1.4f, 6, true, true);
            Silhouette(at, wide, c.Class, going && _blink, shown);
            Words(T("computer.radar.km", ("n", RadarOrbit.Km(c.Offset))), at + new Vector2(0, 22), Tiny, (Hovered(hit) ? Colors.White : Text) with { A = shown }, true);
            if (going) Words(Clock(c.Left), at + new Vector2(0, 36), Tiny, Amber with { A = shown }, true);
            if (shown > 0.5f) _hits.Add(hit);
            if (_ui.RadarContact.Get() == c.Id) HandLoop(at, Mathf.Max(27, wide / 2 + 8), 22, c.Id);
        }
    }

    private void DrawShip(Vector2 sun)
    {
        Vector2 at;
        float angle = 0;
        if (Course is { } c)
        {
            float t = Mathf.Max(_along, 0.001f);
            var (a, b) = (Screen(NavMap.Planet(At)), Screen(NavMap.Planet(c.To)));
            var (x0, y0) = NavMap.Course((a.X, a.Y), (b.X, b.Y), (sun.X, sun.Y), Mathf.Max(0, t - 0.01f));
            var (x1, y1) = NavMap.Course((a.X, a.Y), (b.X, b.Y), (sun.X, sun.Y), t);
            (at, angle) = (new Vector2(x1, y1), new Vector2(x1 - x0, y1 - y0).Angle());
        }
        else at = Screen(NavMap.Ship(At));
        Span<Vector2> shape = [new(8, 0), new(-6, -5), new(-3, 0), new(-6, 5)];
        var polygon = new Vector2[5];
        for (int i = 0; i < 4; i++) polygon[i] = at + shape[i].Rotated(angle);
        polygon[4] = polygon[0];
        DrawColoredPolygon(polygon[..4], ShipColour);
        DrawPolyline(polygon, new Color(0.03f, 0.05f, 0.09f, 0.9f), 1, true);
        _hits.Add(Hit.Round("ship", at, 8, null, () => new Tip(T("computer.radar.ship"),
            [new TipLine(Course != null ? T("computer.radar.inFlight") : T("computer.radar.atPlanet", ("n", At + 1)))])));
        // While the whole system shows, a tag under the ship's planet says what is near and zooms down to it.
        float far = 1 - Mathf.Clamp((ScaleNow - 1.5f) / 1.5f, 0, 1);
        if (Course != null || far < 0.05f) return;
        var planet = Screen(NavMap.Planet(At));
        float r = Px(NavMap.PlanetRadius);
        string words = T("computer.radar.near", ("n", Contacts.Count));
        float wide = Mathf.Ceil(Width(words, Tiny) + 20);
        var box = new Rect2(planet.X < sun.X ? planet.X - r - 16 - wide : planet.X + r + 16, planet.Y + r + 4, wide, 20);
        var tag = Hit.Box("near", box, () => ZoomTo(At), () => new Tip(T("computer.radar.nearTitle"), [new TipLine(T("computer.radar.nearHow"))]));
        DrawRect(box, Card with { A = 0.8f * far });
        SketchBox(box, 6.2f, (Hovered(tag) ? Ink : Faint with { A = 0.5f }) with { A = far }, 1.2f);
        Words(words, box.GetCenter() + new Vector2(0, 4), Tiny, (Hovered(tag) ? Text : Mute) with { A = far }, true);
        _hits.Add(tag);
    }

    /// <summary>
    /// A stretch of space coming: a band on the ship's orbit round its planet, ahead of the ship as its warning starts and over it
    /// as it comes (<see cref="RadarBand"/> across the old strip, mapped round the orbit as contacts are).
    /// </summary>
    private void DrawStretch()
    {
        if (Stretch is not { Shown: > 0 } stretch || Course != null) return;
        var (near, far) = RadarBand.Across(stretch.Grow, 1, 0.5f);
        float from = Angle(near), to = Angle(far);
        if (to - from < 0.01f) return;
        var planet = Screen(NavMap.Planet(At));
        float orbit = Px(NavMap.PlanetRadius + NavMap.OrbitGap), thick = Mathf.Max(3, orbit * 0.1f);
        for (int i = 0; i <= BandSteps; i++)
        {
            var way = Vector2.FromAngle(from + (to - from) * i / BandSteps);
            (_band[i], _band[2 * BandSteps + 1 - i]) = (planet + way * (orbit + thick), planet + way * (orbit - thick));
        }
        DrawColoredPolygon(_band, stretch.Colour with { A = 0.22f * stretch.Shown });
        var edge = Vector2.FromAngle(from);
        DrawLine(planet + edge * (orbit - thick), planet + edge * (orbit + thick), stretch.Colour with { A = 0.8f * stretch.Shown }, 2);

        static float Angle(float x) => -Mathf.Pi / 2 + Mathf.Clamp((x - 0.5f) / 0.5f, -1, 1) * 1.5f;
    }

    /// <summary>The shuttle's route set: its words at the top in the middle, at any zoom.</summary>
    private void DrawRoute()
    {
        if (Route == null) return;
        string words = T("computer.radar.route");
        float wide = Mathf.Ceil(Width(words, Small) + 28);
        var box = new Rect2(Mathf.Round((Size.X - wide) / 2), 14, wide, 26);
        DrawRect(box, Card with { A = 0.85f });
        SketchBox(box, 7.4f, Loop with { A = 0.8f }, 1.2f);
        Words(words, box.GetCenter() + new Vector2(0, 5), Small, Text, true);
    }

    /// <summary>Where the map is and how to move it: the breadcrumb, the zoom's buttons and gauge, a hint.</summary>
    private void DrawChrome()
    {
        bool deep = _zoom >= 0.5f;
        var system = Hit.Box("system", new Rect2(18, 14, 76, 26), () => (_zoomTo, _pan, _dirty) = (0, Vector2.Zero, true), null);
        Words(T("computer.radar.system"), new Vector2(24, 32), Small, deep ? (Hovered(system) ? Colors.White : Mute) : Text);
        _hits.Add(system);
        float alpha = 0.35f + 0.65f * _zoom;
        Words("›", new Vector2(98, 32), Small, DimText with { A = alpha });
        var planet = Hit.Box("crumb", new Rect2(112, 14, 110, 26), () => { if (Course == null) ZoomTo(_focus); }, null);
        Words(PlanetName(_focus), new Vector2(116, 32), Small, (deep ? Text : Hovered(planet) ? Colors.White : Mute) with { A = alpha });
        _hits.Add(planet);
        ZoomButton("zin", 50, "+", 1.7f, () => Zoom(0.34f));
        ZoomButton("zout", 154, "−", 2.9f, () => Zoom(-0.34f));
        DrawLine(new Vector2(32, 82), new Vector2(32, 148), Faint with { A = 0.38f }, 1);
        DrawRect(new Rect2(27, 144 - _zoom * 62, 10, 4), ShipColour);
        Words(T("computer.radar.orbitLevel"), new Vector2(54, 90), 10, _zoom > 0.5f ? Text : DimText);
        Words(T("computer.radar.systemLevel"), new Vector2(54, 148), 10, _zoom <= 0.5f ? Text : DimText);
        string hint = T("computer.radar.hint");
        Words(hint, new Vector2(Size.X - 18 - Width(hint, 10), Size.Y - 16), 10, DimText);
    }

    private void ZoomButton(string id, float y, string label, float seed, Action press)
    {
        var box = new Rect2(18, y, 28, 26);
        var hit = Hit.Box(id, box, press, null);
        DrawRect(box, Card with { A = 0.8f });
        SketchBox(box, seed, Hovered(hit) ? Ink : Faint with { A = 0.5f }, 1.2f);
        Words(label, box.GetCenter() + new Vector2(0, 6), 15, Hovered(hit) ? Colors.White : Text, true);
        _hits.Add(hit);
    }

    /// <summary>The selected thing's callout: beside it, tied by a line of ink, its picture, name and facts, and its actions one under another.</summary>
    private void DrawCallout()
    {
        if (CardNow() is not { } card) return;
        var (anchor, radius) = card.Anchor;
        float open = Mathf.Clamp((_time - _opened) / 0.2f, 0, 1), wide = 270;
        var lines = Wrapped(card.Rows, wide - 32);
        float buttons = 0;
        foreach (var b in card.Buttons) buttons += ButtonStep + (b is { Lit: false, Reason: not null } ? 16 : 0);
        float tall = 76 + lines.Count * 20 + (card.Buttons.Length > 0 ? buttons - ButtonStep + 50 : 6);
        var (x, y) = NavMap.Callout(anchor.X, anchor.Y, radius, wide, tall, Size.X, Size.Y);
        (x, y) = (Mathf.Round(x), Mathf.Round(y + (1 - open) * 8));
        var box = new Rect2(x, y, wide, tall);
        bool right = x > anchor.X;
        var end = new Vector2(anchor.X + (right ? radius : -radius), anchor.Y).Round();
        var start = new Vector2(right ? x : x + wide, Mathf.Clamp(anchor.Y, y + 16, y + tall - 16)).Round();
        Leader(end, start, Ink with { A = open * 0.86f });
        DrawCircle(end, 2, Ink with { A = open });
        DrawRect(box, Card with { A = 0.95f * open });
        SketchBox(box, 8.3f, Ink with { A = open * 0.86f }, 1.4f);
        _hits.Add(Hit.Box("callout", box, null, null));
        var frame = new Rect2(x + 14, y + 14, 60, 48);
        SketchBox(frame, 8.5f, Faint with { A = open * 0.4f }, 1);
        card.Picture(frame.GetCenter());
        Words(card.Title, new Vector2(x + 86, y + 32), Title, Text with { A = open });
        Words(card.Sub, new Vector2(x + 86, y + 52), Tiny, Mute with { A = open });
        for (int i = 0; i < lines.Count; i++)
            Words(lines[i].Text, new Vector2(x + 16, y + 88 + i * 20), Small, (lines[i].Colour ?? Text) with { A = open });
        Close(new Vector2(x + wide - 26, y + 6));
        float top = y + 78 + lines.Count * 20;
        for (int i = 0; i < card.Buttons.Length; i++)
        {
            var button = card.Buttons[i];
            Button(new Rect2(x + 14, top, wide - 28, 34), button, open, i);
            top += ButtonStep + (button is { Lit: false, Reason: not null } ? 16 : 0);
        }
    }

    /// <summary>A callout's button and the gap under it, before the next.</summary>
    private const float ButtonStep = 42;

    /// <summary>The callout's rows broken at spaces into lines no wider than <paramref name="room"/>, a row's colour kept on each of its lines.</summary>
    private List<Row> Wrapped(Row[] rows, float room)
    {
        var lines = new List<Row>();
        foreach (var row in rows)
        {
            string line = "";
            foreach (var word in row.Text.Split(' '))
            {
                string longer = line.Length == 0 ? word : $"{line} {word}";
                if (line.Length > 0 && Width(longer, Small) > room)
                {
                    lines.Add(row with { Text = line });
                    line = word;
                }
                else line = longer;
            }
            lines.Add(row with { Text = line });
        }
        return lines;
    }

    /// <summary>The callout's line of ink to its thing, written again only when either end moves.</summary>
    private void Leader(Vector2 end, Vector2 start, Color colour)
    {
        if (_leaderAt != (end, start))
        {
            _leader = Points(BoardInk.Stroke(end.X, end.Y, start.X, start.Y, 8.1f));
            _leaderAt = (end, start);
        }
        if (_leader.Length > 1) DrawPolyline(_leader, colour, 1.2f, true);
    }

    private void Close(Vector2 at)
    {
        var hit = Hit.Box("close", new Rect2(at, new Vector2(20, 20)), () => Select(null, null),
            () => new Tip(T("computer.radar.close"), [new TipLine(T("computer.radar.closeHow"))]));
        var colour = Hovered(hit) ? Colors.White : Mute;
        DrawLine(at + new Vector2(5, 5), at + new Vector2(15, 15), colour, 1.3f, true);
        DrawLine(at + new Vector2(15, 5), at + new Vector2(5, 15), colour, 1.3f, true);
        _hits.Add(hit);
    }

    private void Button(Rect2 box, ButtonSpec button, float open, int n)
    {
        if (button.Busy is float busy)
        {
            DrawRect(new Rect2(box.Position, new Vector2(box.Size.X * busy, box.Size.Y)), ShipColour with { A = 0.12f * open });
            SketchBox(box, 9.1f, Drawn with { A = 0.6f * open }, 1.2f);
            Words(button.Label, box.GetCenter() + new Vector2(0, 5), Small, ShipColour with { A = open }, true);
            return;
        }
        if (!button.Lit)
        {
            SketchBox(box, 9.1f, new Color(0.55f, 0.59f, 0.67f, 0.3f * open), 1.2f);
            Words(button.Label, box.GetCenter() + new Vector2(0, 5), Small, new Color("566074") with { A = open }, true);
            if (button.Reason is { } reason) Words(reason, new Vector2(box.GetCenter().X, box.End.Y + 16), Tiny, Warn with { A = open }, true);
            return;
        }
        var hit = Hit.Box($"action{n}", box, button.Press, null);
        bool over = Hovered(hit);
        // A quiet one (Forget) is drawn without the action's amber, so the way on stands out.
        if (!button.Quiet || over) DrawRect(box, Loop with { A = (over ? 0.22f : 0.1f) * open });
        SketchBox(box, 9.1f, (over ? Colors.White : Ink) with { A = open }, 1.4f);
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
            if (p < Ores.Count && Ores[p].Count > 0) rows.Add(new Row($"{T("computer.radar.ores")}: {string.Join(", ", Ores[p].Select(o => T($"item.{o}")))}"));
            if (here) rows.Add(new Row(T("computer.radar.here"), ShipColour));
            else
            {
                rows.Add(new Row(T("computer.radar.flight", ("s", LandingData.FlightTicks / Sim.World.TicksPerSecond))));
                rows.Add(new Row(T("computer.radar.fuel", ("n", LandingData.JumpFuel / Stillwake.Content.Fuel.Litre))));
            }
            var anchor = Screen(NavMap.Planet(p));
            return new CardView((anchor, Px(NavMap.PlanetRadius) + 6), PlanetName(p), T($"computer.radar.type.{spec.Type}"), [.. rows],
                at => DrawPlanet(at, 20, spec.Type), PlanetButton(p, here) is { } button ? [button] : []);
        }
        if (_ui.RadarContact.Get() is not int id || NearShown < 0.4f) return null;
        Contact? c = null;
        foreach (var k in Contacts)
            if (k.Id == id) c = k;
        if (c == null) return null;
        var cls = Vessels.Class(c.Class);
        bool going = c.Left < Going;
        Row[] facts =
        [
            new($"{T("computer.radar.length")}: {T("computer.radar.cells", ("n", cls?.Length ?? 0))}"),
            new($"{T("computer.radar.decks")}: {Decks(cls)}"),
            new($"{T("computer.radar.distance")}: {T("computer.radar.km", ("n", RadarOrbit.Km(c.Offset)))}"),
            new($"{T("computer.radar.shuttleFuel")}: {T("computer.radar.litres", ("l", PlacesModule.CostOf(c)))}"),
            new(Leaves(c.Left), going ? (_blink ? Amber : Amber with { A = 0.5f }) : null),
        ];
        string kind = c.Class;
        float onMap = RadarModels.Long(kind, RadarModels.MapPerCell, RadarModels.MapLeast, RadarModels.MapMost);
        float inCard = RadarModels.Long(kind, RadarModels.CardPerCell, RadarModels.CardLeast, RadarModels.CardMost);
        // The course's contact clears it in place of setting it; any contact can be forgotten, for everyone.
        var way = Route == id ? new ButtonSpec(T("computer.radar.clearRoute"), true, null, () => ClearRoute?.Invoke(), null)
            : new ButtonSpec(T("computer.radar.setCourse"), true, null, () => SetRoute?.Invoke(id), null);
        return new CardView((Screen(NavMap.Contact(At, c.Offset)), Mathf.Max(18, onMap / 2)), ClassName(c.Class), T("computer.radar.vessel"), facts,
            at => Silhouette(at, inCard, kind, false, 1),
            [way, new ButtonSpec(T("computer.radar.forget"), true, null, () => Forget?.Invoke(id), null, Quiet: true)]);
    }

    private ButtonSpec? PlanetButton(int p, bool here)
    {
        if (Course is { } c)
        {
            if (p != c.To) return new ButtonSpec(T("computer.radar.plotCourse"), false, T("landing.inFlight"), null, null);
            return new ButtonSpec(T("computer.radar.flying", ("s", (c.TicksLeft + Sim.World.TicksPerSecond - 1) / Sim.World.TicksPerSecond)), false, null, null, _along);
        }
        if (here) return _zoomTo >= 1 && _focus == At ? null : new ButtonSpec(T("computer.radar.whatsNear"), true, null, () => ZoomTo(At), null);
        string? problem = p < Problems.Count ? Problems[p] : null;
        // The crew away is the call home: the press goes, and the sim refuses it to every peer's screen.
        bool lit = problem is null or "landing.crewAway";
        return new ButtonSpec(T("computer.radar.plotCourse"), lit, lit ? null : T(problem!, ("fuel", LandingData.JumpFuel / Stillwake.Content.Fuel.Litre)), () => PlotCourse?.Invoke(p), null);
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

    private bool Hovered(Hit hit) => new Rect2(Vector2.Zero, Size).HasPoint(_mouse) && hit.Holds(_mouse) && HitAt(_mouse) is { } top && top.Key == hit.Key;

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
        DrawArc(at, r, 0, Mathf.Tau, Mathf.Clamp((int)r, 32, 128), new Color(0.88f, 0.91f, 0.95f, 0.5f), 1, true);
    }

    /// <summary>A planet's colours by its type: light, middle, dark, and its spots'.</summary>
    private static (Color, Color, Color, Color) Looks(string type) => type switch
    {
        "scorched" => (new Color("d69a6a"), new Color("a5603b"), new Color("47241a"), new Color("e0b66a")),
        "icy" => (new Color("dbe8f2"), new Color("9cb4c8"), new Color("3c4e60"), new Color("f4f8fc")),
        _ => (new Color("8eaa72"), new Color("5d7a4c"), new Color("25331f"), new Color("7b6c47")),
    };

    /// <summary>
    /// A derelict from the side, <paramref name="wide"/> long, its middle at <paramref name="at"/>: its class's model (<see cref="RadarModels"/>),
    /// written once at a unit's length and drawn scaled, its ink hairlines.
    /// </summary>
    private void Silhouette(Vector2 at, float wide, string cls, bool hot, float alpha)
    {
        var model = RadarModels.Of(cls);
        DrawSetTransform(at - new Vector2(0, model.Mid * wide), 0, new Vector2(wide, wide));
        if (hot) DrawCircle(new Vector2(0, model.Mid), 0.6f, Amber with { A = 0.18f * alpha });
        Color fill = Hull with { A = alpha }, line = (hot ? Amber : Ink) with { A = 0.86f * alpha };
        for (int i = 0; i < model.Back.Length; i++)
        {
            DrawColoredPolygon(model.Back[i], fill);
            DrawPolyline(model.BackLines[i], line, -1);
        }
        DrawColoredPolygon(model.Hull, fill);
        foreach (var glass in model.Glass) DrawColoredPolygon(glass, ShipColour with { A = 0.42f * alpha });
        if (model.Lines.Length > 0) DrawMultiline(model.Lines, Mute with { A = 0.55f * alpha }, -1);
        foreach (var (centre, r) in model.Rings) DrawArc(centre, r, 0, Mathf.Tau, 20, Mute with { A = 0.6f * alpha }, -1);
        DrawPolyline(model.HullLine, line, -1);
        DrawSetTransform(Vector2.Zero);
    }

    /// <summary>The orange loop the board marks a thing with, drawn round as it is selected.</summary>
    private void HandLoop(Vector2 at, float wide, float tall, float seed)
    {
        var loop = BoardInk.Loop(seed, wide, tall);
        int shown = Mathf.Clamp((int)(loop.Length * Mathf.Clamp((_time - _opened) / LoopS, 0, 1)), 2, loop.Length);
        var points = new Vector2[shown];
        for (int i = 0; i < shown; i++) points[i] = at + new Vector2(loop[i].X, loop[i].Y);
        DrawPolyline(points, Loop, 1.8f, true);
    }

    /// <summary>A box sketched as the board's are, written once for its size and moved where it is drawn.</summary>
    private void SketchBox(Rect2 box, float seed, Color colour, float width)
    {
        var key = ((int)box.Size.X, (int)box.Size.Y, seed);
        if (!Boxes.TryGetValue(key, out var strokes))
        {
            strokes = Array.ConvertAll(BoardInk.Box(0, 0, key.Item1, key.Item2, seed, 3, 12), Points);
            Boxes[key] = strokes;
        }
        DrawSetTransform(box.Position.Round());
        foreach (var line in strokes)
            if (line.Length > 1) DrawPolyline(line, colour, width, true);
        DrawSetTransform(Vector2.Zero);
    }

    private static Vector2[] Points(InkPoint[] stroke) => Array.ConvertAll(stroke, p => new Vector2(p.X, p.Y));

    /// <summary>A dotted circle: a unit one of as many dots as fit, scaled to <paramref name="radius"/> and drawn as hairlines in one call.</summary>
    private void Dotted(Vector2 centre, float radius, Color colour)
    {
        if (radius < 2 || centre.X + radius < 0 || centre.X - radius > Size.X || centre.Y + radius < 0 || centre.Y - radius > Size.Y) return;
        int dots = Mathf.Clamp(Mathf.NearestPo2((int)(Mathf.Tau * radius / 7)), 16, 1024);
        if (!Dots.TryGetValue(dots, out var unit))
        {
            unit = new Vector2[dots * 2];
            for (int i = 0; i < dots; i++)
            {
                float a = Mathf.Tau * i / dots;
                (unit[2 * i], unit[2 * i + 1]) = (Vector2.FromAngle(a), Vector2.FromAngle(a + Mathf.Tau / dots * 0.3f));
            }
            Dots[dots] = unit;
        }
        DrawSetTransform(centre, 0, new Vector2(radius, radius));
        DrawMultiline(unit, colour, -1);
        DrawSetTransform(Vector2.Zero);
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
    private sealed record ButtonSpec(string Label, bool Lit, string? Reason, Action? Press, float? Busy, bool Quiet = false);

    private sealed record CardView((Vector2 At, float Radius) Anchor, string Title, string Sub, Row[] Rows, Action<Vector2> Picture, ButtonSpec[] Buttons);

    /// <summary>Something the pointer finds on the map: a circle or a box, what a click on it does, its tooltip made when first shown.</summary>
    private sealed record Hit(string Key, Vector2 Centre, float Radius, Rect2? Area, Action? Click, Func<Tip>? Tip)
    {
        public static Hit Round(string key, Vector2 centre, float radius, Action? click, Func<Tip>? tip) => new(key, centre, radius, null, click, tip);
        public static Hit Box(string key, Rect2 area, Action? click, Func<Tip>? tip) => new(key, area.GetCenter(), 0, area, click, tip);
        public bool Holds(Vector2 at) => Area is { } area ? area.HasPoint(at) : at.DistanceTo(Centre) <= Radius;
    }
}
