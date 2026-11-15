namespace Stillwake.Content;

/// <summary>
/// The body's rates a hero's meals give (food-and-the-body), in thousandths, summed over its meals and capped: gear owns the
/// shell (capacities), food the body (rates). Calm: the share of a breath's cost saved. Work: the share of a tick a <c>Work</c>
/// command is worth more (step A3). Deep and Lungs are whole units, not shares (food-tuning): Deep what a canister gives more,
/// Lungs what the hero's air holds more.
/// </summary>
public readonly record struct Rates(int Calm = 0, int Deep = 0, int Work = 0, int Lungs = 0);

/// <summary>A food: what is eaten, how long its meal lasts in ticks, and the rates it gives while it does.</summary>
public sealed record Food(string Id, int Lasts, Rates Rates);

/// <summary>The diet's numbers (food-and-the-body): the caps of food, a hungry hero's base, the meals a hero holds.</summary>
public static class Diet
{
    /// <summary>The most each rate food can reach, whatever is eaten: calm 400, work 500; a canister 25 more (20 + 25, the basic helmet's 45), lungs 20.</summary>
    public static readonly Rates Caps = new(Calm: 400, Deep: 25, Work: 500, Lungs: 20);

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

/// <summary>Everything eaten (food-and-the-body): the raw foods, each an item.</summary>
public static class Foods
{
    public const string Seed = "seed", Ration = "ration", CannedMeat = "cannedMeat", Fruit = "fruit", Glowberry = "glowberry";

    /// <summary>The black market's stimulant (food-tuning): the lungs hold 20 more for 20 minutes; it fills a meal's slot.</summary>
    public const string Stim = "stim";

    /// <summary>The galley's dishes: builder's porridge, spacer's hash, glow broth, roast and hunter's stew.</summary>
    public const string Porridge = "porridge", Hash = "hash", Broth = "broth", Roast = "roast", Stew = "stew";

    /// <summary>Every food, a first guess for the playtest (food-and-the-body, food-tuning).</summary>
    public static readonly IReadOnlyList<Food> All =
    [
        // A snack: eat it or plant it, the player's choice. No rates; it only keeps hunger off for five minutes.
        new(Seed, 6_000, new()),
        // Supply pods and a derelict's fridge: a spacer's ration, for work and a steadier breath.
        new(Ration, 18_000, new(Calm: 50, Work: 300)),
        // A fridge or a vending machine: a deeper breath, a canister 30.
        new(CannedMeat, 18_000, new(Deep: 10)),
        // The farm's crops (fruit-not-biomass), eaten raw: each may give back its seed or spore, as crushing it does.
        new(Fruit, 12_000, new(Calm: 50, Work: 100)),
        new(Glowberry, 12_000, new(Calm: 120)),
        new(Stim, 24_000, new(Lungs: 20)),
        // The galley's dishes, their rates set by hand, each stronger than any raw food of its recipe; no loadout fills every cap.
        // The longer a recipe, the longer its meal lasts (food-tuning): eaten ahead of a trip, not every few minutes.
        new(Porridge, 30_000, new(Calm: 50, Work: 400)),
        new(Hash, 30_000, new(Calm: 50, Deep: 10, Work: 200)),
        new(Broth, 30_000, new(Calm: 300)),
        new(Roast, 30_000, new(Deep: 20)),
        new(Stew, 48_000, new(Calm: 150, Deep: 10)),
    ];

    private static readonly Dictionary<string, Food> ById = All.ToDictionary(f => f.Id, StringComparer.Ordinal);

    /// <summary>The food <paramref name="id"/> is; null for anything not eaten.</summary>
    public static Food? Of(string id) => ById.GetValueOrDefault(id);
}
