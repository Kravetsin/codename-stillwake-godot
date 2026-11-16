using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using L = Stillwake.Game.Light;
using Looks = Stillwake.Game.VesselLooks;

namespace Stillwake.Game.Features.Light;

/// <summary>How a derelict's lamp burns (derelict-looks): red on its battery, dead, or the vessel's one dying lamp.</summary>
public enum LampState { Red, Dead, Dying }

/// <summary>A lamp the vessel was stamped with: its top-left in the vessel's frame, its kind, how it burns, where its phase starts.</summary>
public readonly record struct VesselLamp(int X, int Y, string Kind, LampState State, double PhaseMs);

/// <summary>A bridge's console the vessel was stamped with: its top-left in the frame, its kind (its look), whether its screens are lit and glitch.</summary>
public readonly record struct VesselConsole(int X, int Y, string Kind, bool Lit, bool Glitching, double PhaseMs);

/// <summary>A doorway the vessel was stamped with: its top-left in the frame, two columns across and <c>Rows</c> down.</summary>
public readonly record struct VesselDoor(int X, int Y, int Rows);

/// <summary>
/// The light inside an abandoned vessel (derelict-looks), a rule of drawing, never the sim's: each lamp, console and doorway it was
/// stamped with (<see cref="Shipyard.Stamp"/> of <see cref="Shipyard.Describe"/>, in reading order), and how each looks, drawn from
/// <see cref="Looks.LookRng"/> of its seed. The live grid decides only whether each shows: a lamp or console while its machine
/// stands, a door's dot while the door is shut. How bright each is runs on this client's clock.
/// </summary>
public sealed class VesselLights
{
    public uint Seed { get; }
    public IReadOnlyList<VesselLamp> Lamps { get; }
    public IReadOnlyList<VesselConsole> Consoles { get; }
    public IReadOnlyList<VesselDoor> Doors { get; }

    /// <summary>The lamp the vessel's one dying lamp is, by its place in <see cref="Lamps"/>; null with none.</summary>
    public int? Dying { get; }

    /// <summary>The derelict's own ceiling lamp, the only lamp its rooms are stamped with.</summary>
    public const string LampKind = Vessels.Lamp;

    /// <summary>What a bridge's consoles are, by their first look: the salvage's and the mockup's.</summary>
    public static readonly IReadOnlyList<string> ConsoleKinds = ["salvageConsole", Vessels.Console];

    public VesselLights(Vessel vessel)
    {
        Seed = vessel.Seed;
        var parts = Shipyard.Stamp(vessel);
        // The airlock: the room a hero first steps into, just inside the first door.
        var (sx, sy) = Shipyard.Door(vessel);
        var airlock = vessel.Compartments.First(c => sx >= c.Left && sx < c.Left + c.Width && sy >= c.Top && sy < c.Top + Vessels.Open);
        bool InAirlock(VesselPart p) => p.X >= airlock.Left && p.X < airlock.Left + airlock.Width && p.Y >= airlock.Top && p.Y < airlock.Top + Vessels.Open;

        var lamps = parts.Where(p => p.Layer == "machine" && p.Kind == LampKind).ToList();
        var others = lamps.Where(p => !InAirlock(p)).ToList();
        var dying = others.Count == 0 ? null : others[Looks.Below(Seed, Looks.Salt.Dying, 0, others.Count)];
        Lamps = lamps.Select(p => new VesselLamp(p.X, p.Y, p.Kind,
            InAirlock(p) ? LampState.Red : p == dying ? LampState.Dying
            : Looks.Below(Seed, Looks.Salt.Lamp, Cell(p), 100) < Looks.LampRedPercent ? LampState.Red : LampState.Dead,
            Phase(p))).ToList();
        Dying = dying == null ? null : lamps.IndexOf(dying);

        Consoles = parts.Where(p => p.Layer == "machine" && ConsoleKinds.Contains(Vessels.FirstLook(p.Kind))).Select(p =>
        {
            bool lit = Looks.Below(Seed, Looks.Salt.Console, Cell(p), Looks.ConsoleDarkOneIn) != 0;
            return new VesselConsole(p.X, p.Y, p.Kind, lit, lit && Looks.Below(Seed, Looks.Salt.Glitch, Cell(p), Looks.GlitchOneIn) == 0, Phase(p));
        }).ToList();

        // A doorway's pieces are stacked a door's height apart: its first is the one with none of it over it.
        var doors = parts.Where(p => p.Layer == "structure" && p.Kind == "door").ToList();
        var at = doors.Select(p => (p.X, p.Y)).ToHashSet();
        Doors = doors.Where(p => !at.Contains((p.X, p.Y - 2))).Select(p =>
        {
            int rows = 0;
            while (at.Contains((p.X, p.Y + rows))) rows += 2;
            return new VesselDoor(p.X, p.Y, rows);
        }).ToList();

        int Cell(VesselPart p) => p.Y * vessel.Width + p.X;
        double Phase(VesselPart p) => Looks.Below(Seed, Looks.Salt.Phase, Cell(p), 6000);
    }

