using System.Text.Json.Serialization;

namespace Stillwake.Sim.Knowledge;


/// <summary>How much of <c>Item</c> there has been so far, for good: obtained by the crew, or sold.</summary>
public sealed record Tally(string Item, int Count);

/// <summary>
/// The mafia's reputation (2026-11-08-mafia-reputation): the market's level reached, and what had been sold as it opened, the start the next
/// level is counted from: the sold count of each good it wants, sorted ordinally, and the worth of everything sold.
/// </summary>
public sealed record Reputation(int Level, IReadOnlyList<Tally> Base, long BaseWorth)
{
    /// <summary>A new world's: the market's first row open, nothing sold.</summary>
    public static readonly Reputation None = new(0, [], 0);
}

/// <summary>A purchase from the mafia on its way (2026-11-08-mafia-deliveries): <c>Good</c> the market node it is for, <c>Id</c> its own, never reused.</summary>
public sealed record Delivery(int Id, string Good);

/// <summary>How many times one player has done <c>Deed</c> (drone-lessons): what a deed lesson is counted by.</summary>
public sealed record DeedCount(string Deed, int Count);

/// <summary>
/// What one player has learned for themself (own-knowledge): the items held, in the order first held; what they understood from them, sorted
/// ordinally; the deeds they have done, sorted ordinally by deed (drone-lessons).
/// </summary>
public sealed class Learner
{
    [JsonInclude] internal int Peer { get; set; }
    [JsonInclude] internal List<string> Found { get; set; } = [];
    [JsonInclude] internal List<string> Granted { get; set; } = [];
    [JsonIgnore] internal List<DeedCount> Deeds { get; set; } = [];
    /// <summary>Deeds as saved: none written while none is done.</summary>
    [JsonInclude, JsonPropertyName("deeds"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    internal List<DeedCount>? SavedDeeds { get => Deeds.Count > 0 ? Deeds : null; set => Deeds = value ?? []; }
    /// <summary><c>Found</c> as a set, for the lookup a slot: derived, never saved.</summary>
    [JsonIgnore] internal HashSet<string> Held { get; } = new(StringComparer.Ordinal);
}

/// <summary>The knowledge's slice. Public so modules can be typed by it, its members internal.</summary>
public sealed class KnowledgeState
{
    /// <summary>The blueprints, crafts and upgrades every player knows: the map's start and what the crew bought, sorted ordinally (own-knowledge).</summary>
    [JsonInclude] internal List<string> Granted { get; set; } = [];
    /// <summary>What each player has learned for themself, by peer.</summary>
    [JsonIgnore] internal List<Learner> Learners { get; set; } = [];
    /// <summary>Learners as saved: none written while none has learned.</summary>
    [JsonInclude, JsonPropertyName("learners"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    internal List<Learner>? SavedLearners { get => Learners.Count > 0 ? Learners : null; set => Learners = value ?? []; }
    /// <summary>The ship's credits.</summary>
    [JsonInclude] internal int Credits { get; set; }
    /// <summary>What the crew has obtained, each item once, in the order first obtained.</summary>
    [JsonIgnore] internal List<Tally> Obtained { get; set; } = [];
    /// <summary>Obtained as saved: none written while none is had.</summary>
    [JsonInclude, JsonPropertyName("obtained"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    internal List<Tally>? SavedObtained { get => Obtained.Count > 0 ? Obtained : null; set => Obtained = value ?? []; }
    /// <summary>What has been sold, sorted ordinally by item.</summary>
    [JsonIgnore] internal List<Tally> Sold { get; set; } = [];
    /// <summary>Sold as saved: none written while none is sold.</summary>
    [JsonInclude, JsonPropertyName("sold"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    internal List<Tally>? SavedSold { get => Sold.Count > 0 ? Sold : null; set => Sold = value ?? []; }
    /// <summary>What everything sold was worth, in credits: what the capsules earned for it.</summary>
    [JsonInclude] internal long SoldWorth { get; set; }
    /// <summary>The mafia's reputation: its level and where the next is counted from.</summary>
    [JsonInclude] internal Reputation Reputation { get; set; } = Reputation.None;
    /// <summary>The purchases paid for and not yet caught, broken or lost, in the order bought.</summary>
    [JsonIgnore] internal List<Delivery> Deliveries { get; set; } = [];
    /// <summary>Deliveries as saved: none written while none is on its way.</summary>
    [JsonInclude, JsonPropertyName("deliveries"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    internal List<Delivery>? SavedDeliveries { get => Deliveries.Count > 0 ? Deliveries : null; set => Deliveries = value ?? []; }
    /// <summary>The id the next delivery gets: they only grow.</summary>
    [JsonInclude] internal int NextDelivery { get; set; } = 1;
    /// <summary>Deliveries lost past the ship since this world was loaded: for the marker that turns red, never saved.</summary>
    [JsonIgnore] internal int Lost { get; set; }
}

/// <summary>What research and trade read of what the crew knows and has earned.</summary>
public interface IKnowledgeQueries
{
    /// <summary>Whether every player knows the blueprint, craft or upgrade <paramref name="id"/>: the map's start, or bought by the crew.</summary>
    bool Granted(string id);
    /// <summary>Whether <paramref name="peer"/> knows <paramref name="id"/>: everyone's, or understood from what they held.</summary>
    bool Knows(int peer, string id);
    /// <summary>Whether any player knows <paramref name="id"/>: what the ship's machines and the world's events go by.</summary>
    bool KnowsAny(string id);
    /// <summary>What <paramref name="peer"/> knows, the start's and their own, sorted ordinally.</summary>
    IReadOnlyList<string> KnownIds(int peer);
    /// <summary>The peers who have learned anything for themselves, in peer order.</summary>
    IReadOnlyList<int> Learners { get; }
    /// <summary>The items <paramref name="peer"/> has held, each once, in the order first held.</summary>
    IReadOnlyList<string> Found(int peer);
    /// <summary>Whether <paramref name="peer"/> has ever held <paramref name="item"/>.</summary>
    bool HasFound(int peer, string item);
    /// <summary>The ship's credits.</summary>
    int Credits { get; }
    /// <summary>The blueprints and crafts known, sorted ordinally.</summary>
    IReadOnlyList<string> GrantedIds { get; }
    /// <summary>What the crew has obtained, each item once, in the order first obtained.</summary>
    IReadOnlyList<Tally> Obtained { get; }
    /// <summary>How much of <paramref name="item"/> the crew has obtained, however it came.</summary>
    int ObtainedOf(string item);
    /// <summary>How much of <paramref name="item"/> has been sold.</summary>
    int SoldOf(string item);
    /// <summary>What everything sold was worth, in credits.</summary>
    long SoldWorth { get; }
    /// <summary>The mafia's level of reputation: the market's rows open are those up to it.</summary>
    int Level { get; }
    /// <summary>How much of <paramref name="item"/> had been sold as the level opened: 0 for a good the next level does not want.</summary>
    int BaseOf(string item);
    /// <summary>What everything sold was worth as the level opened.</summary>
    long BaseWorth { get; }
    /// <summary>The purchases on their way, in the order bought.</summary>
    IReadOnlyList<Delivery> Deliveries { get; }
    /// <summary>Whether a purchase of <paramref name="good"/> is on its way.</summary>
    bool InFlight(string good);
    /// <summary>How many deliveries drifted past the ship since this world was loaded: never saved, for the marker that turns red.</summary>
    int Lost { get; }
    /// <summary>Moves whenever something is obtained or granted: never saved, for a reader to notice a change by.</summary>
    int Version { get; }
}

/// <summary>How research and trade change what the crew knows and has earned.</summary>
public interface IKnowledgeOps
{
    /// <summary>Adds <paramref name="credits"/> to the ship's.</summary>
    void Earn(int credits);
    /// <summary>Takes <paramref name="credits"/> from the ship's; the caller checks they are there.</summary>
    void Spend(int credits);
    /// <summary>Every player knows <paramref name="id"/>: kept once, in order. The start's, a purchase's, a test's or a dev run's.</summary>
    void Grant(string id);
    /// <summary><paramref name="peer"/> has understood <paramref name="id"/>: kept once, in order.</summary>
    void Grant(int peer, string id);
    /// <summary><paramref name="peer"/> holds <paramref name="item"/>: found once, in the order first held.</summary>
    void Find(int peer, string item);
    /// <summary>
    /// <paramref name="peer"/> has done <paramref name="deed"/> once more (drone-lessons): counted, and every deed lesson whose count it reaches
    /// granted to that peer.
    /// </summary>
    void Did(int peer, string deed);
    /// <summary>The crew has obtained <paramref name="count"/> of <paramref name="item"/>; none or less counts for nothing.</summary>
    void Obtain(string item, int count);
    /// <summary><paramref name="count"/> of <paramref name="item"/> has been sold for <paramref name="worth"/> credits in all; none or less counts for nothing.</summary>
    void Sell(string item, int count, long worth);
    /// <summary>
    /// The mafia opens <paramref name="level"/>, counting the next from <paramref name="start"/> and <paramref name="startWorth"/>. Kept only for the
    /// level after the current one, so each surface's research may call it on a tick and the world rises once.
    /// </summary>
    void Open(int level, IReadOnlyList<Tally> start, long startWorth);
    /// <summary>A purchase of <paramref name="good"/> is on its way: queued under a new id.</summary>
    void Deliver(string good);
    /// <summary>Delivery <paramref name="id"/> is caught or broken, or <paramref name="lost"/> past the ship; an id not on its way is let be.</summary>
    void Delivered(int id, bool lost = false);
}

/// <summary>
/// What the crew knows and has earned, wherever it did (0076): the blueprints and crafts known, the credits, what it has obtained and sold.
/// One per world, records only: no commands, no systems. Its rules are research's and trade's, one a surface.
/// </summary>
public sealed class KnowledgeModule(IReadOnlyList<string> every) : Module<KnowledgeState>, IKnowledgeQueries, IKnowledgeOps
{
    /// <summary>Where each item stands in <c>Obtained</c>: looked up, never iterated.</summary>
    private readonly Dictionary<string, int> _obtainedAt = new(StringComparer.Ordinal);

    public override string Id => "knowledge";

    /// <summary>A map's <c>Layout.Known</c> is what its world is granted; a map without one is granted <c>every</c> blueprint and craft.</summary>
    protected override KnowledgeState Init(Layout layout)
    {
        var granted = (layout.Known ?? every).Distinct().ToList();
        granted.Sort(StringComparer.Ordinal);
        return new KnowledgeState { Granted = granted };
    }

    public override void Rebuild()
    {
        _obtainedAt.Clear();
        for (int i = 0; i < State.Obtained.Count; i++) _obtainedAt[State.Obtained[i].Item] = i;
        _learners.Clear();
        foreach (var learner in State.Learners)
        {
            _learners.Add(learner.Peer);
            learner.Held.Clear();
            learner.Held.UnionWith(learner.Found);
        }
    }

    /// <summary>The peers in <c>Learners</c>, in peer order: derived.</summary>
    private readonly List<int> _learners = [];

    public bool Granted(string id) => In(State.Granted, id);

    private static bool In(List<string> sorted, string id) => sorted.BinarySearch(id, StringComparer.Ordinal) >= 0;

    private Learner? LearnerOf(int peer)
    {
        foreach (var learner in State.Learners)
            if (learner.Peer == peer) return learner;
        return null;
    }

    /// <summary><paramref name="peer"/>'s record, made empty in peer order on first need.</summary>
    private Learner Learner(int peer)
    {
        if (LearnerOf(peer) is { } had) return had;
        var made = new Learner { Peer = peer };
        int at = State.Learners.FindIndex(l => l.Peer > peer);
        if (at < 0) at = State.Learners.Count;
        State.Learners.Insert(at, made);
        _learners.Insert(at, peer);
        return made;
    }

    public bool Knows(int peer, string id) => Granted(id) || LearnerOf(peer) is { } l && In(l.Granted, id);

    public bool KnowsAny(string id)
    {
        if (Granted(id)) return true;
        foreach (var learner in State.Learners)
            if (In(learner.Granted, id)) return true;
        return false;
    }

    public IReadOnlyList<string> KnownIds(int peer)
    {
        if (LearnerOf(peer) is not { Granted.Count: > 0 } l) return State.Granted;
        var all = State.Granted.Concat(l.Granted).Distinct(StringComparer.Ordinal).ToList();
        all.Sort(StringComparer.Ordinal);
        return all;
    }

    public IReadOnlyList<int> Learners => _learners;

    public IReadOnlyList<string> Found(int peer) => LearnerOf(peer)?.Found ?? [];

    public bool HasFound(int peer, string item) => LearnerOf(peer) is { } l && l.Held.Contains(item);

    public int Credits => State.Credits;

    public IReadOnlyList<string> GrantedIds => State.Granted;

    public IReadOnlyList<Tally> Obtained => State.Obtained;

    public int ObtainedOf(string item) => _obtainedAt.TryGetValue(item, out int at) ? State.Obtained[at].Count : 0;

    public int SoldOf(string item)
    {
        foreach (var s in State.Sold)
            if (s.Item == item) return s.Count;
        return 0;
    }

    public long SoldWorth => State.SoldWorth;

    public int Level => State.Reputation.Level;

    public int BaseOf(string item)
    {
        foreach (var s in State.Reputation.Base)
            if (s.Item == item) return s.Count;
        return 0;
    }

    public long BaseWorth => State.Reputation.BaseWorth;

    public IReadOnlyList<Delivery> Deliveries => State.Deliveries;

    public bool InFlight(string good)
    {
        foreach (var d in State.Deliveries)
            if (d.Good == good) return true;
        return false;
    }

    public int Lost => State.Lost;

    public int Version { get; private set; }

    public void Earn(int credits) => State.Credits += credits;

    public void Spend(int credits)
    {
        if (credits > State.Credits) throw new InvalidOperationException("knowledge.spend: not enough; check Credits first");
        State.Credits -= credits;
    }

    public void Grant(string id)
    {
        int at = State.Granted.BinarySearch(id, StringComparer.Ordinal);
        if (at >= 0) return;
        State.Granted.Insert(~at, id);
        Version++;
    }

    public void Grant(int peer, string id)
    {
        if (Knows(peer, id)) return;
        var granted = Learner(peer).Granted;
        granted.Insert(~granted.BinarySearch(id, StringComparer.Ordinal), id);
        Version++;
    }

    public void Find(int peer, string item)
    {
        if (LearnerOf(peer) is { } had && had.Held.Contains(item)) return;
        var learner = Learner(peer);
        learner.Found.Add(item);
        learner.Held.Add(item);
        Version++;
    }

    public void Did(int peer, string deed)
    {
        var deeds = Learner(peer).Deeds;
        int at = 0;
        while (at < deeds.Count && string.CompareOrdinal(deeds[at].Deed, deed) < 0) at++;
        int count = at < deeds.Count && deeds[at].Deed == deed ? deeds[at].Count + 1 : 1;
        if (count == 1) deeds.Insert(at, new DeedCount(deed, 1));
        else deeds[at] = deeds[at] with { Count = count };
        foreach (var lesson in Content.Lessons.All)
            if (lesson.Source is Content.Deed d && d.Id == deed && count >= d.Count) Grant(peer, lesson.Form);
    }

    /// <summary>How many times <paramref name="peer"/> has done <paramref name="deed"/>: for tests, never shown (drone-lessons).</summary>
    internal int DeedsOf(int peer, string deed)
    {
        if (LearnerOf(peer) is not { } learner) return 0;
        foreach (var d in learner.Deeds)
            if (d.Deed == deed) return d.Count;
        return 0;
    }

    public void Obtain(string item, int count)
    {
        if (count <= 0) return;
        if (_obtainedAt.TryGetValue(item, out int at))
            State.Obtained[at] = State.Obtained[at] with { Count = State.Obtained[at].Count + count };
        else
        {
            _obtainedAt[item] = State.Obtained.Count;
            State.Obtained.Add(new Tally(item, count));
        }
    }

    public void Sell(string item, int count, long worth)
    {
        if (count <= 0) return;
        State.SoldWorth += worth;
        var sold = State.Sold;
        int at = 0;
        for (; at < sold.Count; at++)
        {
            int order = string.CompareOrdinal(sold[at].Item, item);
            if (order == 0)
            {
                sold[at] = sold[at] with { Count = sold[at].Count + count };
                return;
            }
            if (order > 0) break;
        }
        sold.Insert(at, new Tally(item, count));
    }

    public void Deliver(string good)
    {
        State.Deliveries.Add(new Delivery(State.NextDelivery++, good));
        Version++;
    }

    public void Delivered(int id, bool lost = false)
    {
        int at = State.Deliveries.FindIndex(d => d.Id == id);
        if (at < 0) return;
        State.Deliveries.RemoveAt(at);
        if (lost) State.Lost++;
        Version++;
    }

    public void Open(int level, IReadOnlyList<Tally> start, long startWorth)
    {
        if (level != State.Reputation.Level + 1) return;
        var sorted = start.ToList();
        sorted.Sort((a, b) => string.CompareOrdinal(a.Item, b.Item));
        State.Reputation = new Reputation(level, sorted, startWorth);
        Version++;
    }
}
