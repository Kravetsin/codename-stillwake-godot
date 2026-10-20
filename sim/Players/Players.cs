using System.Text.Json.Serialization;

namespace Stillwake.Sim.Players;

/// <summary>Peer <c>Peer</c>'s hero wears suit colour <c>Colour</c>, of <c>Content.Crew.Colours</c>.</summary>
public sealed record Worn(int Peer, int Colour);

/// <summary>The players' slice. Public so modules can be typed by it, its members internal.</summary>
public sealed class PlayersState
{
    /// <summary>The colour each peer with a hero wears, on whichever surface, in peer order.</summary>
    [JsonInclude] internal List<Worn> Colours { get; set; } = [];
}

/// <summary>What any module or the client may read of the players.</summary>
public interface IPlayersQueries
{
    /// <summary>The suit colour <paramref name="peer"/>'s hero wears; null while it has none on any surface.</summary>
    int? ColourOf(int peer);
    /// <summary>Every colour worn, in peer order: live and read-only.</summary>
    IReadOnlyList<Worn> Worn { get; }
}

/// <summary>How crew dresses its heroes.</summary>
public interface IPlayersOps
{
    /// <summary>
    /// Dresses <paramref name="peer"/>'s hero coming in: the first colour nobody wears, or with every one worn, the
    /// one worn least, the first of those on a tie. One wearing a colour already keeps it.
    /// </summary>
    void Dress(int peer);
    /// <summary>Puts <paramref name="peer"/>'s hero into <paramref name="colour"/>; the caller checks nobody else wears it.</summary>
    void Wear(int peer, int colour);
    /// <summary><paramref name="peer"/>'s hero has gone: its colour is free.</summary>
    void Undress(int peer);
}

/// <summary>
/// What is the player's rather than a surface's (0076): the suit colour each hero wears, so two heroes on two
/// surfaces never meet in one colour. One per world, records only: no commands, no systems.
/// </summary>
public sealed class PlayersModule : Module<PlayersState>, IPlayersQueries, IPlayersOps
{
    public override string Id => "players";

    protected override PlayersState Init(Layout layout) => new();

    public int? ColourOf(int peer) => State.Colours.Find(w => w.Peer == peer)?.Colour;

    public IReadOnlyList<Worn> Worn => State.Colours;

    public void Dress(int peer)
    {
        if (ColourOf(peer) != null) return;
        var worn = new int[Content.Crew.Colours];
        foreach (var w in State.Colours) worn[w.Colour] += 1;
        Wear(peer, Array.IndexOf(worn, worn.Min()));
    }

    public void Wear(int peer, int colour)
    {
        Undress(peer);
        int at = State.Colours.FindIndex(w => w.Peer > peer);
        State.Colours.Insert(at < 0 ? State.Colours.Count : at, new Worn(peer, colour));
    }

    public void Undress(int peer) => State.Colours.RemoveAll(w => w.Peer == peer);
}
