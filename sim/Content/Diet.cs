namespace Stillwake.Content;

/// <summary>
/// The body's rates a hero's meals give (food-and-the-body), in thousandths, summed over its meals and capped: gear owns the
/// shell (capacities), food the body (rates). Calm: the share of a breath's cost saved. Deep: the share a canister gives more.
/// Work: the share of a tick a <c>Work</c> command is worth more (step A3).
/// </summary>
public readonly record struct Rates(int Calm = 0, int Deep = 0, int Work = 0);

/// <summary>A food: what is eaten, how long its meal lasts in ticks, and the rates it gives while it does.</summary>
public sealed record Food(string Id, int Lasts, Rates Rates);

/// <summary>The diet's numbers (food-and-the-body): the caps of food, a hungry hero's base, the meals a hero holds.</summary>
public static class Diet
{
    /// <summary>The most each rate food can reach, whatever is eaten: calm 400, deep 800 (a canister 36), work 500.</summary>
    public static readonly Rates Caps = new(Calm: 400, Deep: 800, Work: 500);

    /// <summary>Thousandths of a unit a breath costs: at rest, and running (<c>Crew.Exerted</c> units).</summary>
    public const int Rest = 1000, Sprint = Crew.Exerted * 1000;

    /// <summary>What a running breath costs a hungry hero, every meal gone: hunger never kills, it tires.</summary>
    public const int HungrySprint = 2500;

    /// <summary>Thousandths of a tick a <c>Work</c> command is worth: fed, before food's work rate; hungry, every fourth moves nothing.</summary>
    public const int Work = 1000, HungryWork = 750;

    /// <summary>How many meals a hero holds at once.</summary>
    public const int Meals = 3;

    /// <summary>Ticks left under which a meal may be eaten again, its ticks back to full: its last minute.</summary>
    public const int RefreshWithin = 1200;
}

/// <summary>Everything eaten (food-and-the-body): the raw foods, each an item but pod paste, which the pod prints into a hero.</summary>
public static class Foods
{
    /// <summary>What the pod prints into every hero it makes: no rates, it only keeps hunger off for 20 minutes.</summary>
    public const string Paste = "podPaste";
    public const string Seed = "seed", Ration = "ration", CannedMeat = "cannedMeat", Fruit = "fruit", Glowberry = "glowberry";

    /// <summary>Every food, a first guess for the playtest (docs/plans/food.md).</summary>
    public static readonly IReadOnlyList<Food> All =
    [
        new(Paste, 24_000, new()),
        // A snack: eat it or plant it, the player's choice. No rates; it only keeps hunger off for five minutes.
        new(Seed, 6_000, new()),
        // Supply pods and a derelict's fridge: a spacer's ration, for work and a steadier breath.
        new(Ration, 18_000, new(Calm: 50, Work: 300)),
        // A fridge or a vending machine: a deeper breath, a canister 26.
        new(CannedMeat, 18_000, new(Deep: 300)),
        // The farm's crops (fruit-not-biomass), eaten raw: each may give back its seed or spore, as crushing it does.
        new(Fruit, 12_000, new(Work: 100)),
        new(Glowberry, 12_000, new(Calm: 120)),
    ];

    private static readonly Dictionary<string, Food> ById = All.ToDictionary(f => f.Id, StringComparer.Ordinal);

    /// <summary>The food <paramref name="id"/> is; null for anything not eaten.</summary>
    public static Food? Of(string id) => ById.GetValueOrDefault(id);
}
