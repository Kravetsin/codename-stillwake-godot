using System.Collections.Generic;
using Godot;
using Stillwake.Sim;
using Stillwake.Sim.Places;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Computer;

/// <summary>
/// The radar program, over the computer's whole screen: the ship a dot held in the middle, rings of range round it out past
/// the edges, a beam sweeping round, a planet's curve below, still, and the sim's contacts on the ship's orbit
/// (2026-11-08-radar-contacts), each flaring as the beam passes it and blinking in its last minutes. A contact selected is
/// framed and named. The beam turns by real time: only a look. Along its top the solar system (solar-system): the sun on the left,
/// its planets in a row, the ship's mark over the one it is at or between two on a course; a planet selected is framed, its card
/// under it.
/// </summary>
public partial class RadarScreen : Control
{
    private static readonly Color Back = new("0b1f16"), Grid = new("1d4a30"), Beam = new("7dffa8"), Trail = new("26804a"),
        Ship = new("ffe08a"), Line = new("1a1e25"), Ground = new("1f2e1a"), Rim = new("6fe08a");

    /// <summary>Seconds a turn of the beam takes.</summary>
    private const float SweepSeconds = 4f;
    /// <summary>A contact with fewer ticks than this left blinks: going soon (2 min).</summary>
    private const int Going = 2 * 60 * World.TicksPerSecond;
    private const int LabelSize = 16;

    /// <summary>The system strip's height, and how far apart its sun and first planet stand from the left.</summary>
    private const float Strip = 64, SunX = 30, FirstX = 92;
    private static readonly Color Sun = new("ffd36a"), Sky = new("08160f");

    private float _time;

    /// <summary>The system planet the ship is at, by index (<c>LandingModule.At</c>).</summary>
    public int At { get; set; }

    /// <summary>The planet the ship flies to on its course and how far along it is, 0 to 1; null standing.</summary>
    public (int To, double Along)? Flight { get; set; }

    /// <summary>The system planet this player selected, by index; null for none.</summary>
    public int? Planet { get; set; }

    /// <summary>The contacts up, as the panel last read them.</summary>
    public IReadOnlyList<Contact> Contacts { get; set; } = [];

    /// <summary>The contact this player selected, by id; null for none.</summary>
    public int? Selected { get; set; }

