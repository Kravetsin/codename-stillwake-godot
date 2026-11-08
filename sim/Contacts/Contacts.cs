using System.Text.Json.Serialization;

namespace Stillwake.Sim.Contacts;

/// <summary>
/// A place the radar shows: contact <c>Id</c> of kind <c>Kind</c> (<c>Content.Contacts.Kinds</c>), <c>Offset</c> radar units along
/// the ship's orbit (minus left of the ship), gone once <c>Left</c> more ticks have run.
/// </summary>
public sealed record Contact(int Id, string Kind, int Offset, int Left);

/// <summary>The contacts' slice. Public so modules can be typed by it, its members internal.</summary>
public sealed class ContactsState
{
    /// <summary>Ticks till the next contact; 0 before the first is counted down.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] internal int NextIn { get; set; }
    /// <summary>The id the next contact takes.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] internal int NextId { get; set; }
    /// <summary>The contacts up now, in id order.</summary>
    [JsonInclude] internal List<Contact> Contacts { get; set; } = [];
}

/// <summary>A contact of <paramref name="Kind"/> at <paramref name="Offset"/> at once, from the dev panel; the countdown is untouched.</summary>
public sealed record ContactNow(string Kind, int Offset) : Command;

/// <summary>
/// The radar's contacts (2026-11-08-radar-contacts): places that come up on the ship's orbit now and then and go after a while.
/// One per world, whichever surface its heroes are on: the radar watches space. Nothing acts on a contact yet.
/// </summary>
public sealed class ContactsModule : Module<ContactsState>
{
    public override string Id => "contacts";

    public ContactsModule()
    {
        Handles<ContactNow>(NowProblem, c => Add(c.Kind, c.Offset));
        System(Phase.Simulation, Tick);
    }

    protected override ContactsState Init(Layout layout) => new();

    /// <summary>The contacts up now, in id order: live and read-only.</summary>
    public IReadOnlyList<Contact> All => State.Contacts;

    /// <summary>Contact <paramref name="id"/>; null once it is gone, or never was.</summary>
    public Contact? Find(int id) => State.Contacts.Find(c => c.Id == id);

    /// <summary>Ticks till the next contact comes up; 0 before the world's first tick.</summary>
    public int NextIn => State.NextIn;

    private static string? NowProblem(ContactNow c) =>
        !Content.Contacts.Kinds.Contains(c.Kind) ? "contacts.unknownKind"
        : c.Offset < -Content.Contacts.Reach || c.Offset > Content.Contacts.Reach ? "contacts.outOfReach" : null;

    /// <summary>
    /// A tick: every contact's time runs down and one run out goes; then the countdown, the first started at
    /// <c>First</c>; at 0 a contact comes up and the next is drawn. Gone before added, so with <c>Lasts</c> no more than
    /// <c>EveryMin</c> two are never up at once.
    /// </summary>
    private void Tick()
    {
        var s = State;
        for (int i = s.Contacts.Count - 1; i >= 0; i--)
        {
            if (s.Contacts[i].Left > 1) s.Contacts[i] = s.Contacts[i] with { Left = s.Contacts[i].Left - 1 };
            else s.Contacts.RemoveAt(i);
        }
        if (s.NextIn == 0) s.NextIn = Content.Contacts.First;
        if (--s.NextIn > 0) return;
        var kinds = Content.Contacts.Kinds;
        string kind = kinds[(int)Random.Below((uint)kinds.Count)];
        int side = Random.Below(2) == 0 ? -1 : 1;
        int distance = Content.Contacts.Nearest + (int)Random.Below((uint)(Content.Contacts.Reach - Content.Contacts.Nearest + 1));
        Add(kind, side * distance);
        s.NextIn = Content.Contacts.EveryMin + (int)Random.Below((uint)(Content.Contacts.EveryMax - Content.Contacts.EveryMin + 1));
    }

    private void Add(string kind, int offset) => State.Contacts.Add(new Contact(State.NextId++, kind, offset, Content.Contacts.Lasts));

    /// <summary>Now and then a contact by hand: a kind there is or none, an offset in reach or a little past it.</summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        if (rng.Below(64) != 0) return null;
        string kind = rng.Below(4) == 0 ? "nothing" : Content.Contacts.Wreck;
        int offset = (int)rng.Below((uint)(2 * Content.Contacts.Reach + 21)) - Content.Contacts.Reach - 10;
        return new ContactNow(kind, offset) { From = from };
    }
}
