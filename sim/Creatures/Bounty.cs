using static Stillwake.Sim.Creatures.CreaturesModule;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// The bounty (tracker-catchers): on the beat, a catcher for each hunted hero on a planet with none here, out of every hero's view; gone
/// once it has left. Plain code until the raid needs a shared spawn rule (creatures-are-composed).
/// </summary>
public sealed partial class CreaturesModule
{
    /// <summary>Whether some hero here is hunted: then the creatures run on a surface with no life, for its catcher.</summary>
    private bool AnyHunted()
    {
        if (Rules.Floats) return false;
        foreach (var hero in _crew.Heroes)
            if (_crew.Hunted(hero.Id)) return true;
        return false;
    }

    /// <summary>The species the bounty sends: the one hunter.</summary>
    private static readonly Spec.Species Bounties = Spec.All.Single(s => s.Lifetime == Spec.Lifetime.Hunter);

    /// <summary>
    /// Per hero in id order, hunted, awake, on the grid, with no catcher here, under the cap: two draws, the side and how many
    /// columns out (<c>Creatures.RingFrom</c> to <c>RingTo</c>), then the place nearest the hero's row in that column where a catcher
    /// fits unseen; none there, the other side's; none either side, it waits for a later beat. Never where nothing has gravity.
    /// </summary>
    private void Bounty()
    {
        if (Rules.Floats) return;
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
                if (!Overlaps(spec.Body, x, y) && !Seen(left, row - height + 1, spec)) return (x, y);
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

    /// <summary>Whether <paramref name="c"/> is a catcher still waiting where it came, its quarry warned.</summary>
    public bool Warning(Creature c) => Hunting(c) != null && State.Clock < c.Recall(HuntDrive.WarnTill);

    /// <summary>The creatures' clock of <paramref name="c"/>'s last strike on rock (a catcher cutting); 0 for none: what its sound plays by.</summary>
    public static int LastWear(Creature c) => c.Recall(FlyerBody.LastCut);

    /// <summary>Whether <paramref name="c"/>, a flyer, stands on ground and is not rising (catcher-paths): drawn running, its jetpack off.</summary>
    public bool Afoot(Creature c)
    {
        var s = Spec.Get(c.Species).Body;
        if (s is not Spec.Flyer || c.VY < 0 || c.Y % Unit != 0 || HuntDrive.Leaving(c)) return false;
        int half = s.Width * Unit / 2, row = c.Y / Unit;
        for (int column = Down(c.X - half, Unit); column <= Down(c.X + half - 1, Unit); column++)
            if (Solid(column, row)) return true;
        return false;
    }

    /// <summary>Whether <paramref name="c"/>, a flyer, struck what is in its way within its last <c>CutEvery</c> ticks: drawn drilling.</summary>
    public bool Cutting(Creature c) =>
        Spec.Get(c.Species).Body is Spec.Flyer { CutEvery: > 0 } f && LastWear(c) > 0 && State.Clock - LastWear(c) <= f.CutEvery;
}
