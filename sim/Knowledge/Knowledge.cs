using System.Text.Json.Serialization;

namespace Stillwake.Sim.Knowledge;

/// <summary>What has gone into tier <c>Tier</c>'s deposit of <c>Item</c> so far.</summary>
public sealed record Deposited(string Tier, string Item, int Count);

/// <summary>The knowledge's slice. Public so modules can be typed by it, its members internal.</summary>
public sealed class KnowledgeState
{
    /// <summary>The blueprints and crafts known, granted at the start or bought since, by id, sorted ordinally.</summary>
    [JsonInclude] internal List<string> Granted { get; set; } = [];
    /// <summary>The ship's credits.</summary>
    [JsonInclude] internal int Credits { get; set; }
    /// <summary>What the tiers' deposits hold so far, sorted ordinally by tier, then item.</summary>
    [JsonInclude] internal List<Deposited> Deposits { get; set; } = [];
}

/// <summary>What research and trade read of what the crew knows and has earned.</summary>
public interface IKnowledgeQueries
{
    /// <summary>Whether the blueprint or craft <paramref name="id"/> is known: granted at the start or bought.</summary>
    bool Granted(string id);
    /// <summary>The ship's credits.</summary>
    int Credits { get; }
    /// <summary>How much of <paramref name="item"/> tier <paramref name="tier"/>'s deposit holds.</summary>
    int DepositOf(string tier, string item);
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
    /// <summary>Adds <paramref name="count"/> of <paramref name="item"/> to tier <paramref name="tier"/>'s deposit.</summary>
    void AddDeposit(string tier, string item, int count);
}

/// <summary>
/// What the crew knows and has earned, wherever it did (0076): the blueprints and crafts known, the credits, the tiers' deposits.
/// One per world, records only: no commands, no systems. Its rules are research's and trade's, one a surface.
/// </summary>
public sealed class KnowledgeModule(IReadOnlyList<string> every) : Module<KnowledgeState>, IKnowledgeQueries, IKnowledgeOps
{
    public override string Id => "knowledge";

    /// <summary>A map's <c>Layout.Known</c> is what its world is granted; a map without one is granted <c>every</c> blueprint and craft.</summary>
    protected override KnowledgeState Init(Layout layout)
    {
        var granted = (layout.Known ?? every).Distinct().ToList();
        granted.Sort(StringComparer.Ordinal);
        return new KnowledgeState { Granted = granted };
    }

    public bool Granted(string id) => State.Granted.BinarySearch(id, StringComparer.Ordinal) >= 0;

    public int Credits => State.Credits;

    public int DepositOf(string tier, string item)
    {
        foreach (var d in State.Deposits)
            if (d.Tier == tier && d.Item == item) return d.Count;
        return 0;
    }

    public void Earn(int credits) => State.Credits += credits;

    public void Spend(int credits)
    {
        if (credits > State.Credits) throw new InvalidOperationException("knowledge.spend: not enough; check Credits first");
        State.Credits -= credits;
    }

    public void Grant(string id)
    {
        int at = State.Granted.BinarySearch(id, StringComparer.Ordinal);
        if (at < 0) State.Granted.Insert(~at, id);
    }

    public void AddDeposit(string tier, string item, int count)
    {
        var deposits = State.Deposits;
        int at = 0;
        for (; at < deposits.Count; at++)
        {
            int order = string.CompareOrdinal(deposits[at].Tier, tier);
            if (order == 0) order = string.CompareOrdinal(deposits[at].Item, item);
            if (order == 0)
            {
                deposits[at] = deposits[at] with { Count = deposits[at].Count + count };
                return;
            }
            if (order > 0) break;
        }
        deposits.Insert(at, new Deposited(tier, item, count));
    }
}
