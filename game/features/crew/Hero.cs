using System;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Rooms;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Features.Crew;

/// <summary>A point in thousandths of a cell.</summary>
public readonly record struct Point(int X, int Y);

/// <summary>
/// What the player holds during one step; <see cref="Y"/> is -1 for up. <see cref="Jump"/> is set once
/// per press. <see cref="Face"/> is the side the pointer is on, which the hero faces whichever way it
/// walks; with none, it faces the way it walks; swimming, the way it swims. <see cref="Sprint"/>, Shift
/// held, runs and swims fast. <see cref="Swings"/>, the axe mid-swing (0039): it faces
/// <see cref="Face"/> swimming too, so the blade comes down where the swing was aimed.
/// </summary>
public readonly record struct HeroInput(int X, int Y, bool Jump, int? Face = null, bool Sprint = false, bool Swings = false);

/// <summary>
/// The cells the hero moves against; which of them are space, where the hero floats; and which are the
/// hull, where a tether is not yet needed: any cell with a structure or a back wall, aired or not.
/// </summary>
public sealed class Ship(int width, int height, Func<int, int> structureAt, Func<int, bool> space, Func<int, bool>? hull = null)
{
    public int Width => width;
    public int Height => height;

    /// <summary>The <see cref="Structure"/> code of a cell on the grid.</summary>
    public int StructureAt(int cell) => structureAt(cell);

    public bool Space(int cell) => space(cell);

    public bool Hull(int cell) => hull?.Invoke(cell) ?? structureAt(cell) != Structure.Empty;

    /// <summary>
    /// The ship as the sim's grid and rooms see it now, its cells' structure as <paramref name="structureAt"/>
    /// gives it when given: doors as this peer shows them, its own toggles in flight made (0014).
    /// </summary>
    public static Ship Of(IGridQueries grid, IRoomsQueries rooms, Func<int, int>? structureAt = null) =>
        new(grid.Width, grid.Height, structureAt ?? grid.StructureAt, cell => rooms.RoomAt(cell) == RoomsModule.Space,
            cell => (structureAt ?? grid.StructureAt)(cell) != Structure.Empty || grid.BackAt(cell) != null);
}

