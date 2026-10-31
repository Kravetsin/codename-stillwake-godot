using System.Text.Json.Serialization;

namespace Stillwake.Sim.Knowledge;

/// <summary>What has gone into gate <c>Gate</c> of <c>Item</c> so far.</summary>
public sealed record Deposited(string Gate, string Item, int Count);

/// <summary>How much of <c>Item</c> there has been so far, for good: obtained by the crew, or sold.</summary>
public sealed record Tally(string Item, int Count);

/// <summary>The knowledge's slice. Public so modules can be typed by it, its members internal.</summary>
public sealed class KnowledgeState
{
    /// <summary>The blueprints and crafts known, granted at the start or bought since, by id, sorted ordinally.</summary>
    [JsonInclude] internal List<string> Granted { get; set; } = [];
    /// <summary>The ship's credits.</summary>
    [JsonInclude] internal int Credits { get; set; }
    /// <summary>What the gates hold so far, sorted ordinally by gate, then item.</summary>
    [JsonInclude] internal List<Deposited> Deposits { get; set; } = [];
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
}

/// <summary>What research and trade read of what the crew knows and has earned.</summary>
public interface IKnowledgeQueries
{
    /// <summary>Whether the blueprint or craft <paramref name="id"/> is known: granted at the start or bought.</summary>
    bool Granted(string id);
    /// <summary>The ship's credits.</summary>
    int Credits { get; }
    /// <summary>How much of <paramref name="item"/> gate <paramref name="gate"/> holds.</summary>
    int DepositOf(string gate, string item);
    /// <summary>The blueprints and crafts known, sorted ordinally.</summary>
    IReadOnlyList<string> GrantedIds { get; }
    /// <summary>What the crew has obtained, each item once, in the order first obtained.</summary>
    IReadOnlyList<Tally> Obtained { get; }
    /// <summary>How much of <paramref name="item"/> the crew has obtained, however it came.</summary>
    int ObtainedOf(string item);
    /// <summary>How much of <paramref name="item"/> has been sold.</summary>
    int SoldOf(string item);
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
    /// <summary>The blueprint or craft <paramref name="id"/> is known: kept once, in order.</summary>
    void Grant(string id);
    /// <summary>Adds <paramref name="count"/> of <paramref name="item"/> to gate <paramref name="gate"/>.</summary>
    void AddDeposit(string gate, string item, int count);
    /// <summary>The crew has obtained <paramref name="count"/> of <paramref name="item"/>; none or less counts for nothing.</summary>
    void Obtain(string item, int count);
    /// <summary><paramref name="count"/> of <paramref name="item"/> has been sold; none or less counts for nothing.</summary>
    void Sell(string item, int count);
}

/// <summary>
/// What the crew knows and has earned, wherever it did (0076): the blueprints and crafts known, the credits, the gates' deposits, what it has obtained and sold.
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
    }

    public bool Granted(string id) => State.Granted.BinarySearch(id, StringComparer.Ordinal) >= 0;

    public int Credits => State.Credits;

    public int DepositOf(string gate, string item)
    {
        foreach (var d in State.Deposits)
            if (d.Gate == gate && d.Item == item) return d.Count;
        return 0;
    }

    public IReadOnlyList<string> GrantedIds => State.Granted;

    public IReadOnlyList<Tally> Obtained => State.Obtained;

    public int ObtainedOf(string item) => _obtainedAt.TryGetValue(item, out int at) ? State.Obtained[at].Count : 0;

    public int SoldOf(string item)
    {
        foreach (var s in State.Sold)
            if (s.Item == item) return s.Count;
        return 0;
    }

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
        Version++;
    }

    public void Sell(string item, int count)
    {
        if (count <= 0) return;
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

    public void AddDeposit(string gate, string item, int count)
    {
        var deposits = State.Deposits;
        int at = 0;
        for (; at < deposits.Count; at++)
        {
            int order = string.CompareOrdinal(deposits[at].Gate, gate);
            if (order == 0) order = string.CompareOrdinal(deposits[at].Item, item);
            if (order == 0)
            {
                deposits[at] = deposits[at] with { Count = deposits[at].Count + count };
                return;
            }
            if (order > 0) break;
        }
        deposits.Insert(at, new Deposited(gate, item, count));
    }
}
