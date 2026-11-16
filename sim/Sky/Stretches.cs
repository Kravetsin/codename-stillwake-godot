using Stretch = Stillwake.Content.Stretches.Stretch;

namespace Stillwake.Sim.Sky;

/// <summary>
/// The stretches (docs/decisions/2026-11-15-stretches-of-space.md) of this surface's kind, the ship's or a planet's weather: their
/// schedule, a tick at a time in the construction phase.
/// </summary>
public sealed partial class SkyModule
{
    /// <summary>Salts of the sky's forks of the world's Rng: one, then one per surface and one per fork drawn; odd, so each spreads.</summary>
    private const uint SkySalt = 0x5c1e_a7b5, HereSalt = 0x85eb_ca6b, DrawnSalt = 0xc2b2_ae35;

    /// <summary>Whether the stretch chosen is in its warning: its own last <c>Stretch.Warning</c> ticks before it starts.</summary>
    private bool Coming => State.Next is { } next && State.NextIn <= StretchOf(next).Warning;

    /// <summary>
    /// A tick of the stretches, on a surface that has them: those newly unlocked opened; the one running counted down and ended, or
    /// the one chosen counted down and started; then, with none running or chosen, the next chosen, its wait drawn.
    /// </summary>
    private void Schedule()
    {
        var stretches = Rules.Stretches;
        if (stretches.Count == 0) return;
        var s = State;
        if (s.Opened.Count < stretches.Count)
            foreach (var stretch in stretches)
                if (!s.Opened.Contains(stretch.Id) && Unlocked(stretch)) Add(s.Opened, stretch.Id);
        if (s.Active != null)
        {
            s.Ran++;
            if (--s.Left <= 0) (s.Active, s.Left, s.Ran) = (null, 0, 0);
        }
        else if (s.Next != null && --s.NextIn <= 0) Start(StretchOf(s.Next));
        if (s.Active == null && s.Next == null && s.Opened.Count > 0) Choose();
    }

    /// <summary>Whether any item that opens <paramref name="stretch"/> has been obtained, however it came; one opened by none is open from the start.</summary>
    private bool Unlocked(Stretch stretch)
    {
        if (stretch.Unlock.Count == 0) return true;
        foreach (var item in stretch.Unlock)
            if (_knowledge.ObtainedOf(item) > 0) return true;
        return false;
    }

    /// <summary>
    /// The next stretch: the first opened that never came, else one drawn from those opened; then its wait, its <c>First</c> if it never
    /// came, else its <c>Gap</c>.
    /// </summary>
    private void Choose()
    {
        var s = State;
        string? next = null;
        foreach (var id in s.Opened)
            if (!s.Came.Contains(id))
            {
                next = id;
                break;
            }
        bool first = next != null;
        next ??= s.Opened[(int)Fork().Below((uint)s.Opened.Count)];
        var stretch = StretchOf(next);
        s.Next = next;
        s.NextIn = Draw(first ? stretch.First : stretch.Gap);
    }

    /// <summary><paramref name="stretch"/> starts: running for its duration drawn now, counted as come, nothing chosen.</summary>
    private void Start(Stretch stretch)
    {
        var s = State;
        s.Active = stretch.Id;
        (s.Left, s.Ran) = (Draw(stretch.Duration), 0);
        if (!s.Came.Contains(stretch.Id)) Add(s.Came, stretch.Id);
        (s.Next, s.NextIn) = (null, 0);
    }

    /// <summary>A dev's <see cref="StretchNow"/>: <paramref name="stretch"/> opened, and chosen next in place of any, its warning whole.</summary>
    private void Bring(Stretch stretch)
    {
        var s = State;
        if (!s.Opened.Contains(stretch.Id)) Add(s.Opened, stretch.Id);
        (s.Next, s.NextIn) = (stretch.Id, stretch.Warning);
    }

    private string? StretchNowProblem(StretchNow c)
    {
        if (Rules.StretchOf(c.Id) == null) return "sky.noStretch";
        return State.Active != null ? "sky.stretchRunning" : null;
    }

    /// <summary>A number of ticks in <paramref name="span"/>, both ends included, drawn by a fork.</summary>
    private int Draw(Content.Stretches.Span span) => span.Least + (int)Fork().Below((uint)(span.Most - span.Least + 1));

    /// <summary>
    /// A fork of the world's Rng for one draw, salted by this surface and the forks drawn before: the world's own stream never moves, so
    /// no draw after a stretch's does; two surfaces drawing on one tick draw apart.
    /// </summary>
    private Rng Fork()
    {
        unchecked
        {
            uint salt = SkySalt ^ (uint)Surfaces.Here * HereSalt ^ (uint)State.Drawn * DrawnSalt;
            State.Drawn++;
            return Random.Fork(salt);
        }
    }

    /// <summary>This surface's stretch of that id; one a save holds and the kind does not is our bug.</summary>
    private Stretch StretchOf(string id) =>
        Rules.StretchOf(id) ?? throw new InvalidOperationException($"sky: a stretch '{id}', which '{Rules.Id}' does not have");

    /// <summary>Puts <paramref name="id"/> into <paramref name="ids"/>, kept in the kind's order.</summary>
    private void Add(List<string> ids, string id)
    {
        int order = IndexOf(id), at = 0;
        while (at < ids.Count && IndexOf(ids[at]) < order) at++;
        ids.Insert(at, id);
    }

    private int IndexOf(string id)
    {
        var all = Rules.Stretches;
        for (int i = 0; i < all.Count; i++)
            if (all[i].Id == id) return i;
        return all.Count;
    }
}
