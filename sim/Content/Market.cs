namespace Stillwake.Content;

/// <summary>
/// Selling goods for credits at a trade station (2026-10-30-trade-and-tech): one unit every <see cref="SellEvery"/> ticks, at a
/// price that never falls. Refined pays more than what it is made of, or nothing would be worth processing.
/// </summary>
public static class Market
{
    /// <summary>Ticks from one unit sold to the next at a station: two a second.</summary>
    public const int SellEvery = 10;

    private static readonly Dictionary<string, int> Prices = new(StringComparer.Ordinal)
    {
        // Raw.
        ["scrap"] = 1,
        ["gravel"] = 1,
        ["rock"] = 1,
        ["scrapChunk"] = 2,
        ["planks"] = 2,
        ["spore"] = 2,
        ["biomass"] = 2,
        ["ironOre"] = 2,
        ["iron"] = 3,
        ["asteroidOre"] = 3,
        ["seed"] = 3,
        ["copperOre"] = 3,
        ["ice"] = 3,
        ["cloth"] = 3,
        ["fat"] = 3,
        // Finds and refined.
        ["hullPlate"] = 4,
        ["alloyScrap"] = 4,
        ["hide"] = 4,
        ["quartz"] = 5,
        ["copper"] = 5,
        ["mica"] = 5,
        ["soil"] = 5,
        ["glass"] = 6,
        ["magnetite"] = 6,
        ["biofuel"] = 6,
        ["silicon"] = 8,
        ["crystal"] = 10,
        ["arcLamp"] = 30,
        // Parts.
        ["wire"] = 3,
        ["nail"] = 3,
        ["gear"] = 4,
        ["tube"] = 4,
        ["plate"] = 8,
        ["canister"] = 12,
        ["rotor"] = 13,
        ["coil"] = 16,
        ["frame"] = 42,
        // Electronics.
        ["circuit"] = 24,
        ["solarCell"] = 36,
        ["display"] = 120,
        ["deckDrive"] = 180,
        ["transceiver"] = 210,
        ["processor"] = 270,
        // Engine parts.
        ["motor"] = 60,
        ["nozzle"] = 100,
        ["igniter"] = 110,
        ["fuelPump"] = 200,
    };

    /// <summary>What a unit of <paramref name="item"/> sells for; null for what no one buys: gear and what is never carried.</summary>
    public static int? PriceOf(string item) => Prices.TryGetValue(item, out int price) ? price : null;
}
