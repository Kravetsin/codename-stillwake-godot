using System.Text.Json.Serialization;

namespace Stillwake.Sim.Knowledge;

/// <summary>The knowledge's slice. Public so modules can be typed by it, its members internal.</summary>
public sealed class KnowledgeState
{
    /// <summary>The blueprints and crafts granted from the start, by id, sorted ordinally.</summary>
    [JsonInclude] internal List<string> Granted { get; set; } = [];
    /// <summary>The items analysed, by id, each once, sorted ordinally.</summary>
    [JsonInclude] internal List<string> Analysed { get; set; } = [];
}

/// <summary>What research reads of what the crew knows. Lists are live: a reader never writes them.</summary>
public interface IKnowledgeQueries
{
    /// <summary>Whether the blueprint or craft <paramref name="id"/> was granted from the start.</summary>
    bool Granted(string id);
    /// <summary>Whether <paramref name="item"/> has been analysed, on any surface.</summary>
    bool IsAnalysed(string item);
    /// <summary>The items analysed, by id, sorted ordinally.</summary>
    IReadOnlyList<string> Analysed { get; }
}

/// <summary>How research adds to what the crew knows.</summary>
public interface IKnowledgeOps
{
    /// <summary><paramref name="item"/> is analysed: kept once, in order; again, nothing changes.</summary>
    void Learn(string item);
}

/// <summary>
/// What the crew knows, wherever it learned it (0076): the blueprints and crafts the world was granted and the
/// items analysed, which research reads to open what is known. One per world, records only: no commands, no
/// systems. What each analyzer is working on is its surface's research's.
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

    public bool IsAnalysed(string item) => State.Analysed.BinarySearch(item, StringComparer.Ordinal) >= 0;

    public IReadOnlyList<string> Analysed => State.Analysed;

    public void Learn(string item)
    {
        int at = State.Analysed.BinarySearch(item, StringComparer.Ordinal);
        if (at < 0) State.Analysed.Insert(~at, item);
    }
}
