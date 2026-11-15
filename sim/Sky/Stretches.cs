using Stretch = Stillwake.Content.Stretches.Stretch;

namespace Stillwake.Sim.Sky;

/// <summary>The stretches of space (docs/plans/stretches.md): their schedule, a tick at a time in the construction phase.</summary>
public sealed partial class SkyModule
{
    /// <summary>Whether the stretch chosen is in its warning: its last <c>Stretches.Warning</c> ticks before it starts.</summary>
    private bool Coming => State.Next != null && State.NextIn <= Content.Stretches.Warning;

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
            if (--s.Left <= 0) (s.Active, s.Left) = (null, 0);
        }
        else if (s.Next != null && --s.NextIn <= 0) Start(StretchOf(s.Next));
        if (s.Active == null && s.Next == null && s.Opened.Count > 0) Choose();
    }

    /// <summary>Whether any item that opens <paramref name="stretch"/> has been obtained, however it came.</summary>
    private bool Unlocked(Stretch stretch)
    {
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
        next ??= s.Opened[(int)Random.Below((uint)s.Opened.Count)];
        var stretch = StretchOf(next);
        s.Next = next;
        s.NextIn = Draw(first ? stretch.First : stretch.Gap);
    }

    /// <summary><paramref name="stretch"/> starts: running for its duration drawn now, counted as come, nothing chosen.</summary>
    private void Start(Stretch stretch)
    {
        var s = State;
        s.Active = stretch.Id;
        s.Left = Draw(stretch.Duration);
        if (!s.Came.Contains(stretch.Id)) Add(s.Came, stretch.Id);
        (s.Next, s.NextIn) = (null, 0);
    }

    /// <summary>A dev's <see cref="StretchNow"/>: <paramref name="stretch"/> opened, and chosen next in place of any, its warning whole.</summary>
    private void Bring(Stretch stretch)
    {
        var s = State;
        if (!s.Opened.Contains(stretch.Id)) Add(s.Opened, stretch.Id);
        (s.Next, s.NextIn) = (stretch.Id, Content.Stretches.Warning);
    }

    private string? StretchNowProblem(StretchNow c)
    {
        if (!Rules.Stretches.Any(s => s.Id == c.Id)) return "sky.noStretch";
        return State.Active != null ? "sky.stretchRunning" : null;
    }

    /// <summary>A number of ticks in <paramref name="span"/>, both ends included, drawn by the world's Rng.</summary>
    private int Draw(Content.Stretches.Span span) => span.Least + (int)Random.Below((uint)(span.Most - span.Least + 1));

    /// <summary>The stretch of that id; one a save holds and the content does not is our bug.</summary>
    private static Stretch StretchOf(string id) =>
        Content.Stretches.Get(id) ?? throw new InvalidOperationException($"sky: a stretch '{id}', which is not in Stretches");

    /// <summary>Puts <paramref name="id"/> into <paramref name="ids"/>, kept in the content's order.</summary>
    private static void Add(List<string> ids, string id)
    {
        int order = IndexOf(id), at = 0;
        while (at < ids.Count && IndexOf(ids[at]) < order) at++;
        ids.Insert(at, id);
    }

    private static int IndexOf(string id)
    {
        var all = Content.Stretches.All;
        for (int i = 0; i < all.Count; i++)
            if (all[i].Id == id) return i;
        return all.Count;
    }
}
