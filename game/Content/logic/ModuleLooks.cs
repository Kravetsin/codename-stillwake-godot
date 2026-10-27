using System.Collections.Generic;
using Stillwake.Content;

namespace Stillwake.Game;

/// <summary>
/// A mount on a held tool's picture (0098), where a fitted module shows: its kind (<see cref="Id"/>, the drill's coil), the
/// pixel of the tool's picture an overlay's top-left goes on, and the box of it the overlay's live effect plays over. A
/// mount of one kind is one shape on every tool that has it, so one overlay fits them all.
/// </summary>
public sealed record ToolMount(string Id, int X, int Y, (double X0, double Y0, double X1, double Y1) Box);

/// <summary>What plays over a mount while its tool runs: nothing, or the arc's crackle (<c>DrillLight</c>).</summary>
public enum MountFx
{
    None,
    Crackle,
}

/// <summary>A module's picture for one kind of mount, drawn over the tool's at it, and what plays over it while the tool runs.</summary>
public sealed record ModuleOverlay(string Mount, string Picture, MountFx Fx = MountFx.None);

/// <summary>
/// How each module looks fitted into a tool (0098), by its item: an overlay for each kind of mount it shows on. A tool's
/// held look, in the hand and on a bench, is put together from these and its own (<see cref="HeldParts"/>), so no combination
/// of tool and modules is ever drawn by hand.
/// </summary>
public static class ModuleLooks
{
    /// <summary>The arc module (0075, 0096): the drill's copper coil turned blue, <c>art/module_arc_coil.png</c>, and crackling as it runs.</summary>
    public static readonly IReadOnlyList<ModuleOverlay> Arc = [new("coil", "res://art/module_arc_coil.png", MountFx.Crackle)];

    private static readonly Dictionary<string, IReadOnlyList<ModuleOverlay>> All = new() { [Modules.Arc.Item] = Arc };

    /// <summary>The overlays of module <paramref name="module"/>; none for one that shows on no tool.</summary>
    public static IReadOnlyList<ModuleOverlay> Of(string module) => All.TryGetValue(module, out var overlays) ? overlays : [];
}

/// <summary>One picture laid over a held tool's own, its top-left at (<see cref="X"/>, <see cref="Y"/>) of it.</summary>
public sealed record HeldLayer(string Picture, int X, int Y);

/// <summary>
/// A held tool as it is drawn with its modules fitted (0098): its look, the overlays laid over each of its pictures (the
/// still one and every spin frame alike), bottom first, and the boxes of its picture where the arc crackles while it runs.
/// </summary>
public sealed record HeldParts(HeldLook Look, IReadOnlyList<HeldLayer> Layers, IReadOnlyList<(double X0, double Y0, double X1, double Y1)> Crackles)
{
    /// <summary><paramref name="look"/> with <paramref name="modules"/> fitted, as fitted (their order on the stack), each as <see cref="ModuleLooks"/> draws it.</summary>
    public static HeldParts Of(HeldLook look, IReadOnlyList<ModuleSpec> modules)
    {
        var overlays = new IReadOnlyList<ModuleOverlay>[modules.Count];
        for (int i = 0; i < modules.Count; i++) overlays[i] = ModuleLooks.Of(modules[i].Item);
        return Overlaid(look, overlays);
    }

    /// <summary>
    /// <paramref name="look"/> with modules fitted whose <paramref name="overlays"/> are these, a list a module, in the modules'
    /// order: its mounts in the order the tool declares them, and on each the modules' overlays for its kind in the modules'
    /// order, each over the one before. An overlay for a mount the tool lacks shows nowhere.
    /// </summary>
    public static HeldParts Overlaid(HeldLook look, IReadOnlyList<IReadOnlyList<ModuleOverlay>> overlays)
    {
        if (overlays.Count == 0 || look.Mounts is not { Count: > 0 } mounts) return new HeldParts(look, [], []);
        var layers = new List<HeldLayer>();
        var crackles = new List<(double, double, double, double)>();
        foreach (var mount in mounts)
            for (int i = 0; i < overlays.Count; i++)
                foreach (var overlay in overlays[i])
                {
                    if (overlay.Mount != mount.Id) continue;
                    layers.Add(new HeldLayer(overlay.Picture, mount.X, mount.Y));
                    if (overlay.Fx == MountFx.Crackle && !crackles.Contains(mount.Box)) crackles.Add(mount.Box);
                }
        return new HeldParts(look, layers, crackles);
    }
}
