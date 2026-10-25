using Stillwake.Content;

namespace Stillwake.Sim.Items;

/// <summary>
/// What a tool works with (0098): its damage a hit, the ticks between its hits and what it draws a tick it runs, folded
/// once from its item and the modules fitted into it, each module's trades in that tool (0100). The sim's hits and draws and the
/// client's card read this one fold, so they never disagree. The modules' additions are summed first (<see cref="Boost.Plus"/>),
/// then applied: damage a gain, rounded down, the draw a cost, rounded up. A tool's pace is its weapon's: speed is a station's.
/// </summary>
/// <param name="Damage">A hit's damage; 0 for an item that deals none.</param>
/// <param name="Pace">The ticks between its hits, its weapon's; 0 for an item that deals no damage.</param>
/// <param name="Draw">What it draws a tick it runs; 0 for an item that draws nothing.</param>
public readonly record struct ToolStats(int Damage, int Pace, int Draw)
{
    private static readonly Dictionary<string, ModuleSpec[]> Alone =
        Modules.All.ToDictionary(m => m.Item, m => new[] { m }, StringComparer.Ordinal);

    /// <summary>The stats of <paramref name="stack"/>'s tool, the modules fitted into it with it.</summary>
    public static ToolStats Of(Stack stack) => Of(stack.Item, ModulesOf(stack));

    /// <summary>The stats of <paramref name="item"/> with <paramref name="modules"/> fitted, in any number; none for the plain tool.</summary>
    public static ToolStats Of(string item, IReadOnlyList<ModuleSpec> modules)
    {
        var boost = new Boost();
        for (int i = 0; i < modules.Count; i++) boost = boost.Plus(modules[i], item);
        var weapon = Weapons.Of(item);
        int draw = Content.Items.Get(item).Draw ?? 0;
        return new ToolStats(weapon is null ? 0 : Boost.Gain(weapon.Damage, boost.Damage), weapon?.Pace ?? 0, Boost.Cost(draw, boost.Draw));
    }

    /// <summary>
    /// What a hit of <paramref name="stack"/>'s tool takes off a block (the fan of rays): its weapon's block damage, gained as its
    /// damage is by the modules fitted (0098); 0 for an item that deals none.
    /// </summary>
    public static int BlockDamageOf(Stack stack) => BlockDamageOf(stack.Item, ModulesOf(stack));

    /// <summary>What a hit of <paramref name="item"/> with <paramref name="modules"/> fitted takes off a block.</summary>
    public static int BlockDamageOf(string item, IReadOnlyList<ModuleSpec> modules)
    {
        var boost = new Boost();
        for (int i = 0; i < modules.Count; i++) boost = boost.Plus(modules[i], item);
        return Weapons.Of(item) is { } weapon ? Boost.Gain(weapon.BlockDamage, boost.Damage) : 0;
    }

    /// <summary>The modules fitted into <paramref name="stack"/>'s tool, in the order they are fitted; none for a plain one. Never a new list for one.</summary>
    public static IReadOnlyList<ModuleSpec> ModulesOf(Stack stack) => stack.Module is { } module ? Alone[module] : [];
}