    public RadarScreen()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ClipContents = true;
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree()) return;
        _time += (float)delta;
        QueueRedraw();
    }

    /// <summary>What lies under <paramref name="global"/>, a point on the screen: a contact's, the ship's, the sun's or the planet's i18n key; none elsewhere.</summary>
    public string? KeyAt(Vector2 global)
    {
        var at = Local(global);
        if (at == null) return null;
        if (at.Value.Y < Strip)
        {
            if (at.Value.DistanceTo(new Vector2(SunX, Strip / 2)) < 18) return "computer.radar.sun";
            return Mathf.Abs(at.Value.X - ShipX()) < 8 && at.Value.Y < Strip / 2 - 6 ? "computer.radar.ship" : null;
        }
        // A selected contact is named over it already.
        if (ContactAt(global) is { } id) return id == Selected ? null : "computer.radar.vessel";
        var (middle, core, radius) = Shape();
        // A little wider than the dot itself, so the pointer finds it.
        if (ShipBox(middle).Grow(4).HasPoint(at.Value)) return "computer.radar.ship";
        return at.Value.DistanceTo(core) <= radius ? "computer.radar.planet" : null;
    }

    /// <summary>The system planet under <paramref name="global"/>, a point on the screen, by index; none elsewhere.</summary>
    public int? PlanetAt(Vector2 global)
    {
        if (Local(global) is not { } at || at.Y >= Strip) return null;
        for (int i = 0; i < Stillwake.Content.Landing.Planets.Count; i++)
            if (Mathf.Abs(at.X - PlanetX(i)) <= 14) return i;
        return null;
    }

    /// <summary>Where system planet <paramref name="i"/> stands across the strip: in a row from the sun to the screen's right.</summary>
    private float PlanetX(int i)
    {
        int n = Stillwake.Content.Landing.Planets.Count;
        return FirstX + (Size.X - FirstX - 40) * i / Mathf.Max(1, n - 1);
    }

    /// <summary>Where the ship's mark stands across the strip: over its planet, or between two along its course.</summary>
    private float ShipX() => Flight is { } f ? Mathf.Lerp(PlanetX(At), PlanetX(f.To), (float)f.Along) : PlanetX(At);

    /// <summary>The contact under <paramref name="global"/>, a point on the screen, by id: the nearest, a little wider than its dot; none elsewhere.</summary>
    public int? ContactAt(Vector2 global)
    {
        if (Local(global) is not { } at || at.Y < Strip) return null;
        int? found = null;
        float best = float.MaxValue;
        foreach (var c in Contacts)
        {
            var spot = Spot(c);
            float d = Mathf.Max(Mathf.Abs(spot.X - at.X), Mathf.Abs(spot.Y - at.Y));
            if (d <= 12 && d < best) (found, best) = (c.Id, d);
        }
        return found;
    }

    private Vector2? Local(Vector2 global)
    {
        var at = GetGlobalTransform().AffineInverse() * global;
        return new Rect2(Vector2.Zero, Size).HasPoint(at) ? at : null;
    }

    /// <summary>The ship in the middle, and the planet's core and radius: its curve a third of the screen under the ship, so wide it reads as ground.</summary>
    private (Vector2 Middle, Vector2 Core, float Radius) Shape()
    {
        var middle = (Size / 2).Round();
        float radius = Size.X * 1.6f;
        return (middle, middle + new Vector2(0, Mathf.Min(Size.X, Size.Y) * 0.3f + radius), radius);
    }

    /// <summary>Where contact <paramref name="c"/> is drawn: on the ship's orbit (<see cref="RadarOrbit"/>).</summary>
    private Vector2 Spot(Contact c)
    {
        var (middle, core, _) = Shape();
        var (x, y) = RadarOrbit.At(c.Offset, Size.X, middle.X, middle.Y, core.Y - middle.Y);
        return new Vector2(x, y);
    }

    private static Rect2 ShipBox(Vector2 middle) => new(middle - new Vector2(7, 7), new Vector2(14, 14));

    public override void _Draw()
    {
        var size = Size;
        var (middle, core, radius) = Shape();
        float reach = size.Length() / 2;
        float side = Mathf.Min(size.X, size.Y);
        DrawRect(new Rect2(Vector2.Zero, size), Back);

        // The planet below.
        DrawCircle(core, radius, Ground);
        DrawArc(core, radius, 0, Mathf.Tau, 512, Rim, 2);

        // Rings of range round the ship, and its cross.
        for (float ring = side / 4; ring < reach; ring += side / 4) DrawArc(middle, ring, 0, Mathf.Tau, 96, Grid, 1.5f);
        DrawLine(new Vector2(0, middle.Y), new Vector2(size.X, middle.Y), Grid, 1.5f);
        DrawLine(new Vector2(middle.X, 0), new Vector2(middle.X, size.Y), Grid, 1.5f);

        // The ship's orbit, faint: where contacts lie.
        float orbit = core.Y - middle.Y, half = size.X / orbit;
        DrawArc(core, orbit, -Mathf.Pi / 2 - half, -Mathf.Pi / 2 + half, 256, Rim with { A = 0.18f }, 1.5f);

        // The beam turning anticlockwise, a fading trail behind it.
        float beam = -Mathf.Tau * _time / SweepSeconds;
        const int steps = 12;
        for (int i = 0; i < steps; i++)
        {
            float from = beam + i * 0.05f, to = beam + (i + 1) * 0.05f;
            var fade = Trail with { A = 0.55f * (1 - (float)i / steps) };
            DrawColoredPolygon([middle, middle + Polar(from, reach), middle + Polar(to, reach)], fade);
        }
        DrawLine(middle, middle + Polar(beam, reach), Beam, 2);

        foreach (var c in Contacts) DrawContact(c, middle, beam);

        // The ship, and how high it rides over the ground.
        DrawDashedLine(middle, new Vector2(middle.X, core.Y - radius), Ship with { A = 0.6f }, 1.5f, 4);
        var ship = ShipBox(middle);
        DrawRect(ship, Line);
        DrawRect(ship.Grow(-2), Ship);

        DrawSystem();
    }

    /// <summary>The solar system along the top: the sun, its planets on a line, the ship's mark, a selected planet framed and its card.</summary>
    private void DrawSystem()
    {
        float mid = Strip / 2;
        DrawRect(new Rect2(0, 0, Size.X, Strip), Sky);
        DrawLine(new Vector2(0, Strip), new Vector2(Size.X, Strip), Grid, 1.5f);
        DrawLine(new Vector2(SunX, mid), new Vector2(PlanetX(Stillwake.Content.Landing.Planets.Count - 1), mid), Grid, 1.5f);
        float pulse = 0.85f + 0.15f * Mathf.Sin(_time * 1.3f);
        DrawCircle(new Vector2(SunX, mid), 22 * pulse, Sun with { A = 0.12f });
        DrawCircle(new Vector2(SunX, mid), 14, Sun);
        for (int i = 0; i < Stillwake.Content.Landing.Planets.Count; i++)
        {
            var at = new Vector2(PlanetX(i), mid);
            bool target = Flight is { } f && f.To == i;
            DrawCircle(at, 9, Line);
            DrawCircle(at, 7, i == At && Flight == null ? Rim : Ground);
            DrawArc(at, 7, 0, Mathf.Tau, 32, Rim, 1.5f);
            if (target) DrawArc(at, 12, 0, Mathf.Tau, 32, Ship with { A = 0.4f + 0.4f * Mathf.Abs(Mathf.Sin(_time * 3)) }, 1.5f);
            if (i == Planet) Brackets(at, 14, 5, Beam);
        }
        // The ship's mark, a little wedge pointing down at where it is.
        float x = ShipX();
        DrawColoredPolygon([new Vector2(x - 7, 6), new Vector2(x + 7, 6), new Vector2(x, 16)], Line);
        DrawColoredPolygon([new Vector2(x - 5, 7), new Vector2(x + 5, 7), new Vector2(x, 14)], Ship);
        if (Planet is int p) DrawCard(p);
    }

    /// <summary>A selected planet's card under the strip: its name, its type, the technology whose ruins prevail on it.</summary>
    private void DrawCard(int p)
    {
        var planet = Stillwake.Content.Landing.Planets[p];
        string[] lines = [T("computer.radar.planetN", ("n", p + 1)), T($"computer.radar.type.{planet.Type}"), T($"computer.radar.tech.{planet.Technology}")];
        var font = GetThemeDefaultFont();
        float line = font.GetHeight(LabelSize), width = 0;
        foreach (var l in lines) width = Mathf.Max(width, font.GetStringSize(l, HorizontalAlignment.Left, -1, LabelSize).X);
        float left = Mathf.Clamp(PlanetX(p) - width / 2, 6, Size.X - width - 6);
        var back = new Rect2(left - 6, Strip + 8, width + 12, lines.Length * line + 6);
        DrawRect(back, Back with { A = 0.9f });
        DrawRect(back, Grid, false, 1);
        float top = back.Position.Y + 3 + font.GetAscent(LabelSize);
        for (int i = 0; i < lines.Length; i++)
            DrawString(font, new Vector2(left, top + i * line), lines[i], HorizontalAlignment.Left, -1, LabelSize, i == 0 ? Beam : Rim with { A = 0.8f });
    }

    /// <summary>Corner brackets round <paramref name="at"/>, <paramref name="box"/> out, their arms <paramref name="arm"/> long.</summary>
    private void Brackets(Vector2 at, float box, float arm, Color colour)
    {
        foreach (var (sx, sy) in new[] { (-1, -1), (1, -1), (1, 1), (-1, 1) })
        {
            var corner = at + new Vector2(sx * box, sy * box);
            DrawPolyline([corner - new Vector2(0, sy * arm), corner, corner - new Vector2(sx * arm, 0)], colour, 2);
        }
    }

    /// <summary>
    /// A contact: a diamond, brightest as the beam has just passed it and fading over the turn, as a radar's blip; blinking
    /// while it is going; framed and named while selected.
    /// </summary>
    private void DrawContact(Contact c, Vector2 middle, float beam)
    {
        var spot = Spot(c);
        float since = Mathf.PosMod(Mathf.Atan2(spot.Y - middle.Y, spot.X - middle.X) - beam, Mathf.Tau) / Mathf.Tau;
        float bright = 0.35f + 0.65f * Mathf.Pow(1 - since, 3);
        if (c.Left < Going) bright *= 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(_time * 2.2f));
        if (bright > 0.7f) DrawCircle(spot, 10 * bright, Beam with { A = 0.18f * bright });
        DrawColoredPolygon(Diamond(spot, 8.5f), Line);
        DrawColoredPolygon(Diamond(spot, 5.5f), Beam with { A = bright });
        if (c.Id != Selected) return;

        // Corner brackets round it, and its name over them.
        const float box = 13;
        Brackets(spot, box, 6, Beam);
        // Its name, and under it how far it lies from the ship, dimmer.
        var font = GetThemeDefaultFont();
        string name = T("computer.radar.vessel"), far = T("computer.radar.km", ("n", RadarOrbit.Km(c.Offset)));
        float line = font.GetHeight(LabelSize);
        float width = Mathf.Max(font.GetStringSize(name, HorizontalAlignment.Left, -1, LabelSize).X,
            font.GetStringSize(far, HorizontalAlignment.Left, -1, LabelSize).X);
        float left = Mathf.Clamp(spot.X - width / 2, 6, Size.X - width - 6);
        var back = new Rect2(left - 5, spot.Y - box - 10 - 2 * line, width + 10, 2 * line + 4);
        DrawRect(back, Back with { A = 0.85f });
        DrawRect(back, Grid, false, 1);
        float top = back.Position.Y + 2 + font.GetAscent(LabelSize);
        DrawString(font, new Vector2(left, top), name, HorizontalAlignment.Left, -1, LabelSize, Beam);
        DrawString(font, new Vector2(left, top + line), far, HorizontalAlignment.Left, -1, LabelSize, Rim with { A = 0.8f });
    }

    private static Vector2[] Diamond(Vector2 at, float r) => [at + new Vector2(0, -r), at + new Vector2(r, 0), at + new Vector2(0, r), at + new Vector2(-r, 0)];

    private static Vector2 Polar(float angle, float length) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * length;
}
