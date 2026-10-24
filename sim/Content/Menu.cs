namespace Stillwake.Content;

/// <summary>
/// Where a card sits in the build menu (0114): its <paramref name="Tab"/>, one of <see cref="MenuTabs.All"/>, and its place on
/// it, lower first; cards of one order keep their rows' order, materials' walls first, then their back walls, builds, machines.
/// </summary>
public sealed record MenuSlot(string Tab, int Order);

/// <summary>
/// The build menu's tabs in order (0114): what the player builds, grouped by use. A card is a row with a <see cref="MenuSlot"/>:
/// a machine's <c>Menu</c>, a build's, a material's for its wall and its <c>BackMenu</c> for its back wall.
/// </summary>
public static class MenuTabs
{
    public const string Structure = "structure", Air = "air", Power = "power", Production = "production", Storage = "storage";

    public static readonly IReadOnlyList<string> All = [Structure, Air, Power, Production, Storage];
}
