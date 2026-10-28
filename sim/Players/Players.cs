using System.Text.Json.Serialization;

namespace Stillwake.Sim.Players;

/// <summary>Peer <c>Peer</c>'s hero wears suit colour <c>Colour</c>, of <c>Content.Crew.Colours</c>.</summary>
public sealed record Worn(int Peer, int Colour);

/// <summary>
/// What a hero's person looks like (2026-10-28-looks-are-printed): indices into the client's skins, hairs, hair colours,
/// faces and beards (beard 0 none). Not checked: the client draws each by a non-negative modulo of its palette.
/// </summary>
public sealed record Look(int Skin, int Hair, int HairColour, int Face, int Beard)
{
    /// <summary>Look 0, every index 0: what a peer that never chose wears.</summary>
    public static readonly Look Zero = new(0, 0, 0, 0, 0);
}

/// <summary>Peer <c>Peer</c>'s look: <c>Worn</c> by its hero, <c>Next</c> chosen for its next print.</summary>
public sealed record PeerLook(int Peer, Look Worn, Look Next);

/// <summary>The players' slice. Public so modules can be typed by it, its members internal.</summary>
public sealed class PlayersState
{
    /// <summary>The colour each peer with a hero wears, on whichever surface, in peer order.</summary>
    [JsonInclude] internal List<Worn> Colours { get; set; } = [];
    /// <summary>The look of each peer that chose one, with a hero or not, in peer order.</summary>
    [JsonInclude] internal List<PeerLook> Looks { get; set; } = [];
}

/// <summary>What any module or the client may read of the players.</summary>
public interface IPlayersQueries
{
    /// <summary>The suit colour <paramref name="peer"/>'s hero wears; null while it has none on any surface.</summary>
    int? ColourOf(int peer);
    /// <summary>Every colour worn, in peer order: live and read-only.</summary>
    IReadOnlyList<Worn> Worn { get; }
    /// <summary>The look <paramref name="peer"/>'s hero wears: look 0 for a peer that never chose, or one gone.</summary>
    Look LookOf(int peer);
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
    /// <summary><paramref name="peer"/> chooses <paramref name="look"/> for its next print, with a hero or not.</summary>
    void Choose(int peer, Look look);
    /// <summary>The pod prints <paramref name="peer"/>'s hero: it wears the look chosen last.</summary>
    void Print(int peer);
}

/// <summary>
/// What is the player's rather than a surface's (0076): the suit colour each hero wears, so two heroes on two
/// surfaces never meet in one colour, and the look each player chose. One per world, records only: no commands, no systems.
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

    public Look LookOf(int peer) => State.Looks.Find(l => l.Peer == peer)?.Worn ?? Look.Zero;

    public void Choose(int peer, Look look)
    {
        int at = State.Looks.FindIndex(l => l.Peer >= peer);
        if (at >= 0 && State.Looks[at].Peer == peer) State.Looks[at] = State.Looks[at] with { Next = look };
        else State.Looks.Insert(at < 0 ? State.Looks.Count : at, new PeerLook(peer, Look.Zero, look));
    }

    public void Print(int peer)
    {
        int at = State.Looks.FindIndex(l => l.Peer == peer);
        if (at >= 0) State.Looks[at] = State.Looks[at] with { Worn = State.Looks[at].Next };
    }
}