    /// <summary>The light inside the vessel a surface of <paramref name="rules"/> holds from <paramref name="seed"/>; none off a vessel.</summary>
    public static VesselLights? For(Surfaces.Kind rules, uint seed) =>
        rules is VesselKind kind ? new VesselLights(Shipyard.Describe(Vessels.Class(kind.Class)!, seed)) : null;

    /// <summary>The grid cell of the frame's (<paramref name="x"/>, <paramref name="y"/>), the frame's top-left at <c>Vessels.Margin</c> every way.</summary>
    public static int CellOf(int x, int y, int gridWidth) => (y + Vessels.Margin) * gridWidth + x + Vessels.Margin;

    /// <summary>The grid cells of its lamps' top-lefts on a grid <paramref name="gridWidth"/> wide.</summary>
    public HashSet<int> LampCells(int gridWidth) => Lamps.Select(l => CellOf(l.X, l.Y, gridWidth)).ToHashSet();

    /// <summary>
    /// Whether <paramref name="machine"/> is one of the vessel's own lamps, at one of <paramref name="lampCells"/>: lit by its own view on
    /// its battery, never by the light's power share, and carrying no power bolt. One a hero builds there is not.
    /// </summary>
    public static bool Claims(Machine machine, IReadOnlySet<int> lampCells) => machine.Kind == LampKind && lampCells.Contains(machine.Cell);

    /// <summary>Whether a machine of <paramref name="kind"/> still stands with its top-left at grid cell <paramref name="cell"/>.</summary>
    public static bool Stands(IGridQueries grid, string kind, int cell) => grid.MachineAt(cell) is { } m && m.Kind == kind && m.Cell == cell;

    /// <summary>Whether <paramref name="door"/>'s first cell is still a shut doorway in the live grid: its dot shows.</summary>
    public static bool Shut(IGridQueries grid, VesselDoor door) => Doorways.IsClosed(grid.StructureAt(CellOf(door.X, door.Y, grid.Width)));

    /// <summary>How bright, of its whole, a lamp in <paramref name="state"/> burns <paramref name="ms"/> into this client's clock.</summary>
    public static float Shines(LampState state, double ms, double phaseMs) => state switch
    {
        LampState.Red => Breathes(L.EmergencyBreathe, L.EmergencyRate, ms, phaseMs),
        LampState.Dying => DyingAt(ms, phaseMs),
        _ => 0,
    };

    /// <summary>How bright a lit screen is, glitching or not.</summary>
    public static float Screen(bool glitching, double ms, double phaseMs) =>
        glitching && Draw(phaseMs, L.GlitchRate, ms, 0) < L.GlitchOdds ? L.GlitchLow : Breathes(L.ScreenHum, L.ScreenHumRate, ms, phaseMs);

    /// <summary>Whole less up to <paramref name="depth"/>, a sine <paramref name="rate"/> radians a second.</summary>
    private static float Breathes(float depth, float rate, double ms, double phaseMs) =>
        1 - depth * (1 - (float)Math.Sin((ms + phaseMs) / 1000 * rate)) / 2;

    private static float DyingAt(double ms, double phaseMs)
    {
        bool burst = Draw(phaseMs, L.DyingBurstRate, ms, 1) < L.DyingBurstOdds;
        bool off = Draw(phaseMs, burst ? L.DyingBurstSteps : L.DyingCalmSteps, ms, 2) < (burst ? L.DyingBurstOff : L.DyingCalmOff);
        return off ? L.DyingLow : Breathes(L.DyingHum, L.DyingHumRate, ms, phaseMs);
    }

    /// <summary>A draw in [0, 1) for the step of <paramref name="rate"/> a second that <paramref name="ms"/> is in, one light's own.</summary>
    private static float Draw(double phaseMs, float rate, double ms, int which)
    {
        long step = (long)Math.Floor((ms + phaseMs) / 1000 * rate);
        return (float)(Looks.LookRng((uint)phaseMs, Looks.Salt.Flicker, (int)(step * 4 + which)) >> 40) / (1 << 24);
    }
}
