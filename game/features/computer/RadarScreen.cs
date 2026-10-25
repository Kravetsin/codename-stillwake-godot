using Godot;

namespace Stillwake.Game.Features.Computer;

/// <summary>
/// The radar program, over the computer's whole screen: the ship a dot held in the middle, rings of range round it out past
/// the edges, a beam sweeping round, and a planet's curve below, still: the ship drifts round it too slowly to see. Only a
/// picture so far: the sim knows no orbit nor anything in range, so the beam turns by real time.
/// </summary>
public partial class RadarScreen : Control
{
    private static readonly Color Back = new("0b1f16"), Grid = new("1d4a30"), Beam = new("7dffa8"), Trail = new("26804a"),
        Ship = new("ffe08a"), Line = new("1a1e25"), Ground = new("1f2e1a"), Rim = new("6fe08a");

    /// <summary>Seconds a turn of the beam takes.</summary>
    private const float SweepSeconds = 4f;

    private float _time;

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

    /// <summary>What lies under <paramref name="global"/>, a point on the screen: the ship's or the planet's i18n key; none elsewhere.</summary>
    public string? KeyAt(Vector2 global)
    {
        var at = GetGlobalTransform().AffineInverse() * global;
        if (!new Rect2(Vector2.Zero, Size).HasPoint(at)) return null;
        var (middle, core, radius) = Shape();
        // A little wider than the dot itself, so the pointer finds it.
        if (ShipBox(middle).Grow(4).HasPoint(at)) return "computer.radar.ship";
        return at.DistanceTo(core) <= radius ? "computer.radar.planet" : null;
    }

    /// <summary>The ship in the middle, and the planet's core and radius: its curve a third of the screen under the ship, so wide it reads as ground.</summary>
    private (Vector2 Middle, Vector2 Core, float Radius) Shape()
    {
        var middle = (Size / 2).Round();
        float radius = Size.X * 1.6f;
        return (middle, middle + new Vector2(0, Mathf.Min(Size.X, Size.Y) * 0.3f + radius), radius);
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

        // The ship, and how high it rides over the ground.
        DrawDashedLine(middle, new Vector2(middle.X, core.Y - radius), Ship with { A = 0.6f }, 1.5f, 4);
        var ship = ShipBox(middle);
        DrawRect(ship, Line);
        DrawRect(ship.Grow(-2), Ship);
    }

    private static Vector2 Polar(float angle, float length) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * length;
}
