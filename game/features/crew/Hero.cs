using System;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Rooms;
using Pack = Stillwake.Content.Jetpack;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Features.Crew;

/// <summary>A point in thousandths of a cell.</summary>
public readonly record struct Point(int X, int Y);

/// <summary>
/// What the player holds during one step; <see cref="Y"/> is -1 for up, 1 for down: down a ladder, else into
/// a hole one cell wide or through the floors stood on. <see cref="Jump"/> is set once
/// per press. <see cref="Face"/> is the side the pointer is on, which the hero faces whichever way it
/// walks; with none, it faces the way it walks; swimming, the way it swims. <see cref="Sprint"/>, Shift
/// held, runs aboard; in space it only thrusts, with a jetpack worn. <see cref="Swings"/>, the axe
/// mid-swing (0039) or the harpoon aimed (0044): it faces <see cref="Face"/> swimming too, so the blade comes down where the swing was aimed and the gun
/// points at the pointer. <see cref="Pull"/>, the harpoon's grapple (0044): the body's middle is pulled
/// toward that point, keys and gravity aside, and held there once it is. <see cref="Jetpack"/>, a jetpack worn
/// (0047): in space the hero keeps its speed, ties no tether, and while <see cref="Thrust"/> is set, a unit
/// vector in thousandths, gains <c>Jetpack.Accel</c> a step that way up to <c>Jetpack.Top</c>; else it slows
/// by <c>Jetpack.Drag</c> a step toward what the keys swim.
/// </summary>
public readonly record struct HeroInput(
    int X, int Y, bool Jump, int? Face = null, bool Sprint = false, bool Swings = false, Point? Pull = null,
    bool Jetpack = false, Point? Thrust = null);

