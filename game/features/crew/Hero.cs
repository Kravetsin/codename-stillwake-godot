using System;
using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Rooms;
using Pack = Stillwake.Content.Jetpack;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Features.Crew;

/// <summary>A point in thousandths of a cell.</summary>
public readonly record struct Point(int X, int Y);

/// <summary>
/// What the player holds during one step; <see cref="Y"/> is -1 for up, 1 for down: down a ladder, else into
/// a hole two cells wide or through the floors stood on. <see cref="Jump"/> is set once
/// per press. <see cref="Face"/> is the side the pointer is on, which the hero faces whichever way it
/// walks; with none, it faces the way it walks; swimming, the way it swims. <see cref="Sprint"/>, Shift
/// held, runs aboard; in space it only thrusts, with a jetpack worn. <see cref="Swings"/>, the axe
/// mid-swing (0039) or the harpoon aimed (0044): it faces <see cref="Face"/> swimming too, so the blade comes down where the swing was aimed and the gun
/// points at the pointer. <see cref="Pull"/>, the harpoon's grapple (0044): the body's middle is pulled
/// toward that point, keys and gravity aside, and held there once it is. <see cref="Jetpack"/>, a jetpack worn
/// (0047): in space the hero keeps its speed, ties no tether, and while <see cref="Thrust"/> is set, a unit
/// vector in thousandths, gains <c>Jetpack.Accel</c> a step that way up to <c>Jetpack.Top</c>; else it slows
/// by <c>Jetpack.Drag</c> a step toward what the keys swim. <see cref="JumpHeld"/>, Space held down: let go while a
/// jump rises, it rises less (<c>Hero.JumpCut</c>); the step <see cref="Jump"/> is set counts as held too. <see cref="Reeling"/>,
/// knocked by a ram (0103): the keys aside, no ladder taken, the knock's speed kept in the air and braked on the floor.
/// </summary>
public readonly record struct HeroInput(
    int X, int Y, bool Jump, int? Face = null, bool Sprint = false, bool Swings = false, Point? Pull = null,
    bool Jetpack = false, Point? Thrust = null, bool JumpHeld = false, bool Reeling = false);

/// <summary>
/// The cells the hero moves against; which of them are space, where the
/// hero floats; and which are the hull, where a tether is not yet needed: any cell with a structure or a back
/// wall, aired or not. <paramref name="gravity"/> is what a fall gains a step, the ship's unless its surface's
/// kind scales it (0076): a planet's lower one, where nothing floats, lets the hero jump higher and farther.
/// </summary>
public sealed class Ship(
    int width, int height, Func<int, int> structureAt, Func<int, bool> space, Func<int, bool>? hull = null,
    int gravity = Spec.Gravity, int falling = Spec.FallGravity)
{
    /// <summary>What a fall gains a step, in thousandths of a cell a step.</summary>
    public int Gravity => gravity;

    /// <summary>What a hero gains a step coming down: <paramref name="falling"/> thousandths of <see cref="Gravity"/>, rounded down (0089).</summary>
    public int FallGravity => gravity * falling / 1000;

    /// <summary>What a held jump gains a step at its top: <c>Hero.ApexGravity</c> thousandths of <see cref="Gravity"/>, rounded down (0089).</summary>
    public int ApexGravity => gravity * Spec.ApexGravity / 1000;

    public int Width => width;
    public int Height => height;

    /// <summary>The <see cref="Structure"/> code of a cell on the grid.</summary>
    public int StructureAt(int cell) => structureAt(cell);


    public bool Space(int cell) => space(cell);

    public bool Hull(int cell) => hull?.Invoke(cell) ?? structureAt(cell) != Structure.Empty;

    /// <summary>
    /// The ship as the sim's grid and rooms see it now, its cells' structure as <paramref name="structureAt"/>
    /// gives it when given: doors as this peer shows them, its own toggles in flight made (0014). Under the rules of
    /// its surface's kind, <paramref name="rules"/>, the ship's when none: the hero floats only where the rooms say
    /// something does, never on a planet, and falls by the kind's gravity (0076), coming down the faster under a
    /// planet's own (<c>Hero.PlanetFallGravity</c>, 0089).
    /// </summary>
    public static Ship Of(IGridQueries grid, IRoomsQueries rooms, Func<int, int>? structureAt = null, Stillwake.Content.Surfaces.Kind? rules = null)
    {
        var kind = rules ?? Stillwake.Content.Surfaces.Ship;
        return new(grid.Width, grid.Height, structureAt ?? grid.StructureAt, rooms.Afloat,
            cell => (structureAt ?? grid.StructureAt)(cell) != Structure.Empty || grid.BackAt(cell) != null,
            kind.Fall(Spec.Gravity), kind.Floats ? Spec.FallGravity : Spec.PlanetFallGravity);
    }
}

