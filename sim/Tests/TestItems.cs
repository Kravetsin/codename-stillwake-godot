using System.Reflection;
using System.Runtime.CompilerServices;
using Stillwake.Content;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Items for rules no content uses yet: added to <c>Content.Items</c>' lookup alone as this assembly loads, before any
/// test runs and so before any reads it, never to <c>Items.All</c>, so content tests and the fuzz never see them.
/// </summary>
internal static class TestItems
{
    /// <summary>Ore, as the crusher's to come: an iron and a scrap a smelt, and one smelt in two a crystal besides.</summary>
    public const string Ore = "testOre";

    /// <summary>Gravel, as the crusher's to come: four a smelt into one glass.</summary>
    public const string Gravel = "testGravel";

    public static readonly IReadOnlyList<ItemSpec> All =
    [
        new(Ore, 50, Yields: [("iron", 1), ("scrap", 1)], SmeltTicks: 20, Byproducts: [new LootEntry("crystal", 1, 1, OneIn: 2)], Group: ItemGroup.Salvage),
        new(Gravel, 50, Yields: [("glass", 1)], SmeltTicks: 20, SmeltTakes: 4),
    ];

#pragma warning disable CA2255 // A test assembly, not a library: its content must be whole before its first test.
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Add()
    {
        var byId = (Dictionary<string, ItemSpec>)typeof(Content.Items)
            .GetField("ById", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        foreach (var spec in All) byId.Add(spec.Id, spec);
    }
}