/// <summary>
/// The cells the hero moves against and the shapes of their walls (0069); which of them are space, where the
/// hero floats; and which are the hull, where a tether is not yet needed: any cell with a structure or a back
/// wall, aired or not.
/// </summary>
public sealed class Ship(
    int width, int height, Func<int, int> structureAt, Func<int, bool> space, Func<int, bool>? hull = null, Func<int, int>? shapeAt = null)
{
    public int Width => width;
    public int Height => height;

    /// <summary>The <see cref="Structure"/> code of a cell on the grid.</summary>
    public int StructureAt(int cell) => structureAt(cell);

    /// <summary>The <see cref="Shape"/> of a cell's wall: none for a whole wall or no wall.</summary>
    public int ShapeAt(int cell) => shapeAt?.Invoke(cell) ?? Shape.None;

    public bool Space(int cell) => space(cell);

    public bool Hull(int cell) => hull?.Invoke(cell) ?? structureAt(cell) != Structure.Empty;

    /// <summary>
    /// The ship as the sim's grid and rooms see it now, its cells' structure as <paramref name="structureAt"/>
    /// gives it when given: doors as this peer shows them, its own toggles in flight made (0014).
    /// </summary>
    public static Ship Of(IGridQueries grid, IRoomsQueries rooms, Func<int, int>? structureAt = null) =>
        new(grid.Width, grid.Height, structureAt ?? grid.StructureAt, cell => rooms.RoomAt(cell) == RoomsModule.Space,
            cell => (structureAt ?? grid.StructureAt)(cell) != Structure.Empty || grid.BackAt(cell) != null, grid.ShapeAt);
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
    /// ladders hold the hero up but let them through from below, and down drops through floors; ladders are
    /// climbed while held. With the body in space there is no gravity: the hero swims where the keys point and stops with them,
    /// on a tether that keeps the body's middle within <see cref="Spec.Tether"/> of where it left the ship;
    /// with a jetpack worn, untethered and flying on (<see cref="HeroInput.Jetpack"/>).
    /// </summary>
    public Hero Step(HeroInput input, Ship ship)
    {
        var after = Move(this, input, ship);
        // A jetpack worn ties no tether: the hero flies free.
        return input.Jetpack ? after with { Anchor = null } : Tethered(this, after, ship);
    }

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
        int x = MoveX(ship, before.X, before.Y, byX, false);
        (int X, int Y) to = (before.X, before.Y);
        foreach (var t in new[]
        {
            (x, MoveY(ship, x, before.Y, byY, false, false)),
            (x, before.Y),
            (before.X, MoveY(ship, before.X, before.Y, byY, false, false)),
        })
        {
            if (!Within(t.Item1, t.Item2)) continue;
            to = t;
            break;
        }
        return after with { X = to.X, Y = to.Y, Vx = to.X - before.X, Vy = to.Y - before.Y, Anchor = anchor };
    }

    /// <summary>How far the grapple pulls the body a step: <c>Harpoon.PullSpeed</c> a tick, rounded to whole thousandths.</summary>
    public const int PullStep = (Stillwake.Content.Harpoon.PullSpeed * Stillwake.Sim.World.TicksPerSecond + 30) / 60;

    private static Hero Move(Hero hero, HeroInput input, Ship ship)
    {
        int facing = input.Face ?? (input.X == 0 ? hero.Facing : input.X);
        if (input.Pull is Point to) return Pulled(ship, hero, to, facing);
        // Swimming, the hero faces the way it swims, whatever the pointer does, unless it swings the axe.
        if (ship.Space(hero.BodyCell(ship.Width))) return Swim(ship, hero, input, input.X == 0 || input.Swings ? facing : input.X);
        bool grab = !hero.Climbing && input.Y != 0 && input.X == 0 && !input.Jump &&
            TouchesLadder(ship, hero.X, hero.Y, input.Y > 0);
        if (grab || hero.Climbing)
        {
            // A ladder centres the hero on it; narrower than a cell, the hero touches nothing beside it but a slope's
            // solid half, which it may not climb into: it stops on the ladder, or does not take it (0069).
            if (input.X == 0 && !input.Jump)
            {
                var climbed = Climb(ship, CellOf(hero.X) * Unit + Unit / 2, hero.Y, input.Y, facing);
                if (Clear(ship, climbed.X, climbed.Y)) return climbed;
                if (hero.Climbing) return hero with { Vx = 0, Vy = 0, Facing = facing };
            }
            // Walking or jumping lets go.
            else return Fall(ship, hero.X, hero.Y, input.X, input.Jump ? -Spec.Jump : 0, facing, input.Sprint);
        }
        int vy = input.Jump && Grounded(ship, hero.X, hero.Y) ? -Spec.Jump : hero.Vy;
        return Fall(ship, hero.X, hero.Y, input.X, vy, facing, input.Sprint, input.Y > 0 && !input.Jump);
    }

    /// <summary>
    /// A step under gravity. Standing, a walk follows the ground up and down slopes (<see cref="Walk"/>) and stops where
    /// the body first fits a hole below it (<see cref="Caught"/>), <paramref name="down"/> with no walk slides the body
    /// into the hole under its middle (<see cref="Slid"/>), and <paramref name="down"/> on floors alone drops through
    /// them (<see cref="OnFloors"/>).
    /// </summary>
    private static Hero Fall(Ship ship, int x, int y, int dir, int vy, int facing, bool sprint, bool down = false)
    {
        int speed = Math.Min(vy + Spec.Gravity, Spec.MaxFall);
        bool standing = vy >= 0 && Grounded(ship, x, y);
        int step = dir * (sprint ? Spec.Run : Spec.Walk);
        var (nx, wy) = standing && dir != 0 ? Walk(ship, x, y, step)
            : standing && down ? (Slid(ship, x, y), y)
            : Aloft(ship, x, y, step);
        // Feet a thousandth into the floor's row: a row the feet are in is never landed on (MoveY).
        int ny = standing && down && OnFloors(ship, nx, wy)
            ? MoveY(ship, nx, wy + 1, speed - 1, false, true)
            : MoveY(ship, nx, wy, speed, false, true);
        if (!Clear(ship, nx, ny)) (nx, wy, ny) = Clear(ship, nx, wy) ? (nx, wy, wy) : (x, y, y);
        return new Hero(nx, ny, nx - x, ny - wy == speed ? speed : 0, false, facing);
    }

    /// <summary>
    /// A walk of <paramref name="dx"/> standing at (<paramref name="x"/>, <paramref name="y"/>), the feet kept on the
    /// ground, in parts that end where the box's leading edge or its middle crosses into another column, so the ground
    /// and the walls ahead are read where the feet are then (0069). After each part the feet step up onto a slope or a
    /// ledge no more than half a width above them, the head permitting, and stick to ground that fell by no more than
    /// the part walked, so a walk down a slope never leaves it. Walked off an edge, the rest is walked in the air.
    /// </summary>
    private static (int X, int Y) Walk(Ship ship, int x, int y, int dx)
    {
        int dir = Math.Sign(dx);
        for (int left = Math.Abs(dx); left > 0;)
        {
            int edge = dir > 0 ? x + Half - 1 : x - Half;
            int toEdge = dir > 0 ? (CellOf(edge) + 1) * Unit - edge : edge - CellOf(edge) * Unit + 1;
            int toMiddle = dir > 0 ? (CellOf(x) + 1) * Unit - x : x - CellOf(x) * Unit + 1;
            int part = Math.Min(left, Math.Min(toEdge, toMiddle));
            int nx = Caught(ship, x, MoveX(ship, x, y, dir * part, true, ledge: true), y);
            if (nx == x) break;
            int moved = Math.Abs(nx - x), ny;
            if (Rise(ship, nx, y) is int top)
            {
                if (y - top > Half || MoveY(ship, nx, y, top - y, false, true) != top) break;
                ny = top;
            }
            else if (Landing(ship, nx, y, moved + 1, false, true) is int ground) ny = ground;
            else if (!Clear(ship, nx, y)) break;
            else return moved < part ? (nx, y) : Aloft(ship, nx, y, dir * (left - moved));
            if (!Clear(ship, nx, ny)) break;
            (x, y) = (nx, ny);
            left -= moved;
            if (moved < part) break;
        }
        return (x, y);
    }

    /// <summary>
    /// A move of <paramref name="dx"/> off the ground: the box stopped by what is whole, the feet lifted onto a slope's
    /// diagonal they moved into when no more than half a width below it and the head has room; else, or where the hero
    /// would not fit (<see cref="Clear"/>), no move.
    /// </summary>
    private static (int X, int Y) Aloft(Ship ship, int x, int y, int dx)
    {
        if (dx == 0) return (x, y);
        int nx = MoveX(ship, x, y, dx, true);
        (int X, int Y) to = Rise(ship, nx, y) is not int top ? (nx, y)
            : y - top <= Half && MoveY(ship, nx, y, top - y, false, true) == top ? (nx, top) : (x, y);
        return Clear(ship, to.X, to.Y) ? to : (x, y);
    }

    /// <summary>
    /// Where the feet at (<paramref name="x"/>, <paramref name="y"/>) would stand if they are inside something: the
    /// first place above them in nothing, a slope's diagonal or a wall's top, out of a wall and up the slope on it;
    /// null when they are in nothing.
    /// </summary>
    private static int? Rise(Ship ship, int x, int y)
    {
        int? top = null;
        // Out of a wall's top the feet may still be in the slope on it: up again, never past a step.
        for (int at = y; y - at <= Half && TopOver(ship, x, at) is int over; at = over) top = over;
        return top;
    }

    /// <summary>The top of the solid the feet at (<paramref name="x"/>, <paramref name="y"/>) are in, or null.</summary>
    private static int? TopOver(Ship ship, int x, int y)
    {
        int column = CellOf(x), row = CellOf(y - 1);
        int slope = SlopeAt(ship, column, row);
        if (slope != Shape.None)
            return Shape.Solid(slope, x - column * Unit, y - 1 - row * Unit) ? row * Unit + Shape.TopAt(slope, x - column * Unit) : null;
        return Whole(ship, column, row) ? row * Unit : null;
    }

    /// <summary>
    /// The body's middle pulled straight toward <paramref name="to"/> by <see cref="PullStep"/>, landing on it,
    /// walls and closed doors stopping it as anything does; no gravity, so pulled there it stays.
    /// </summary>
    private static Hero Pulled(Ship ship, Hero hero, Point to, int facing)
    {
        var middle = hero.Middle;
        int dx = to.X - middle.X, dy = to.Y - middle.Y;
        double far = Distance(dx, dy);
        if (far > PullStep) (dx, dy) = ((int)Math.Round(dx / far * PullStep), (int)Math.Round(dy / far * PullStep));
        int nx = MoveX(ship, hero.X, hero.Y, dx, false);
        int ny = MoveY(ship, nx, hero.Y, dy, false, false);
        if (!Clear(ship, nx, ny)) (nx, ny) = Clear(ship, nx, hero.Y) ? (nx, hero.Y) : (hero.X, hero.Y);
        return new Hero(nx, ny, nx - hero.X, ny - hero.Y, false, facing);
    }

    /// <summary>
    /// A step in space: where the keys point at <c>Hero.Swim</c>, stopping with them; with a jetpack worn,
    /// on at its speed (<see cref="Flown"/>). Walls stop it, and what a wall stops it loses.
    /// </summary>
    private static Hero Swim(Ship ship, Hero hero, HeroInput input, int facing)
    {
        var (vx, vy) = input.Jetpack ? Flown(hero.Vx, hero.Vy, input) : (input.X * Spec.Swim, input.Y * Spec.Swim);
        int nx = MoveX(ship, hero.X, hero.Y, vx, false);
        int ny = MoveY(ship, nx, hero.Y, vy, false, false);
        if (!Clear(ship, nx, ny)) (nx, ny) = Clear(ship, nx, hero.Y) ? (nx, hero.Y) : (hero.X, hero.Y);
        return new Hero(nx, ny, nx - hero.X, ny - hero.Y, false, facing);
    }

    /// <summary>
    /// A jetpack's speed after a step from (<paramref name="vx"/>, <paramref name="vy"/>), never past
    /// <c>Jetpack.Top</c> (a fall carried out into space is cut to it): thrusting, gaining <c>Jetpack.Accel</c>
    /// along the thrust; coasting, nearing what the keys swim by <c>Jetpack.Drag</c>, landing on it.
    /// </summary>
    public static (int X, int Y) Flown(int vx, int vy, HeroInput input)
    {
        double x = vx, y = vy;
        if (input.Thrust is Point dir) (x, y) = (x + (double)dir.X * Pack.Accel / Unit, y + (double)dir.Y * Pack.Accel / Unit);
        else
        {
            int tx = input.X * Spec.Swim, ty = input.Y * Spec.Swim;
            double dx = x - tx, dy = y - ty, off = Distance(dx, dy);
            (x, y) = off <= Pack.Drag ? (tx, ty) : (x - dx / off * Pack.Drag, y - dy / off * Pack.Drag);
        }
        double speed = Distance(x, y);
        if (speed > Pack.Top) (x, y) = (x / speed * Pack.Top, y / speed * Pack.Top);
        return ((int)Math.Round(x), (int)Math.Round(y));
    }

    /// <summary>
    /// Which way a jetpack thrusts, a unit vector in thousandths (0047): where the movement keys
    /// (<paramref name="x"/>, <paramref name="y"/>) point, or with none held toward <paramref name="pointer"/>,
    /// in cells, from the body's <paramref name="middle"/>; none with no key held and no pointer, or the pointer
    /// on the middle.
    /// </summary>
    public static Point? ThrustToward(int x, int y, Point middle, (double X, double Y)? pointer)
    {
        double dx = x, dy = y;
        if (x == 0 && y == 0)
        {
            if (pointer is not var (px, py)) return null;
            (dx, dy) = (px * Unit - middle.X, py * Unit - middle.Y);
        }
        double far = Distance(dx, dy);
        return far < 1 ? null : new Point((int)Math.Round(dx / far * Unit), (int)Math.Round(dy / far * Unit));
    }

    private static double Distance(double dx, double dy) => Math.Sqrt(dx * dx + dy * dy);

    private static Hero Climb(Ship ship, int x, int y, int dir, int facing)
    {
        int ny = MoveY(ship, x, y, dir * Spec.Climb, true, true);
        int column = CellOf(x);
        int before = CellOf(y - 1);
        int after = CellOf(ny - 1);
        // The feet rose past the ladder's top: the hero steps off and stands on it.
        if (after < before && Climbable(ship, column, before) && !Climbable(ship, column, after))
            return new Hero(x, before * Unit, 0, 0, false, facing);
        return new Hero(x, ny, 0, ny - y, TouchesLadder(ship, x, ny, false), facing);
    }

    private static bool Grounded(Ship ship, int x, int y) => MoveY(ship, x, y, 1, false, true) == y;

    /// <summary>
    /// Where a hero standing at <paramref name="y"/> and walking from <paramref name="x"/> to <paramref name="nx"/>
    /// stops: at the first place on the way where its body fits over a hole below the feet, or
    /// <paramref name="nx"/>. A step is longer than the 50 thousandths a cell leaves beside the body, so
    /// unstopped a walk steps over a hole one cell wide or into it by where it began (playtest 1).
    /// </summary>
    private static int Caught(Ship ship, int x, int nx, int y)
    {
        int row = CellOf(y);
        int dir = Math.Sign(nx - x);
        for (int c = CellOf(x); c != CellOf(nx) + dir; c += dir)
        {
            if (!Open(ship, c, row)) continue;
            int lo = c * Unit + Half, hi = (c + 1) * Unit - Half;
            if (dir > 0 && x < lo && nx >= lo) return Math.Min(nx, hi);
            if (dir < 0 && x > hi && nx <= hi) return Math.Max(nx, lo);
        }
        return nx;
    }

    /// <summary>
    /// <paramref name="x"/> moved a walk's step at most toward where the body fits over the hole under its
    /// middle, if there is one below the feet: down over a hole drops the hero in.
    /// </summary>
    private static int Slid(Ship ship, int x, int y)
    {
        int c = CellOf(x);
        if (!Open(ship, c, CellOf(y))) return x;
        int to = Math.Clamp(x, c * Unit + Half, (c + 1) * Unit - Half);
        return MoveX(ship, x, y, Math.Clamp(to - x, -Spec.Walk, Spec.Walk), true);
    }

    /// <summary>
    /// Whether what holds up a hero standing at (<paramref name="x"/>, <paramref name="y"/>) is floors only,
    /// which down drops through: no wall, slope, closed door or ladder top under any part of the feet.
    /// </summary>
    private static bool OnFloors(Ship ship, int x, int y)
    {
        int row = CellOf(y);
        bool floor = false;
        for (int c = CellOf(x - Half); c <= CellOf(x + Half - 1); c++)
        {
            if (Solid(ship, c, row) || Climbable(ship, c, row)) return false;
            floor |= StructureAt(ship, c, row) == Structure.Floor;
        }
        return floor;
    }

    /// <summary>Whether a cell lets a hero fall into it from above: nothing in it blocks or holds; a slope holds.</summary>
    private static bool Open(Ship ship, int x, int y) => !Solid(ship, x, y) && !Holds(ship, x, y, false);

    /// <summary>Whether the hero's column holds a ladder beside the body, or also just below the feet.</summary>
    private static bool TouchesLadder(Ship ship, int x, int y, bool belowFeet)
    {
        int column = CellOf(x);
        int last = CellOf(belowFeet ? y : y - 1);
        for (int row = CellOf(y - Spec.Height); row <= last; row++)
            if (Climbable(ship, column, row)) return true;
        return false;
    }

    /// <summary>
    /// <paramref name="x"/> moved by <paramref name="dx"/>, stopped flush against the first blocking column the leading
    /// edge enters. With <paramref name="slopes"/>, a floor slope blocks only through its closed side (◢ from the right,
    /// ◣ from the left); through its open side the box goes in and the feet climb the diagonal. Else every wall is whole,
    /// as swimming and pulled. With <paramref name="ledge"/>, walking, a wall whose top is no more than half a width
    /// above the feet does not stop the box: at 45° the leading edge meets the next column exactly that far above the
    /// feet, so the equal case passes, or every ramp would stop at its top (0069).
    /// </summary>
    private static int MoveX(Ship ship, int x, int y, int dx, bool slopes, bool ledge = false)
    {
        int top = CellOf(y - Spec.Height);
        int bottom = CellOf(y - 1);
        bool Blocked(int column, int side)
        {
            for (int row = top; row <= bottom; row++)
            {
                int slope = slopes ? SlopeAt(ship, column, row) : Shape.None;
                bool stops = slope != Shape.None ? (Shape.ClosedSides(slope) & side) != 0 : Solid(ship, column, row);
                if (stops && !(ledge && row * Unit >= y - Half)) return true;
            }
            return false;
        }
        int to = x + dx;
        if (dx > 0)
        {
            for (int c = CellOf(x + Half - 1) + 1; c <= CellOf(x + Half + dx - 1); c++)
                if (Blocked(c, Doorways.Left)) { to = c * Unit - Half; break; }
        }
        else if (dx < 0)
        {
            for (int c = CellOf(x - Half) - 1; c >= CellOf(x - Half + dx); c--)
                if (Blocked(c, Doorways.Right)) { to = (c + 1) * Unit + Half; break; }
        }
        // Every shape whole, the feet still never move into a slope the box was let into by walking.
        return slopes || to == x || Rise(ship, to, y) == null ? to : x;
    }

    /// <summary>
    /// <paramref name="y"/> moved by <paramref name="dy"/>, stopped flush against the first blocking row the leading
    /// edge enters, or going down where the feet land (<see cref="Landing"/>). Rising, every wall stops the head, a
    /// slope's closed bottom too.
    /// </summary>
    private static int MoveY(Ship ship, int x, int y, int dy, bool climbing, bool slopes)
    {
        if (dy > 0) return Landing(ship, x, y, dy, climbing, slopes) ?? y + dy;
        if (dy < 0)
        {
            int left = CellOf(x - Half);
            int right = CellOf(x + Half - 1);
            int top = y - Spec.Height;
            for (int r = CellOf(top) - 1; r >= CellOf(top + dy); r--)
                for (int c = left; c <= right; c++)
                    if (Solid(ship, c, r)) return (r + 1) * Unit + Spec.Height;
        }
        return y + dy;
    }

    /// <summary>
    /// Where feet falling from <paramref name="y"/> by up to <paramref name="reach"/> first come to rest, or null. Each
    /// column under the box holds them at its ground (<see cref="GroundIn"/>). Mixed footing stands on the higher, but
    /// a corner no more than half a width above the ground under the middle gives way to it: down a slope off a plateau
    /// the feet follow the slope rather than hang on the corner and drop (0069).
    /// </summary>
    private static int? Landing(Ship ship, int x, int y, int reach, bool climbing, bool slopes)
    {
        int middle = CellOf(x);
        int? under = GroundIn(ship, middle, x, y, reach + Half, climbing, slopes);
        int? best = under <= y + reach ? under : null;
        for (int c = CellOf(x - Half); c <= CellOf(x + Half - 1); c++)
        {
            if (c == middle || GroundIn(ship, c, x, y, reach, climbing, slopes) is not int at) continue;
            if (under is int u && at <= u && u - at <= Half) continue;
            best = Math.Min(best ?? at, at);
        }
        return best;
    }

    /// <summary>
    /// The highest ground in <paramref name="column"/> for a hero whose middle is at <paramref name="x"/>, at or below
    /// <paramref name="y"/> and no further than <paramref name="reach"/> below it; null for none. Whole cells, floors and
    /// ladder tops hold the box's bottom. A slope holds the middle of the feet on its diagonal, and beside the middle
    /// the box's bottom at its point nearest the middle and the box above its lowest half width at its highest point
    /// under the box. A row the feet are in is never ground but for a slope's diagonal below them, which lets a hero up
    /// through a floor from below. With <paramref name="slopes"/> off every shape is whole but a slope the feet are in.
    /// </summary>
    private static int? GroundIn(Ship ship, int column, int x, int y, int reach, bool climbing, bool slopes)
    {
        int left = column * Unit, middle = CellOf(x), feet = CellOf(y - 1);
        int a = Math.Max(0, x - Half - left), b = Math.Min(Unit - 1, x + Half - 1 - left);
        int near = column == middle ? x - left : column < middle ? b : a;
        int? best = null;
        void Take(int at)
        {
            if (at >= y && at <= y + reach) best = Math.Min(best ?? at, at);
        }
        for (int r = CellOf(y - Half - 1); r <= CellOf(y + reach); r++)
        {
            int slope = slopes || r == feet ? SlopeAt(ship, column, r) : Shape.None;
            if (slope != Shape.None)
            {
                Take(r * Unit + Shape.TopAt(slope, near));
                if (slopes && column != middle) Take(r * Unit + Math.Min(Shape.TopAt(slope, a), Shape.TopAt(slope, b)) + Half);
            }
            else if (Solid(ship, column, r) || Holds(ship, column, r, climbing)) Take(r * Unit);
        }
        return best;
    }

    /// <summary>
    /// Whether the hero fits at (<paramref name="x"/>, <paramref name="y"/>): the middle of its feet in nothing solid,
    /// and its box above the lowest half width clear of every wall, closed door and slope's solid half. Only that lowest
    /// band may overlap: the uphill side of a slope, a ledge stepped onto (0069). A move that would end otherwise is not made.
    /// </summary>
    private static bool Clear(Ship ship, int x, int y)
    {
        if (Rise(ship, x, y) != null) return false;
        int band = y - Half;
        for (int c = CellOf(x - Half); c <= CellOf(x + Half - 1); c++)
        {
            int a = Math.Max(0, x - Half - c * Unit), b = Math.Min(Unit - 1, x + Half - 1 - c * Unit);
            for (int r = CellOf(y - Spec.Height); r <= CellOf(band - 1); r++)
            {
                int slope = SlopeAt(ship, c, r);
                if (slope == Shape.None ? Solid(ship, c, r) : r * Unit + Math.Min(Shape.TopAt(slope, a), Shape.TopAt(slope, b)) < band) return false;
            }
        }
        return true;
    }

    /// <summary>The cell a length in thousandths falls in, rounding down below zero too.</summary>
    private static int CellOf(int v) => v >= 0 ? v / Unit : -((Unit - 1 - v) / Unit);

    private static int? StructureAt(Ship ship, int x, int y) =>
        x < 0 || y < 0 || x >= ship.Width || y >= ship.Height ? null : ship.StructureAt(y * ship.Width + x);

    /// <summary>Walls of any shape, closed doors, and everything past the grid's edge.</summary>
    private static bool Solid(Ship ship, int x, int y) =>
        StructureAt(ship, x, y) is null or Structure.Wall or Structure.DoorClosed;

    /// <summary>What holds the whole box out: <see cref="Solid"/> but a floor slope. A ceiling slope is whole for now (0069).</summary>
    private static bool Whole(Ship ship, int x, int y) => Solid(ship, x, y) && SlopeAt(ship, x, y) == Shape.None;

    /// <summary>The floor slope (◢ ◣) a cell's wall is cut to, or <see cref="Shape.None"/>.</summary>
    private static int SlopeAt(Ship ship, int x, int y)
    {
        if (StructureAt(ship, x, y) != Structure.Wall) return Shape.None;
        int shape = ship.ShapeAt(y * ship.Width + x);
        return Shape.IsFloor(shape) ? shape : Shape.None;
    }

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