/// <summary>
/// One hero's movement (apps/game/src/features/crew/hero.ts): <see cref="X"/> is the middle of the box
/// and <see cref="Y"/> its bottom, where the feet are, in thousandths of a cell. <see cref="Anchor"/>,
/// out in space, is where the tether is fixed: the body's middle where it last was in a cell not in space
/// (0007, 0104); none aboard. The jump's timers (0089): <see cref="Airborne"/>, steps since the feet last stood, kept at
/// most one past <c>Hero.Coyote</c>, and that once a jump is spent or before the hero first stands, so one made in
/// mid-air cannot jump; <see cref="Buffered"/>, the steps a press of Space made in the air still jumps on landing;
/// <see cref="Rising"/>, a jump rising that letting go of Space cuts. <see cref="LetGo"/>, steps since the hands let go of a
/// ladder by a jump or a walk, kept at most one past <c>Hero.Regrab</c>: up grabs none before then (0092). <see cref="Leaping"/>,
/// in the air from a jump, off the floor or a ladder, till the feet stand: only a jump steps up onto a ledge it fell short of.
/// </summary>
public readonly record struct Hero(
    int X, int Y, int Vx, int Vy, bool Climbing, int Facing, Point? Anchor = null,
    int Airborne = Spec.Coyote + 1, int Buffered = 0, bool Rising = false, int LetGo = Spec.Regrab + 1, bool Leaping = false)
{
    /// <summary>Thousandths of a cell: the unit of every length and speed in <see cref="Spec"/>.</summary>
    public const int Unit = 1000;

    /// <summary>The controller steps 60 times a second, whatever the frame rate.</summary>
    public const double StepMs = 1000.0 / 60;

    /// <summary>
    /// How far the body's middle is above the feet: half the height rounded down, so every position stays a whole
    /// number (the TypeScript's 1312.5 at 16 px; 2625 now, 0104).
    /// </summary>
    public const int MiddleUp = Spec.Height / 2;

    private const int Half = Spec.Width / 2;

    /// <summary>
    /// A hero standing on the bottom of <paramref name="cell"/>, its middle on the cell's right edge: the middle of the 16 px block
    /// whose bottom-left a map's S or a lifepod's hatch is (0104), where a body nearly two cells wide fits.
    /// </summary>
    public static Hero At(Ship ship, int cell) =>
        new((cell % ship.Width + 1) * Unit, (cell / ship.Width + 1) * Unit, 0, 0, false, 1);

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
        if (ShutOut(ship, this) is Hero pushed) return pushed;
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
        // Tied where the body's middle last was in the hull, not its cell's middle: at 8 px a cell's middle is under a standing
        // hero's, so the tether would sink it below the floor it walked off and the floor's end would stop it swimming back (0104).
        Point? tied = before.Anchor ?? (ship.Space(left) && !ship.Hull(left) ? null : before.Middle);
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

    /// <summary>How far a door shut on the hero pushes it a step: two cells, a 16 px one (0104), in the 200 ms a door takes to slide.</summary>
    public const int PushStep = Unit / 6;

    /// <summary>
    /// The hero pushed a step out of a door or hatch shut on it, keys aside: a closed door in its box where it no longer
    /// fits, by another peer or its own hand. Toward the nearest place it fits beside the doorway, on top of it or under
    /// it, the way it faces first on a tie. Null when no closed door holds it, or none of those places fits (doors shut
    /// on either side of it): it stays, as before.
    /// </summary>
    private static Hero? ShutOut(Ship ship, Hero hero)
    {
        if (Clear(ship, hero.X, hero.Y)) return null;
        int left = int.MaxValue, right = int.MinValue, top = int.MaxValue, bottom = int.MinValue;
        for (int c = CellOf(hero.X - Half); c <= CellOf(hero.X + Half - 1); c++)
            for (int r = CellOf(hero.Y - Spec.Height); r <= CellOf(hero.Y - 1); r++)
                if (StructureAt(ship, c, r) is int structure && Doorways.IsClosed(structure))
                    (left, right, top, bottom) = (Math.Min(left, c), Math.Max(right, c), Math.Min(top, r), Math.Max(bottom, r));
        if (left == int.MaxValue) return null;
        int x = hero.X, y = hero.Y, besideLeft = left * Unit - Half, besideRight = (right + 1) * Unit + Half;
        (int X, int Y)[] outs = hero.Facing < 0
            ? [(besideLeft, y), (besideRight, y), (x, top * Unit), (x, (bottom + 1) * Unit + Spec.Height)]
            : [(besideRight, y), (besideLeft, y), (x, top * Unit), (x, (bottom + 1) * Unit + Spec.Height)];
        (int X, int Y)? to = null;
        int far = int.MaxValue;
        foreach (var o in outs)
        {
            int d = Math.Abs(o.X - x) + Math.Abs(o.Y - y);
            if (d < far && Clear(ship, o.X, o.Y)) (to, far) = (o, d);
        }
        if (to is not { } way) return null;
        int nx = x + Math.Clamp(way.X - x, -PushStep, PushStep), ny = y + Math.Clamp(way.Y - y, -PushStep, PushStep);
        // No speed kept: lifted onto a hatch, the hero lands on it rather than hopping off it, and pushed out of a door it
        // stops beside it rather than walking on (0089).
        return new Hero(nx, ny, 0, 0, false, hero.Facing, hero.Anchor);
    }

    /// <summary>How far the grapple pulls the body a step: <c>Harpoon.PullSpeed</c> a tick, rounded to whole thousandths.</summary>
    public const int PullStep = (Stillwake.Content.Harpoon.PullSpeed * Stillwake.Sim.World.TicksPerSecond + 30) / 60;

    private static Hero Move(Hero hero, HeroInput input, Ship ship)
    {
        int facing = input.Face ?? (input.X == 0 ? hero.Facing : input.X);
        if (input.Pull is Point to) return Pulled(ship, hero, to, facing);
        // Swimming, the hero faces the way it swims, whatever the pointer does, unless it swings the axe.
        if (ship.Space(hero.BodyCell(ship.Width))) return Swim(ship, hero, input, input.X == 0 || input.Swings ? facing : input.X);
        if (input.Reeling)
        {
            // Knocked by a ram (0103): thrown by it, not walked, so the air's control does not eat the knock; the floor brakes it.
            bool footed = hero.Vy >= 0 && Grounded(ship, hero.X, hero.Y);
            return Fall(ship, hero.X, hero.Y, footed ? Paced(hero.Vx, default, true) : hero.Vx, hero.Vy, facing, false);
        }
        bool held = input.Jump || input.JumpHeld;
        int letGo = Math.Min(hero.LetGo + 1, Spec.Regrab + 1);
        // Up grabs a ladder walking, running or rising too, but not for a moment after letting go of one, so a jump off it
        // with up held goes on; down grabs one standing or falling, no side held (0092).
        bool grab = !hero.Climbing && !input.Jump &&
            (input.Y < 0 && letGo > Spec.Regrab && TouchesLadder(ship, hero.X, hero.Y, false, overHead: true)
                || input.Y > 0 && input.X == 0 && hero.Vy >= 0 && TouchesLadder(ship, hero.X, hero.Y, true));
        if (grab || hero.Climbing)
        {
            bool footed = Grounded(ship, hero.X, hero.Y);
            // A jump lets go: straight up as from the floor, or to a side, leaping off across and less high (0092).
            if (input.Jump)
            {
                int across = input.X * Spec.Run * Spec.LadderJumpAcross / 1000;
                int rise = input.X == 0 ? Spec.Jump : Spec.Jump * Spec.LadderJumpRise / 1000;
                return Fall(ship, hero.X, hero.Y, across, -rise, facing, held) with { Rising = true, LetGo = 0, Leaping = true };
            }
            // A side held in mid-air only turns the hero on it, so a jump that follows goes that way (0077); on the ground it
            // walks off, unless up is held too, which climbs.
            if (input.X != 0 && input.Y >= 0 && footed)
                return Fall(ship, hero.X, hero.Y, Paced(hero.Vx, input, footed), 0, facing, held) with { LetGo = 0 };
            // The hero eases onto the ladder's middle; narrower than its two cells, it may still meet a wall beside it there,
            // which it may not climb into: it stops on the ladder, or does not take it.
            int x = Centred(hero.X, LadderMiddle(ship, hero.X, hero.Y));
            if (x != hero.X && !Clear(ship, x, hero.Y)) x = hero.X;
            int speed = input.Y < 0 ? Spec.ClimbUp : input.Sprint ? Spec.Slide : Spec.ClimbDown;
            var climbed = Climb(ship, x, hero.Y, input.Y, speed, facing) with { LetGo = letGo };
            if (Clear(ship, climbed.X, climbed.Y)) return climbed;
            if (hero.Climbing) return hero with { X = x, Vx = 0, Vy = 0, Facing = facing, LetGo = letGo };
        }
        // A press counts for the buffer's steps, and the ground for the coyote's after the feet leave it (0089).
        bool grounded = hero.Vy >= 0 && Grounded(ship, hero.X, hero.Y);
        // Pressed against a ledge the jump falls just short of, rising no faster than the assist lifts, it steps up onto it.
        if (hero.Leaping && !grounded && input.X != 0 && !input.Jump && hero.Vy >= -Spec.MantleRise && Mantle(ship, hero, input.X, facing) is Hero up)
            return up with { LetGo = letGo, Leaping = true };
        int airborne = grounded ? 0 : Math.Min(hero.Airborne + 1, Spec.Coyote + 1);
        bool jumps = (input.Jump || hero.Buffered > 0) && airborne <= Spec.Coyote;
        int vy = jumps ? -Spec.Jump : hero.Vy;
        bool rising = jumps || hero.Rising;
        if (rising && !held && vy < 0) (vy, rising) = (vy * Spec.JumpCut / 1000, false);
        var after = Fall(ship, hero.X, hero.Y, Paced(hero.Vx, input, grounded), vy, facing, held, input.Y > 0 && !input.Jump);
        return after with
        {
            Airborne = jumps ? Spec.Coyote + 1 : airborne,
            Buffered = jumps ? 0 : input.Jump ? Spec.JumpBuffer : Math.Max(0, hero.Buffered - 1),
            Rising = rising && vy < 0,
            Leaping = jumps || hero.Leaping && !grounded,
            LetGo = letGo,
        };
    }

    /// <summary>
    /// How far a jump falls short of a ledge yet looks on it, in thousandths of a cell: the drawn foot's height over the feet at
    /// <paramref name="vy"/> (<see cref="Poses.FootLift"/>), rounded down; about 0.6 of a cell at a jump's top, little falling fast.
    /// </summary>
    public static int MantleReach(int vy) => (int)(Poses.FootLift(vy) * Unit / Rig.Cell);

    /// <summary>
    /// The top of the whole wall the feet at (<paramref name="x"/>, <paramref name="y"/>) are pressed against going
    /// <paramref name="dir"/>, in thousandths of a cell, where the hero fits standing on it <c>Hero.MantleAcross</c> over its
    /// edge with room to rise there; null for none, or the feet over it already.
    /// </summary>
    private static int? LedgeAhead(Ship ship, int x, int y, int dir)
    {
        if (MoveX(ship, x, y, dir) != x) return null;
        int column = CellOf(dir > 0 ? x + Half : x - Half - 1), row = CellOf(y - 1);
        if (!Solid(ship, column, row)) return null;
        while (row > 0 && Solid(ship, column, row - 1)) row--;
        int top = row * Unit;
        if (top >= y || MoveY(ship, x, y, top - y, false) != top) return null;
        int onto = MoveX(ship, x, top, dir * Spec.MantleAcross);
        return onto != x && Clear(ship, onto, top) ? top : null;
    }

    /// <summary>
    /// The ledge assist (0092): a step of a hero pressed <paramref name="dir"/> against a ledge its feet are short of by no more
    /// than its drawn foot clears (<see cref="MantleReach"/>): raised by up to <c>Hero.MantleRise</c>, and the step its feet reach
    /// the top, put <c>Hero.MantleAcross</c> onto it, standing. Null where it does not apply: the jump falls as any.
    /// </summary>
    private static Hero? Mantle(Ship ship, Hero hero, int dir, int facing)
    {
        if (LedgeAhead(ship, hero.X, hero.Y, dir) is not int top || hero.Y - top > MantleReach(hero.Vy)) return null;
        int lift = Math.Min(hero.Y - top, Spec.MantleRise), y = hero.Y - lift;
        if (y > top) return new Hero(hero.X, y, 0, -lift, false, facing);
        int x = MoveX(ship, hero.X, y, dir * Spec.MantleAcross);
        return new Hero(x, y, x - hero.X, 0, false, facing);
    }

    /// <summary>
    /// How far short of a ledge it steps up onto <paramref name="hero"/>'s feet are, in thousandths of a cell, rising or still in
    /// the air against it, facing it or not; none otherwise. What the figure pulls up onto, read alike for every hero from where
    /// it is (0092).
    /// </summary>
    public static int? MantleLedge(Ship ship, Hero hero)
    {
        if (hero.Climbing || hero.Vy >= 0 || hero.Vy < -Spec.MantleRise || ship.Space(hero.BodyCell(ship.Width))) return null;
        foreach (int dir in (ReadOnlySpan<int>)[hero.Facing, -hero.Facing])
            if (LedgeAhead(ship, hero.X, hero.Y, dir) is int top && hero.Y - top <= MantleReach(hero.Vy)) return hero.Y - top;
        return null;
    }

    /// <summary><paramref name="x"/> moved <c>Hero.LadderCentring</c> of the way to a ladder's <paramref name="middle"/>, onto it once within <c>Hero.LadderSnap</c>.</summary>
    private static int Centred(int x, int middle)
    {
        int to = x + (middle - x) * Spec.LadderCentring / 1000;
        return Math.Abs(middle - to) < Spec.LadderSnap ? middle : to;
    }

    /// <summary>
    /// The walk's speed after a step from <paramref name="vx"/>: gaining <c>Hero.WalkAccel</c> or <c>Hero.RunAccel</c>
    /// toward what the keys walk or run, else losing <c>Hero.Stop</c> toward it, landing on it; in the air by
    /// <c>Hero.AirControl</c> of either (0089).
    /// </summary>
    private static int Paced(int vx, HeroInput input, bool grounded)
    {
        int top = input.Sprint ? Spec.Run : Spec.Walk, target = input.X * top;
        bool gains = input.X != 0 && (vx == 0 || Math.Sign(vx) == input.X) && Math.Abs(vx) <= top;
        int rate = gains ? (input.Sprint ? Spec.RunAccel : Spec.WalkAccel) : Spec.Stop;
        if (!grounded) rate = rate * Spec.AirControl / 1000;
        return vx < target ? Math.Min(target, vx + rate) : Math.Max(target, vx - rate);
    }

    /// <summary>
    /// A step under gravity, moving <paramref name="vx"/> across. Standing, a walk follows the ground up and down steps
    /// (<see cref="Walk"/>) and stops where the body first fits a hole below it (<see cref="Caught"/>), <paramref name="down"/>
    /// with no walk slides the body into the hole under its middle (<see cref="Slid"/>), and <paramref name="down"/> on
    /// floors alone drops through them (<see cref="OnFloors"/>). Off the ground, a fall gains <see cref="Ship.FallGravity"/>,
    /// and a jump's top with Space <paramref name="held"/> <see cref="Ship.ApexGravity"/> (0089).
    /// </summary>
    private static Hero Fall(Ship ship, int x, int y, int vx, int vy, int facing, bool held, bool down = false)
    {
        bool standing = vy >= 0 && Grounded(ship, x, y);
        int gravity = standing ? ship.Gravity
            : held && Math.Abs(vy) < Spec.Apex ? ship.ApexGravity
            : vy > 0 ? ship.FallGravity
            : ship.Gravity;
        int speed = Math.Min(vy + gravity, Spec.MaxFall);
        var (nx, wy) = standing && vx != 0 ? Walk(ship, x, y, vx)
            : standing && down ? (Slid(ship, x, y), y)
            : Aloft(ship, x, y, vx);
        // Feet a thousandth into the floor's row: a row the feet are in is never landed on (MoveY).
        int ny = standing && down && OnFloors(ship, nx, wy)
            ? MoveY(ship, nx, wy + 1, speed - 1, false)
            : MoveY(ship, nx, wy, speed, false);
        if (!Clear(ship, nx, ny)) (nx, wy, ny) = Clear(ship, nx, wy) ? (nx, wy, wy) : (x, y, y);
        bool falls = speed > 0 && ny - wy == speed;
        // A walk ended where the body drops off an edge or into a hole keeps its speed; only what stops the body takes it (0089).
        int across = falls && nx - x != vx && vx != 0 && MoveX(ship, nx, ny, Math.Sign(vx)) != nx ? vx : nx - x;
        return new Hero(nx, ny, across, ny - wy == speed ? speed : 0, false, facing);
    }

    /// <summary>
    /// A walk of <paramref name="dx"/> standing at (<paramref name="x"/>, <paramref name="y"/>), the feet kept on the ground, in
    /// parts that end as the box's leading edge reaches the end of its column, so each column ahead is met where the feet are
    /// then (0106). Stopped by ground no more than <c>Hero.StepUp</c> above the feet, the feet step up onto it, the head
    /// permitting (<see cref="StepOnto"/>); after each part they follow ground that fell by no more than that, never leaving it.
    /// Walked off a higher edge, the rest is walked in the air; over a hole the body fits, the walk ends (<see cref="Caught"/>).
    /// The step is whole in a step of the controller: the figure eases up and down it (<see cref="StepEase"/>).
    /// </summary>
    private static (int X, int Y) Walk(Ship ship, int x, int y, int dx)
    {
        int dir = Math.Sign(dx);
        for (int left = Math.Abs(dx); left > 0;)
        {
            int edge = dir > 0 ? x + Half - 1 : x - Half;
            int inColumn = dir > 0 ? (CellOf(edge) + 1) * Unit - 1 - edge : edge - CellOf(edge) * Unit;
            int part = Math.Min(left, inColumn > 0 ? inColumn : 1);
            int to = MoveX(ship, x, y, dir * part), up = y;
            if (to != x + dir * part && StepOnto(ship, x, y, x + dir * part) is int top) (to, up) = (x + dir * part, top);
            // Over a hole the body fits, the walk ends there and the hero falls in; one no deeper than a step is walked down into.
            int? caught = Caught(ship, x, to, up);
            if (caught is int over && Landing(ship, over, up, Spec.StepUp, false) != null) caught = null;
            int nx = caught ?? to;
            if (nx == x) break;
            int moved = Math.Abs(nx - x), ny = up;
            bool stopped = moved < part || caught != null;
            if (!Grounded(ship, nx, up))
            {
                if (Landing(ship, nx, up, Spec.StepUp, false) is int ground) ny = ground;
                else return stopped ? (nx, up) : Aloft(ship, nx, up, dir * (left - moved));
            }
            if (!Clear(ship, nx, ny)) break;
            (x, y) = (nx, ny);
            left -= moved;
            if (stopped) break;
        }
        return (x, y);
    }

    /// <summary>
    /// Where the feet stand stepping up from (<paramref name="x"/>, <paramref name="y"/>) to <paramref name="nx"/>, onto what stops
    /// a walk there: the first row's top no more than <c>Hero.StepUp</c> above the feet where the box fits, raised over where it is
    /// and where it goes; null for none, a wall too high or a ceiling too low (0106).
    /// </summary>
    private static int? StepOnto(Ship ship, int x, int y, int nx)
    {
        for (int top = CellOf(y - 1) * Unit; top >= y - Spec.StepUp; top -= Unit)
            if (Clear(ship, x, top) && Clear(ship, nx, top)) return top;
        return null;
    }

    /// <summary>A move of <paramref name="dx"/> off the ground: the box stopped by what is solid; where the hero would not fit (<see cref="Clear"/>), no move.</summary>
    private static (int X, int Y) Aloft(Ship ship, int x, int y, int dx)
    {
        if (dx == 0) return (x, y);
        int nx = MoveX(ship, x, y, dx);
        return Clear(ship, nx, y) ? (nx, y) : (x, y);
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
        int nx = MoveX(ship, hero.X, hero.Y, dx);
        int ny = MoveY(ship, nx, hero.Y, dy, false);
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
        int nx = MoveX(ship, hero.X, hero.Y, vx);
        int ny = MoveY(ship, nx, hero.Y, vy, false);
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

    /// <summary>
    /// A step on a ladder at (<paramref name="x"/>, <paramref name="y"/>), <paramref name="dir"/> up or down at
    /// <paramref name="speed"/>. The step the feet reach its top the hero stands on it, no further than the step went; the
    /// figure has pulled itself up meanwhile (<see cref="LedgeAbove"/>). Held on at its top while down cannot yet take the
    /// body into a shaft, it stays on the ladder as it eases onto its middle.
    /// </summary>
    private static Hero Climb(Ship ship, int x, int y, int dir, int speed, int facing)
    {
        int ny = MoveY(ship, x, y, dir * speed, true);
        int column = CellOf(x);
        int before = CellOf(y - 1);
        int after = CellOf(ny - 1);
        // An open hatch a cell over the ladder's top is still climbed through: the body bridges the gap, so the feet go on up.
        bool onTop = y % Unit == 0 && !Climbable(ship, column, before) && Climbable(ship, column, CellOf(y))
            && !TouchesLadder(ship, x, y, false);
        // The feet rose past the ladder's top, or stood on it already: the hero steps off and stands on it.
        if (dir < 0 && after < before && Climbable(ship, column, before) && !Climbable(ship, column, after))
            return new Hero(x, before * Unit, 0, 0, false, facing);
        if (dir < 0 && onTop) return new Hero(x, y, 0, 0, false, facing);
        bool on = TouchesLadder(ship, x, ny, false, overHead: dir < 0) || dir > 0 && ny == y && onTop;
        return new Hero(x, ny, 0, ny - y, on, facing);
    }

    /// <summary>
    /// How far above <paramref name="hero"/>'s feet the top of the ladder it climbs is, in thousandths of a cell: where its
    /// feet stand on stepping off, the top of the highest ladder cell (or hatch it climbs through) within
    /// <paramref name="rows"/> rows over its feet, where it fits standing; none off a ladder or with no such top. What the
    /// figure pulls itself up onto, read alike for every hero from where it is (0092).
    /// </summary>
    public static int? LedgeAbove(Ship ship, Hero hero, int rows = 6)
    {
        if (!hero.Climbing) return null;
        int column = CellOf(hero.X), feet = CellOf(hero.Y - 1);
        int? ledge = null;
        for (int row = feet + 1; row >= feet - rows; row--)
        {
            if (!Climbable(ship, column, row) || Climbable(ship, column, row - 1)) continue;
            int top = row * Unit;
            if (top <= hero.Y && Clear(ship, LadderMiddle(ship, hero.X, hero.Y), top)) ledge = hero.Y - top;
        }
        return ledge;
    }

    private static bool Grounded(Ship ship, int x, int y) => MoveY(ship, x, y, 1, false) == y;

    /// <summary>
    /// Where a hero standing at <paramref name="y"/> and walking from <paramref name="x"/> to <paramref name="nx"/>
    /// stops: at the first place on the way where its body fits over a hole below the feet, or null for none, the walk
    /// going on to <paramref name="nx"/>. A step is longer than the 100 thousandths two cells leave beside the body, so
    /// unstopped a walk steps over a hole two cells wide or into it by where it began (playtest 1).
    /// </summary>
    private static int? Caught(Ship ship, int x, int nx, int y)
    {
        int row = CellOf(y);
        int dir = Math.Sign(nx - x);
        for (int c = CellOf(x); c != CellOf(nx) + dir; c += dir)
        {
            if (!Open(ship, c, row)) continue;
            // The hole is the run of open cells this one is in: the body, wider than a cell (0104), fits over two.
            var (a, b) = HoleOf(ship, c, row);
            int lo = a * Unit + Half, hi = (b + 1) * Unit - Half;
            if (lo > hi) continue;
            if (dir > 0 && x < lo && nx >= lo) return Math.Min(nx, hi);
            if (dir < 0 && x > hi && nx <= hi) return Math.Max(nx, lo);
        }
        return null;
    }

    /// <summary>
    /// <paramref name="x"/> moved a walk's step at most toward where the body fits over the hole under its
    /// middle, if there is one below the feet: down over a hole drops the hero in.
    /// </summary>
    private static int Slid(Ship ship, int x, int y)
    {
        int c = CellOf(x), row = CellOf(y);
        if (!Open(ship, c, row)) return x;
        var (a, b) = HoleOf(ship, c, row);
        if (a * Unit + Half > (b + 1) * Unit - Half) return x;
        int to = Math.Clamp(x, a * Unit + Half, (b + 1) * Unit - Half);
        return MoveX(ship, x, y, Math.Clamp(to - x, -Spec.Walk, Spec.Walk));
    }

    /// <summary>The run of cells open to fall into along <paramref name="row"/> round <paramref name="column"/>: no wider than the body needs.</summary>
    private static (int Left, int Right) HoleOf(Ship ship, int column, int row)
    {
        int a = column, b = column, most = 2 * Spec.Width / Unit + 1;
        while (b - a < most && Open(ship, a - 1, row)) a--;
        while (b - a < most && Open(ship, b + 1, row)) b++;
        return (a, b);
    }

    /// <summary>
    /// The middle of the ladder a hero at <paramref name="x"/>, its feet at <paramref name="y"/>, climbs: between its column and a
    /// column beside it that holds a ladder too, the nearer first, a ladder two cells wide as a 16 px one is (0104); else its column's middle.
    /// </summary>
    private static int LadderMiddle(Ship ship, int x, int y)
    {
        int column = CellOf(x), near = x - column * Unit < Unit / 2 ? column - 1 : column + 1;
        foreach (int beside in (ReadOnlySpan<int>)[near, 2 * column - near])
            if (TouchesLadderIn(ship, beside, y, true, overHead: true)) return Math.Max(column, beside) * Unit;
        return column * Unit + Unit / 2;
    }

    /// <summary>
    /// Whether what holds up a hero standing at (<paramref name="x"/>, <paramref name="y"/>) is floors only,
    /// which down drops through: no wall, closed door, closed hatch or ladder top under any part of the feet.
    /// </summary>
    private static bool OnFloors(Ship ship, int x, int y)
    {
        int row = CellOf(y);
        bool floor = false;
        for (int c = CellOf(x - Half); c <= CellOf(x + Half - 1); c++)
        {
            if (Solid(ship, c, row) || Climbable(ship, c, row)) return false;
            floor |= Has(ship, c, row, Trait.Supports);
        }
        return floor;
    }

    /// <summary>Whether a cell lets a hero fall into it from above: nothing in it blocks or holds.</summary>
    private static bool Open(Ship ship, int x, int y) => !Solid(ship, x, y) && !Holds(ship, x, y, false);

    /// <summary>Whether the hero's column holds a ladder beside the body, or also just below the feet, or within reach over the head.</summary>
    private static bool TouchesLadder(Ship ship, int x, int y, bool belowFeet, bool overHead = false) =>
        TouchesLadderIn(ship, CellOf(x), y, belowFeet, overHead);

    /// <summary>
    /// Whether <paramref name="column"/> holds a ladder beside a body with its feet at <paramref name="y"/>, or also just below them,
    /// or <paramref name="overHead"/> within its reach over the head (<c>Hero.HoldReach</c>): up takes hold of an open hatch in the
    /// deck above from under it and climbs on into it (0112); climbing down lets go where the body leaves the ladder.
    /// </summary>
    private static bool TouchesLadderIn(Ship ship, int column, int y, bool belowFeet, bool overHead = false)
    {
        int last = CellOf(belowFeet ? y : y - 1);
        for (int row = CellOf(y - Spec.Height - (overHead ? Spec.HoldReach : 0)); row <= last; row++)
            if (Climbable(ship, column, row)) return true;
        return false;
    }

    /// <summary>
    /// <paramref name="x"/> moved by <paramref name="dx"/>, stopped flush against the first column the leading edge enters with
    /// something solid in the box's rows. A walk steps up onto what stops it here (<see cref="Walk"/>).
    /// </summary>
    private static int MoveX(Ship ship, int x, int y, int dx)
    {
        int top = CellOf(y - Spec.Height), bottom = CellOf(y - 1);
        bool Blocks(int column)
        {
            for (int row = top; row <= bottom; row++)
                if (Solid(ship, column, row)) return true;
            return false;
        }
        if (dx > 0)
        {
            for (int c = CellOf(x + Half - 1) + 1; c <= CellOf(x + Half + dx - 1); c++)
                if (Blocks(c)) return Math.Max(x, c * Unit - Half);
        }
        else if (dx < 0)
        {
            for (int c = CellOf(x - Half) - 1; c >= CellOf(x - Half + dx); c--)
                if (Blocks(c)) return Math.Min(x, (c + 1) * Unit + Half);
        }
        return x + dx;
    }

    /// <summary>
    /// <paramref name="y"/> moved by <paramref name="dy"/>: going down, to where the feet land (<see cref="Landing"/>); rising,
    /// stopped flush under the first row the head enters with something solid over the box.
    /// </summary>
    private static int MoveY(Ship ship, int x, int y, int dy, bool climbing)
    {
        if (dy > 0) return Landing(ship, x, y, dy, climbing) ?? y + dy;
        if (dy < 0)
        {
            int left = CellOf(x - Half), right = CellOf(x + Half - 1), top = y - Spec.Height;
            for (int r = CellOf(top) - 1; r >= CellOf(top + dy); r--)
                for (int c = left; c <= right; c++)
                    if (Solid(ship, c, r)) return Math.Min(y, (r + 1) * Unit + Spec.Height);
        }
        return y + dy;
    }

    /// <summary>Where feet falling from <paramref name="y"/> by up to <paramref name="reach"/> first come to rest, or null: the highest ground under the box (<see cref="GroundIn"/>).</summary>
    private static int? Landing(Ship ship, int x, int y, int reach, bool climbing)
    {
        int? best = null;
        for (int c = CellOf(x - Half); c <= CellOf(x + Half - 1); c++)
            if (GroundIn(ship, c, y, reach, climbing) is int at) best = Math.Min(best ?? at, at);
        return best;
    }

    /// <summary>
    /// The highest ground in <paramref name="column"/> at or below <paramref name="y"/> and no further than <paramref name="reach"/>
    /// below it; null for none. Walls, closed doors, floors and ladder tops hold the box's bottom. A row the feet are in is never
    /// ground, which lets a hero up through a floor from below.
    /// </summary>
    private static int? GroundIn(Ship ship, int column, int y, int reach, bool climbing)
    {
        for (int r = CellOf(y - 1) + 1; r <= CellOf(y + reach); r++)
            if (Solid(ship, column, r) || Holds(ship, column, r, climbing)) return r * Unit;
        return null;
    }

    /// <summary>Whether the hero fits at (<paramref name="x"/>, <paramref name="y"/>): its box clear of every wall and closed door. A move that would end otherwise is not made.</summary>
    private static bool Clear(Ship ship, int x, int y)
    {
        for (int c = CellOf(x - Half); c <= CellOf(x + Half - 1); c++)
            for (int r = CellOf(y - Spec.Height); r <= CellOf(y - 1); r++)
                if (Solid(ship, c, r)) return false;
        return true;
    }

    /// <summary>The cell a length in thousandths falls in, rounding down below zero too.</summary>
    private static int CellOf(int v) => v >= 0 ? v / Unit : -((Unit - 1 - v) / Unit);

    private static int? StructureAt(Ship ship, int x, int y) =>
        x < 0 || y < 0 || x >= ship.Width || y >= ship.Height ? null : ship.StructureAt(y * ship.Width + x);

    /// <summary>
    /// What a body stops in (<see cref="Trait.Blocks"/>, <see cref="Trait.BlocksFromBelow"/>: walls, shut doors and hatches), and
    /// everything past the grid's edge: a shut hatch lies in the deck, holding a walk from above and stopping a body from below.
    /// </summary>
    private static bool Solid(Ship ship, int x, int y) =>
        StructureAt(ship, x, y) is not int s || Structures.Is(s, Trait.Blocks | Trait.BlocksFromBelow);

    /// <summary>Whether the cell at (<paramref name="x"/>, <paramref name="y"/>) has <paramref name="trait"/>; nothing past the grid's edge does.</summary>
    private static bool Has(Ship ship, int x, int y, Trait trait) => StructureAt(ship, x, y) is int s && Structures.Is(s, trait);

    /// <summary>What a hero climbs (<see cref="Trait.Climbable"/>): a ladder, and an open hatch, a ladder of its own (0112).</summary>
    private static bool Climbable(Ship ship, int x, int y) => Has(ship, x, y, Trait.Climbable);

    /// <summary>What holds a hero up from below only: what supports (floors, shut hatches), and a climbable's top cell unless climbing.</summary>
    private static bool Holds(Ship ship, int x, int y, bool climbing) =>
        Has(ship, x, y, Trait.Supports)
        || (!climbing && Climbable(ship, x, y) && !Climbable(ship, x, y - 1));
}
