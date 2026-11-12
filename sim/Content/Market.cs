namespace Stillwake.Content;

/// <summary>
/// What things are worth, in credits (2026-10-30-tech-graph). Only what no recipe makes is priced by hand: anything else is worth
/// its inputs and the time they take, marked up, per unit it comes to. Every step of a chain so pays, and a recipe changed
/// reprices all made of it. A trade station sends what it holds by capsule, at a price that never falls (2026-10-30-trade-capsule).
/// </summary>
public static class Market
{
    /// <summary>Ticks a trade station's capsule is away, launch to credits: thirty seconds.</summary>
    public const int CapsuleFlight = 30 * 20;

    /// <summary>What a step of a chain adds to its inputs and time, in percent.</summary>
    public const int Markup = 125;

    /// <summary>Ticks of a recipe's work worth a credit: a credit a second.</summary>
    public const int TicksPerCredit = 20;

    /// <summary>
    /// What no recipe makes, what is grown so slowly that its time would swamp it, or what the ground gives for a swing, whatever
    /// a recipe would make of it: all else is worked out from these.
    /// </summary>
    private static readonly Dictionary<string, int> Raw = new(StringComparer.Ordinal)
    {
        ["scrapChunk"] = 20,
        ["hullPlate"] = 40,
        ["asteroidOre"] = 30,
        ["rock"] = 1,
        ["soil"] = 1,
        ["quartz"] = 50,
        ["planks"] = 20,
        ["cloth"] = 30,
        ["hide"] = 40,
        ["fat"] = 30,
        ["seed"] = 30,
        ["spore"] = 20,
        ["biomass"] = 20,
        // Grown, priced as the biomass each is crushed into: never sold (rule 7 of the food plan), but a dish is worth its fruit.
        ["fruit"] = 20,
        ["glowberry"] = 20,
        ["ice"] = 30,
        // Salvaged only since its ice craft went (2026-11-06-no-ice-canister): that craft's worth kept, so what is made of it costs the same.
        ["canister"] = 117,
        ["copperOre"] = 30,
        ["mica"] = 50,
        ["magnetite"] = 60,
        ["sulfur"] = 40,
        ["opal"] = 150,
        ["alloyScrap"] = 40,
        ["crystal"] = 100,
        ["arcLamp"] = 300,
        ["arcShard"] = 200,
        // Gear only the mafia sells, delivered (2026-11-08-mafia-deliveries): the worth its craft had when it went, so its price stays put.
        ["scanner"] = 492,
        ["powerDriver"] = 750,
        ["heavyHelmet"] = 1239,
        ["plasmaCutter"] = 1032,
        ["regenSuit"] = 1457,
    };

    /// <summary>What has a worth but no one buys: tools and what is worn, what stores or flies by itself, the arc's.</summary>
    private static readonly HashSet<string> Unsold = new(StringComparer.Ordinal)
    {
        "ration", "cannedMeat", "fruit", "glowberry", "arcShard", "arcModule", "drone", "salvageDrone", "screwdriver", "fireAxe", "harpoon", "grapple", "nailer", "railgun", "drill", "powerDriver", "grinder", "plasmaCutter", "spear", "sledgehammer", "pickaxe", "jetpack", "helmetLamp", "scanner", "battery", "accumulator", "helmet", "heavyHelmet", "suit", "regenSuit", "cape",
    };

    private static readonly Dictionary<string, int> Worths = Worked();

    /// <summary>What a unit of <paramref name="item"/> is worth: 0 for what nothing makes and nothing prices, wreckage whole.</summary>
    public static int WorthOf(string item) => Worths.GetValueOrDefault(item);

    /// <summary>What a unit of <paramref name="item"/> sells for; null for what no one buys: gear and what is never carried.</summary>
    public static int? PriceOf(string item) => WorthOf(item) > 0 && !Unsold.Contains(item) ? WorthOf(item) : null;

    private static Dictionary<string, int> Worked()
    {
        var worths = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var item in Items.All) Worth(item.Id, worths, []);
        return worths;
    }

    private static int Worth(string id, Dictionary<string, int> worths, HashSet<string> working)
    {
        if (worths.TryGetValue(id, out int known)) return known;
        if (!working.Add(id)) throw new InvalidOperationException($"market: {id} is made of itself; price one of its chain by hand");
        int worth = 0;
        if (Raw.TryGetValue(id, out int raw)) worth = raw;
        else if (Crafts.TryGet(id, out var craft) && craft.Cost.Count > 0) worth = Marked(craft.Cost.Sum(c => c.Count * Worth(c.Item, worths, working)), craft.Ticks, 1);
        else if (Items.All.Where(i => i.Yields?.Any(y => y.Item == id) == true).ToList() is { Count: > 0 } sources)
            // Smelted from several, it is worth its cheapest: iron its hull plate's, never the four scrap smelted at a loss.
            worth = sources.Min(s => Marked(s.SmeltTakes * Worth(s.Id, worths, working), s.SmeltTicks ?? 0, s.Yields!.Sum(y => y.Count)));
        working.Remove(id);
        worths[id] = worth;
        return worth;
    }

    /// <summary>The worth of a unit of <paramref name="units"/> made of <paramref name="inputs"/> in <paramref name="ticks"/>, rounded up, so a step never loses to rounding.</summary>
    private static int Marked(int inputs, int ticks, int units)
    {
        long marked = (inputs * (long)TicksPerCredit + ticks) * Markup, per = TicksPerCredit * 100L * units;
        return (int)((marked + per - 1) / per);
    }
}
