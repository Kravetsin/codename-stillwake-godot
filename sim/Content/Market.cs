namespace Stillwake.Content;

/// <summary>
/// What things are worth, in credits (2026-10-30-tech-graph). Only what no recipe makes is priced by hand: anything else is worth
/// its inputs and the time they take, marked up, per unit it comes to. Every step of a chain so pays, and a recipe changed
/// reprices all made of it. Selling at a trade station is one unit every <see cref="SellEvery"/> ticks, at a price that never falls.
/// </summary>
public static class Market
{
    /// <summary>Ticks from one unit sold to the next at a station: two a second.</summary>
    public const int SellEvery = 10;

    /// <summary>What a step of a chain adds to its inputs and time, in percent.</summary>
    public const int Markup = 125;

    /// <summary>Ticks of a recipe's work worth a credit: a credit a second.</summary>
    public const int TicksPerCredit = 20;

    /// <summary>What no recipe makes, or what is grown so slowly that its time would swamp it: all else is worked out from these.</summary>
    private static readonly Dictionary<string, int> Raw = new(StringComparer.Ordinal)
    {
        ["scrapChunk"] = 20,
        ["hullPlate"] = 40,
        ["asteroidOre"] = 30,
        ["rock"] = 4,
        ["quartz"] = 50,
        ["planks"] = 20,
        ["cloth"] = 30,
        ["hide"] = 40,
        ["fat"] = 30,
        ["seed"] = 30,
        ["spore"] = 20,
        ["biomass"] = 20,
        ["ice"] = 30,
        ["copperOre"] = 30,
        ["mica"] = 50,
        ["magnetite"] = 60,
        ["alloyScrap"] = 40,
        ["crystal"] = 100,
        ["arcLamp"] = 300,
        ["arcShard"] = 200,
    };

    /// <summary>What has a worth but no one buys: tools and what is worn, what stores or flies by itself, the arc's.</summary>
    private static readonly HashSet<string> Unsold = new(StringComparer.Ordinal)
    {
        "arcShard", "arcModule", "drone", "screwdriver", "fireAxe", "harpoon", "nailer", "railgun", "drill", "jetpack", "helmetLamp", "battery", "accumulator", "helmet", "suit", "cape",
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
        else if (Items.All.FirstOrDefault(i => i.Yields?.Any(y => y.Item == id) == true) is { } source)
            worth = Marked(source.SmeltTakes * Worth(source.Id, worths, working), source.SmeltTicks ?? 0, source.Yields!.Sum(y => y.Count));
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
