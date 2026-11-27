using System.Text.Json.Serialization;

namespace Stillwake.Sim.Players;

/// <summary>Peer <c>Peer</c>'s hero wears suit colour <c>Colour</c>, of <c>Content.Crew.Colours</c>.</summary>
public sealed record Worn(int Peer, int Colour);

/// <summary>
/// What a hero's person looks like (2026-10-28-looks-are-printed): indices into the client's skins, hairs, hair colours,
/// faces, beards (beard 0 none) and clothes. Not checked: the client draws each by a non-negative modulo of its palette.
/// </summary>
public sealed record Look(int Skin, int Hair, int HairColour, int Face, int Beard, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] int Outfit = 0)
{
    /// <summary>Look 0, every index 0: what a peer that never chose wears.</summary>
    public static readonly Look Zero = new(0, 0, 0, 0, 0);
}

/// <summary>Peer <c>Peer</c>'s look: <c>Worn</c> by its hero, <c>Next</c> chosen for its next print.</summary>
public sealed record PeerLook(int Peer, Look Worn, Look Next);

/// <summary>
/// Peer <c>Peer</c>'s skins (hero-skins): ids the client draws its hero's gear in, a set never checked; the client knows each
/// id's kind and draws an unknown one as the item's own.
/// </summary>
public sealed record PeerSkins(int Peer, IReadOnlyList<string> Ids);

/// <summary>Peer <c>Peer</c>'s drone's core (drone-abilities): its <c>Charge</c>, 0..<c>Content.Abilities.Full</c>.</summary>
public sealed record Core(int Peer, int Charge);

/// <summary>The players' slice. Public so modules can be typed by it, its members internal.</summary>
public sealed class PlayersState
{
    /// <summary>The colour each peer with a hero wears, on whichever surface, in peer order.</summary>
    [JsonInclude] internal List<Worn> Colours { get; set; } = [];
    /// <summary>The look of each peer that chose one, with a hero or not, in peer order.</summary>
    [JsonInclude] internal List<PeerLook> Looks { get; set; } = [];
    /// <summary>The peers paired with their light drone (drone-companion), in order: never undone, with a hero or not.</summary>
    [JsonInclude] internal List<int> Drones { get; set; } = [];
    /// <summary>The skins of each peer that chose any, with a hero or not, in peer order.</summary>
    [JsonIgnore] internal List<PeerSkins> Skins { get; set; } = [];
    /// <summary>The core of each paired peer that gathered any charge, in peer order.</summary>
    [JsonIgnore] internal List<Core> Cores { get; set; } = [];

    /// <summary><see cref="Cores"/> as a save writes it: left out while none, as a save from before them; loaded, held to a full core.</summary>
    [JsonInclude, JsonPropertyName("cores"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    internal List<Core>? CoresSaved
    {
        get => Cores.Count == 0 ? null : Cores;
        set => Cores = value?.Select(c => c with { Charge = Math.Clamp(c.Charge, 0, Content.Abilities.Full) }).ToList() ?? [];
    }

    /// <summary><see cref="Skins"/> as a save writes it: left out while nobody chose one, as a save from before skins.</summary>
    [JsonInclude, JsonPropertyName("skins"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    internal List<PeerSkins>? SkinsSaved
    {
        get => Skins.Count == 0 ? null : Skins;
        set => Skins = value ?? [];
    }
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
    /// <summary>The skins <paramref name="peer"/> chose last, worn at once: none for a peer that never chose, or chose none.</summary>
    IReadOnlyList<string> SkinsOf(int peer);
    /// <summary>Whether <paramref name="peer"/> has paired with its light drone (drone-companion): its bag rows open, its drone drawn.</summary>
    bool HasDrone(int peer);
    /// <summary>How much charge <paramref name="peer"/>'s drone's core holds (drone-abilities), 0..<c>Abilities.Full</c>; 0 unpaired.</summary>
    int ChargeOf(int peer);
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
    /// <summary><paramref name="peer"/> wears <paramref name="ids"/> from now on, in place of every skin before, with a hero or not.</summary>
    void Skin(int peer, IReadOnlyList<string> ids);
    /// <summary><paramref name="peer"/> pairs with its light drone for good; the caller checks it has none yet.</summary>
    void Pair(int peer);
    /// <summary><paramref name="peer"/>'s drone's core gains <paramref name="by"/>, or spends it below 0, held to 0..full; nothing unpaired.</summary>
    void Charge(int peer, long by);
}

/// <summary>
/// What is the player's rather than a surface's (0076): the suit colour each hero wears, so two heroes on two
/// surfaces never meet in one colour, the look each player chose and who has paired with its drone. One per world, records only:
/// no commands, no systems.
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

    public IReadOnlyList<string> SkinsOf(int peer) => State.Skins.Find(s => s.Peer == peer)?.Ids ?? [];

    public void Skin(int peer, IReadOnlyList<string> ids)
    {
        // A copy: the slice never holds the command's own list.
        var skins = new PeerSkins(peer, [.. ids]);
        int at = State.Skins.FindIndex(s => s.Peer >= peer);
        if (at >= 0 && State.Skins[at].Peer == peer) State.Skins[at] = skins;
        else State.Skins.Insert(at < 0 ? State.Skins.Count : at, skins);
    }

    public bool HasDrone(int peer) => State.Drones.BinarySearch(peer) >= 0;

    public void Pair(int peer)
    {
        int at = State.Drones.BinarySearch(peer);
        // Paired twice is crew's bug: PairDrone refuses a hero that has its drone.
        if (at >= 0) throw new InvalidOperationException($"peer {peer} is paired already");
        State.Drones.Insert(~at, peer);
    }

    public int ChargeOf(int peer) => State.Cores.Find(c => c.Peer == peer)?.Charge ?? 0;

    public void Charge(int peer, long by)
    {
        if (!HasDrone(peer) || by == 0) return;
        int at = State.Cores.FindIndex(c => c.Peer >= peer);
        bool has = at >= 0 && State.Cores[at].Peer == peer;
        int charge = (int)Math.Clamp((has ? State.Cores[at].Charge : 0) + by, 0, Content.Abilities.Full);
        if (has) State.Cores[at] = State.Cores[at] with { Charge = charge };
        else if (charge > 0) State.Cores.Insert(at < 0 ? State.Cores.Count : at, new Core(peer, charge));
    }
}
