using static Stillwake.Sim.Creatures.CreaturesModule;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// The bounty (tracker-catchers, catchers-space): on the beat, a catcher for each hunted hero with none here, out of every hero's view; gone
/// once it has left. Plain code until the raid needs a shared spawn rule (creatures-are-composed).
/// </summary>
public sealed partial class CreaturesModule
{
    /// <summary>Whether some hero here is hunted: then the creatures run on a surface with no life, for its catcher.</summary>
    private bool AnyHunted()
    {
        foreach (var hero in _crew.Heroes)
            if (_crew.Hunted(hero.Id)) return true;
        return false;
    }

    /// <summary>The species the bounty sends: the one hunter.</summary>
    private static readonly Spec.Species Bounties = Spec.All.Single(s => s.Lifetime == Spec.Lifetime.Hunter);

    /// <summary>
    /// Per hero in id order, hunted, awake, on the grid, with no catcher here, under the cap: two draws, the side and how many
    /// columns out (<c>Creatures.RingFrom</c> to <c>RingTo</c>), then the place nearest the hero's row in that column where a catcher
    /// fits unseen, where things float wholly in open space (catchers-space); none there, the other side's; none either side, it waits for a
    /// later beat.
    /// </summary>
    private void Bounty()
    {
        var spec = Bounties;
        foreach (var hero in _crew.Heroes)
        {
            if (!_crew.Hunted(hero.Id) || HeroAt(hero.Id, awake: true) is not { } at) continue;
            if (State.Creatures.Exists(c => !c.Dead && HuntDrive.QuarryOf(c) == hero.Id)) continue;
            if (State.Creatures.Count >= Spec.Max) return;
            bool right = Random.Below(2) == 0;
            int out_ = Spec.RingFrom + (int)Random.Below(Spec.RingTo - Spec.RingFrom + 1);
            if ((Lair(spec, at, right, out_) ?? Lair(spec, at, !right, out_)) is not var (x, y)) continue;
            var catcher = Add(spec, x, y);
            HuntDrive.Start(this, catcher, spec, hero.Id);
        }
    }

    /// <summary>
    /// Where a catcher comes for a hero at <paramref name="at"/>, <paramref name="out_"/> columns to its <paramref name="right"/> or
    /// left: the row nearest the hero's, above first at a tie, where its body is in nothing solid and in no hero's view, as its bottom
    /// middle; null for none, or off the grid.
    /// </summary>
    private (int X, int Y)? Lair(Spec.Species spec, (int X, int Y) at, bool right, int out_)
    {
        int width = spec.Body.Width, height = spec.Body.Height;
        int left = right ? at.X + out_ : at.X - out_ - width + 1;
        if (left < 0 || left + width > _grid.Width) return null;
        for (int d = 0; d < _grid.Height; d++)
            foreach (int row in (int[])[at.Y - d, at.Y + d])
            {
                if (row < height - 1 || row >= _grid.Height || d == 0 && row != at.Y) continue;
                var (x, y) = Standing(spec, left, row);
                if (!Overlaps(spec.Body, x, y) && !Seen(left, row - height + 1, spec) && (!Rules.Floats || InSpace(spec.Body, x, y))) return (x, y);
            }
        return null;
    }

    /// <summary>
    /// Whether a catcher goes now: once it has left the hunt, out of every hero's view, or at the top of the grid. With no hero on the
    /// surface it is seen by none.
    /// </summary>
    private bool Gone(Creature c, Spec.Species spec)
    {
        if (!HuntDrive.Leaving(c)) return false;
        int left = Down(c.X, Unit) - spec.Body.Width / 2, top = Down(c.Y - spec.Body.Height * Unit, Unit);
        return top <= 0 || !Seen(left, top, spec);
    }

    // Queries for the client.

    /// <summary>The peer whose hero <paramref name="c"/> hunts (a catcher, tracker-catchers); null for any other, or one leaving.</summary>
    public static int? Hunting(Creature c) => HuntDrive.Leaving(c) ? null : HuntDrive.QuarryOf(c);

    /// <summary>The catcher hunting <paramref name="peer"/>'s hero here, alive and not leaving; null for none: what its mark points at.</summary>
    public Creature? HunterOf(int peer) => State.Creatures.Find(c => !c.Dead && Hunting(c) == peer);

    /// <summary>
    /// What a hunter shows of its fight (catcher-combat): whether it aims at its quarry in sight, free of any move, and the cell its aim
    /// holds before a shot (-1 for none); the spot it gathers for (-1 for none) and the ticks left of the gather; the clock it last
    /// blinked at (0 for never).
    /// </summary>
    public readonly record struct Fight(bool Aims, int Held, int Ghost, int GatherLeft, int Blinked);

    /// <summary><paramref name="c"/>'s fight as it stands (<see cref="Fight"/>); nothing for one leaving or not a hunter.</summary>
    public Fight FightOf(Creature c)
    {
        if (HuntDrive.QuarryOf(c) == null || HuntDrive.Leaving(c)) return new(false, -1, -1, 0, 0);
        int ghost = c.Recall(HuntDrive.BlinkTo) - 1;
        return new(c.Act == null && c.Recall(HuntDrive.Aiming) == 1, c.Recall(HuntDrive.Held) - 1, ghost,
            ghost >= 0 ? Math.Max(0, c.Recall(HuntDrive.BlinkAt) - State.Clock) : 0, c.Recall(HuntDrive.BlinkedAt));
    }

    /// <summary>A shell's mark: the cell it lands on, the ticks left till it does, and its whole flight.</summary>
    public readonly record struct Mark(int Cell, int Left, int Flight);

    /// <summary>The marks of <paramref name="c"/>'s shells still flying, in throw order: what its marks are drawn by.</summary>
    public IEnumerable<Mark> MarksOf(Creature c)
    {
        if (c.Recall(HuntDrive.Thrown) <= 0) yield break;
        int thrown = c.Recall(HuntDrive.Thrown) - 1;
        var salvo = Spec.Get(c.Species).Brain.OfType<Spec.Hunt>().Single().Salvo;
        for (int i = 0; i < HuntDrive.Marks.Length; i++)
        {
            int cell = c.Recall(HuntDrive.Marks[i]) - 1, flight = salvo.Flight + i * salvo.Stagger, left = thrown + flight - State.Clock;
            if (cell >= 0 && left >= 0) yield return new(cell, left, flight);
        }
    }

    /// <summary>
    /// Whether <paramref name="c"/>, a flyer, follows a path's run step (up a step too, as a hero steps up) or stands on ground not
    /// rising (catcher-paths): drawn running, its jetpack off.
    /// </summary>
    public bool Afoot(Creature c)
    {
        var s = Spec.Get(c.Species).Body;
        if (s is not Spec.Flyer || HuntDrive.Leaving(c)) return false;
        if (c.Act == null && c.Path is [var step, ..] && step % 4 == (int)StepKind.Run) return true;
        if (c.VY < 0 || c.Y % Unit != 0) return false;
        int half = s.Width * Unit / 2, row = c.Y / Unit;
        for (int column = Down(c.X - half, Unit); column <= Down(c.X + half - 1, Unit); column++)
            if (Solid(column, row)) return true;
        return false;
    }
}