/// <summary>
/// One hero's movement (apps/game/src/features/crew/hero.ts): <see cref="X"/> is the middle of the box
/// and <see cref="Y"/> its bottom, where the feet are, in thousandths of a cell. <see cref="Anchor"/>,
/// out in space, is where the tether is fixed: the middle of the last cell not in space the body left
/// (0007); none aboard.
/// </summary>
public readonly record struct Hero(int X, int Y, int Vx, int Vy, bool Climbing, int Facing, Point? Anchor = null)
{
    /// <summary>Thousandths of a cell: the unit of every length and speed in <see cref="Spec"/>.</summary>
    public const int Unit = 1000;

    /// <summary>The controller steps 60 times a second, whatever the frame rate.</summary>
    public const double StepMs = 1000.0 / 60;

    /// <summary>
    /// How far the body's middle is above the feet: half the height rounded down, 1312 where the
    /// TypeScript has 1312.5, so every position stays a whole number.
    /// </summary>
    public const int MiddleUp = Spec.Height / 2;

    private const int Half = Spec.Width / 2;

    /// <summary>A hero standing on the bottom of <paramref name="cell"/>, in the middle.</summary>
    public static Hero At(Ship ship, int cell) =>
        new(cell % ship.Width * Unit + Unit / 2, (cell / ship.Width + 1) * Unit, 0, 0, false, 1);

    /// <summary>The cell the hero's body is in: the one under the middle of its box.</summary>
    public int BodyCell(int width) => CellOf(Y - MiddleUp) * width + CellOf(X);

    /// <summary>The middle of the hero's body.</summary>
    public Point Middle => new(X, Y - MiddleUp);

    /// <summary>
    /// One controller step. Walls, closed doors and the grid's edges block; floors and the tops of
    /// ladders hold the hero up but let them through from below; ladders are climbed while held. With
    /// the body in space there is no gravity: the hero swims where the keys point and stops with them,
    /// on a tether that keeps the body's middle within <see cref="Spec.Tether"/> of where it left the ship.
    /// </summary>
    public Hero Step(HeroInput input, Ship ship) => Tethered(this, Move(this, input, ship), ship);

    /// <summary>
    /// <paramref name="after"/>, a step from <paramref name="before"/>, on its tether: anchored as the
    /// body leaves the hull, cleared as it comes aboard, and a move past the tether's length cut back to
    /// the limit, toward where it was going, so the hero slides along it. A ship open to space all
    /// through is still the hull: afloat inside it the hero needs no tether, and leaving it ties one.
    /// </summary>
    private static Hero Tethered(Hero before, Hero after, Ship ship)
    {
        int at = after.BodyCell(ship.Width);
        if (!ship.Space(at)) return after;
        int left = before.BodyCell(ship.Width);
        // Back within the hull, the tether comes off; it ties again where the body next leaves it.
        if (ship.Hull(at)) return after;
        Point? tied = before.Anchor ?? (ship.Space(left) && !ship.Hull(left)
            ? null
            : new Point(left % ship.Width * Unit + Unit / 2, left / ship.Width * Unit + Unit / 2));
        if (tied is not Point anchor) return after;
        bool Within(int x, int y) => Distance(x - anchor.X, y - MiddleUp - anchor.Y) <= Spec.Tether;
        if (Within(after.X, after.Y)) return after with { Anchor = anchor };
        // To the limit, the way the body was going; walls still stop it, and where a wall leaves it
        // past the limit, only the part across or the part up or down, or no move at all.
        var middle = after.Middle;
        double far = Distance(middle.X - anchor.X, middle.Y - anchor.Y);
        var from = before.Middle;
        int byX = anchor.X + (int)Math.Truncate((double)(middle.X - anchor.X) * Spec.Tether / far) - from.X;
        int byY = anchor.Y + (int)Math.Truncate((double)(middle.Y - anchor.Y) * Spec.Tether / far) - from.Y;
        int x = MoveX(ship, before.X, before.Y, byX);
        (int X, int Y) to = (before.X, before.Y);
        foreach (var t in new[]
        {
            (x, MoveY(ship, x, before.Y, byY, false)),
            (x, before.Y),
            (before.X, MoveY(ship, before.X, before.Y, byY, false)),
        })
        {
            if (!Within(t.Item1, t.Item2)) continue;
            to = t;
            break;
        }
        return after with { X = to.X, Y = to.Y, Vx = to.X - before.X, Vy = to.Y - before.Y, Anchor = anchor };
    }

    private static double Distance(int dx, int dy) => Math.Sqrt((double)dx * dx + (double)dy * dy);

    private static Hero Move(Hero hero, HeroInput input, Ship ship)
    {
        int facing = input.Face ?? (input.X == 0 ? hero.Facing : input.X);
        // Swimming, the hero faces the way it swims, whatever the pointer does, unless it swings the axe.
        if (ship.Space(hero.BodyCell(ship.Width))) return Swim(ship, hero.X, hero.Y, input, input.X == 0 || input.Swings ? facing : input.X);
        bool grab = !hero.Climbing && input.Y != 0 && input.X == 0 && !input.Jump &&
            TouchesLadder(ship, hero.X, hero.Y, input.Y > 0);
        if (grab || hero.Climbing)
        {
            // A ladder centres the hero on it; narrower than a cell, the hero touches nothing beside it.
            if (input.X == 0 && !input.Jump) return Climb(ship, CellOf(hero.X) * Unit + Unit / 2, hero.Y, input.Y, facing);
            // Walking or jumping lets go.
            return Fall(ship, hero.X, hero.Y, input.X, input.Jump ? -Spec.Jump : 0, facing, input.Sprint);
        }
        int vy = input.Jump && Grounded(ship, hero.X, hero.Y) ? -Spec.Jump : hero.Vy;
        return Fall(ship, hero.X, hero.Y, input.X, vy, facing, input.Sprint);
    }

    private static Hero Fall(Ship ship, int x, int y, int dir, int vy, int facing, bool sprint)
    {
        int speed = Math.Min(vy + Spec.Gravity, Spec.MaxFall);
        int nx = MoveX(ship, x, y, dir * (sprint ? Spec.Run : Spec.Walk));
        int ny = MoveY(ship, nx, y, speed, false);
        return new Hero(nx, ny, nx - x, ny - y == speed ? speed : 0, false, facing);
    }

    private static Hero Swim(Ship ship, int x, int y, HeroInput input, int facing)
    {
        int swim = input.Sprint ? Spec.SwimFast : Spec.Swim;
        int nx = MoveX(ship, x, y, input.X * swim);
        int ny = MoveY(ship, nx, y, input.Y * swim, false);
        return new Hero(nx, ny, nx - x, ny - y, false, facing);
    }

    private static Hero Climb(Ship ship, int x, int y, int dir, int facing)
    {
        int ny = MoveY(ship, x, y, dir * Spec.Climb, true);
        int column = CellOf(x);
        int before = CellOf(y - 1);
        int after = CellOf(ny - 1);
        // The feet rose past the ladder's top: the hero steps off and stands on it.
        if (after < before && Climbable(ship, column, before) && !Climbable(ship, column, after))
            return new Hero(x, before * Unit, 0, 0, false, facing);
        return new Hero(x, ny, 0, ny - y, TouchesLadder(ship, x, ny, false), facing);
    }

    private static bool Grounded(Ship ship, int x, int y) => MoveY(ship, x, y, 1, false) == y;

    /// <summary>Whether the hero's column holds a ladder beside the body, or also just below the feet.</summary>
    private static bool TouchesLadder(Ship ship, int x, int y, bool belowFeet)
    {
        int column = CellOf(x);
        int last = CellOf(belowFeet ? y : y - 1);
        for (int row = CellOf(y - Spec.Height); row <= last; row++)
            if (Climbable(ship, column, row)) return true;
        return false;
    }

    /// <summary><paramref name="x"/> moved by <paramref name="dx"/>, stopped flush against the first blocking column the leading edge enters.</summary>
    private static int MoveX(Ship ship, int x, int y, int dx)
    {
        int top = CellOf(y - Spec.Height);
        int bottom = CellOf(y - 1);
        bool Blocked(int column)
        {
            for (int row = top; row <= bottom; row++)
                if (Solid(ship, column, row)) return true;
            return false;
        }
        if (dx > 0)
        {
            for (int c = CellOf(x + Half - 1) + 1; c <= CellOf(x + Half + dx - 1); c++)
                if (Blocked(c)) return c * Unit - Half;
        }
        else if (dx < 0)
        {
            for (int c = CellOf(x - Half) - 1; c >= CellOf(x - Half + dx); c--)
                if (Blocked(c)) return (c + 1) * Unit + Half;
        }
        return x + dx;
    }

    /// <summary>
    /// <paramref name="y"/> moved by <paramref name="dy"/>, stopped flush against the first blocking row
    /// the leading edge enters. Going down, floors block too, and ladder tops unless the hero climbs down
    /// through them. A row the feet were already in is never entered, which lets a hero through a floor
    /// from below.
    /// </summary>
    private static int MoveY(Ship ship, int x, int y, int dy, bool climbing)
    {
        int left = CellOf(x - Half);
        int right = CellOf(x + Half - 1);
        bool Blocked(int row, bool down)
        {
            for (int c = left; c <= right; c++)
                if (Solid(ship, c, row) || (down && Holds(ship, c, row, climbing))) return true;
            return false;
        }
        if (dy > 0)
        {
            for (int r = CellOf(y - 1) + 1; r <= CellOf(y + dy - 1); r++)
                if (Blocked(r, true)) return r * Unit;
        }
        else if (dy < 0)
        {
            int top = y - Spec.Height;
            for (int r = CellOf(top) - 1; r >= CellOf(top + dy); r--)
                if (Blocked(r, false)) return (r + 1) * Unit + Spec.Height;
        }
        return y + dy;
    }

    /// <summary>The cell a length in thousandths falls in, rounding down below zero too.</summary>
    private static int CellOf(int v) => v >= 0 ? v / Unit : -((Unit - 1 - v) / Unit);

    private static int? StructureAt(Ship ship, int x, int y) =>
        x < 0 || y < 0 || x >= ship.Width || y >= ship.Height ? null : ship.StructureAt(y * ship.Width + x);

    /// <summary>Walls, closed doors, and everything past the grid's edge.</summary>
    private static bool Solid(Ship ship, int x, int y) =>
        StructureAt(ship, x, y) is null or Structure.Wall or Structure.DoorClosed;

    /// <summary>Ladders, and an open door in line with one: a hatch the ladder climbs through.</summary>
    private static bool Climbable(Ship ship, int x, int y) => StructureAt(ship, x, y) switch
    {
        Structure.Ladder => true,
        Structure.DoorOpen => StructureAt(ship, x, y - 1) == Structure.Ladder || StructureAt(ship, x, y + 1) == Structure.Ladder,
        _ => false,
    };

    /// <summary>What holds a hero up from below only: floors, and a ladder's top cell unless climbing.</summary>
    private static bool Holds(Ship ship, int x, int y, bool climbing) =>
        StructureAt(ship, x, y) == Structure.Floor || (!climbing && Climbable(ship, x, y) && !Climbable(ship, x, y - 1));
}
