namespace Stillwake.Content;

/// <summary>
/// What grows on a planet's surface (0078): the kinds of plant, and the stones lying about, that the ground's generator
/// (<c>Terrain</c>) sets on open soil under the sky. Each stands in one cell, its own, over the whole soil cell it grows
/// from; the client draws it as large as it is. Cosmetic for now: a hit fells one, giving its <see cref="Kind.Yields"/>.
/// </summary>
public static class Flora
{
    /// <summary>
    /// A kind of plant or stone. In a column with room for it, the generator draws a number under 1000 and sets the kind
    /// whose <c>Chance</c>, in this list's order, it falls in, when it has room: the ground level <c>Level</c> columns either
    /// side, open sky <c>Clear</c> rows above its cell across <c>Room</c> columns either side, none of its kind nearer than
    /// <c>Apart</c> columns.
    /// </summary>
    /// <param name="Tall">The cells it stands in, up from its own: a hit on any of them hits it.</param>
    public sealed record Kind(string Id, int Tall, int Level, int Room, int Clear, int Chance, int Apart)
    {
        /// <summary>What felling one gives the hero who did: nothing yet. Trees giving wood and seeds come here.</summary>
        public IReadOnlyList<(string Item, int Count)> Yields { get; init; } = [];
    }

    /// <summary>
    /// The first planet's: an acacia with hanging fronds and glowing seed pods, a bush of glowing berries, a rock, bell
    /// flowers glowing in three colours, a tuft of grass; most columns of open soil bare.
    /// </summary>
    public static readonly IReadOnlyList<Kind> All =
    [
        new("acacia", Tall: 9, Level: 1, Room: 3, Clear: 12, Chance: 80, Apart: 30),
        new("bush", Tall: 1, Level: 1, Room: 2, Clear: 2, Chance: 55, Apart: 10),
        new("rock", Tall: 1, Level: 1, Room: 1, Clear: 2, Chance: 22, Apart: 16),
        new("bellTeal", Tall: 1, Level: 0, Room: 0, Clear: 1, Chance: 40, Apart: 4),
        new("bellViolet", Tall: 1, Level: 0, Room: 0, Clear: 1, Chance: 30, Apart: 5),
        new("bellAmber", Tall: 1, Level: 0, Room: 0, Clear: 1, Chance: 25, Apart: 6),
        new("tuft", Tall: 1, Level: 0, Room: 0, Clear: 1, Chance: 150, Apart: 2),
    ];

    /// <summary>The kind <paramref name="id"/>; one not here is our bug.</summary>
    public static Kind Get(string id) =>
        All.FirstOrDefault(k => k.Id == id) ?? throw new KeyNotFoundException($"No plant '{id}'.");
}
