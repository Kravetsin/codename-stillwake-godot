namespace Stillwake.Content;

/// <summary>
/// The kinds of surface a world may open besides the ship (0076): each names its rules and the ground it is
/// drawn as. So far one, a flat strip to stand heroes on while planets are built; its rules are the ship's.
/// </summary>
public static class Surfaces
{
    /// <summary>A kind of surface: its id, saved with each surface of it, and its ground drawn as a map in <see cref="Legend"/>'s characters.</summary>
    public sealed record Kind(string Id, IReadOnlyList<string> Rows);

    /// <summary>
    /// A long floor with a few steps and walls, a band of back wall above it and open sky over that: no generator
    /// yet, the ground drawn by hand. Heroes beam down to its <c>S</c>.
    /// </summary>
    public static readonly Kind TestStrip = new("testStrip", Strip());

    public static readonly IReadOnlyList<Kind> All = [TestStrip];

    /// <summary>The kind named <paramref name="id"/>; one not here is our bug: peers run one build.</summary>
    public static Kind Get(string id) =>
        All.FirstOrDefault(k => k.Id == id) ?? throw new KeyNotFoundException($"No surface kind '{id}'.");

    private static string[] Strip()
    {
        const int width = 160, sky = 24;
        var rows = new List<string>();
        for (int y = 0; y < sky; y++) rows.Add(new string(' ', width));
        string Band(string cut) => cut.PadRight(width, '.');
        rows.Add(Band(""));
        rows.Add(Band(""));
        rows.Add(Band(".........................................................#.........................................######"));
        rows.Add(Band("..............S........................###...............#........................................#######"));
        rows.Add(Band("....................................#####................#............................###.......########"));
        for (int y = 0; y < 4; y++) rows.Add(new string('#', width));
        return [.. rows];
    }
}
