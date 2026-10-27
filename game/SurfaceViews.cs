using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Building;
using Stillwake.Game.Features.Crafting;
using Stillwake.Game.Features.Furnace;
using Stillwake.Game.Features.Grid;
using Stillwake.Game.Features.Research;
using Stillwake.Game.Shared;
using Stillwake.Sim;

namespace Stillwake.Game;

/// <summary>What a surface's views are made from: its registry, and what of this machine they read.</summary>
/// <param name="HeadLamps">Each helmet lamp worn on this surface this frame (<c>HeroView.HeadLamps</c>).</param>
/// <param name="HeroBodies">The middle of every hero's body on this surface, in pixels of the world.</param>
/// <param name="Homes">Whether a planet's homes and spires are drawn: <c>--no-homes</c> leaves them out, to measure what they cost.</param>
/// <param name="HomesMood">The homes' mood held lit or dark (<c>--homes</c>); their own by the hour with none.</param>
/// <param name="Peer">This player's peer, and the world's tick (<paramref name="Tick"/>): what a view of the sim's shots reads.</param>
/// <param name="Hosts">Whether this peer hosts its world, so draws its own shots as the sim flies them (<c>ShotDraws</c>).</param>
public sealed record SurfaceContext(
    Registry M, Hands Hands, Func<double> Progress, Func<IReadOnlyList<(Vector2 At, Vector2 Toward, Vector2 Head)>> HeadLamps,
    Func<IReadOnlyList<Vector2>> HeroBodies, bool Homes, bool? HomesMood, Func<int> Peer, Func<int> Tick, Func<bool> Hosts);

/// <summary>One view of a surface: how it is made, null where it has nothing to draw; whether it fades with the plan's front layer.</summary>
public sealed record SurfaceView(Func<SurfaceContext, Node?> Make, bool Fades = false);

/// <summary>
/// Every view of the surface drawn, made again when the registry changes (a load, a resync, a transfer), in the order they are
/// drawn. A new view is a line here; one giving light implements <c>ILightSource</c>, and <c>LightView</c>, made after them,
/// draws its lights.
/// </summary>
public static class SurfaceViews
{
    public static readonly IReadOnlyList<SurfaceView> All =
    [
        new(c => new GridView(c.M.Grid)),
        new(c => new DigView(c.M.Grid, c.M.Building)),
        new(c => new Features.Flora.FloraView(c.M.Grid, c.M.Building)),
        // A planet's caves: their stone tinted by depth under their growth (0080).
        new(c => c.M.Rules is Content.PlanetType p ? new Features.Flora.CaveTint(p, PlanetLooks.Of[p.Id]) : null),
        new(c => c.M.Rules is Content.PlanetType p ? new Features.Flora.CaveView(c.M.Grid, c.M.Building, p, c.HeadLamps) : null),
        // The arc makers' homes in its ground (0091), and the spires alight round their relics, dark once taken (0096).
        new(c => c.M.Rules is Content.PlanetType && c.Homes ? new Features.Ruins.HomesView(c.M.Grid, c.M.Building, c.HeroBodies) { Mood = c.HomesMood } : null),
        new(c => c.M.Rules is Content.PlanetType && c.Homes ? new Features.Ruins.SpiresView(c.M.Grid, c.M.Building, c.HeroBodies) : null),
        // Its core: lava under the caves, glowing.
        new(c => c.M.Rules is Content.PlanetType p ? new LavaView(c.M.Grid, p.Ground) : null),
        new(c => new AnalyzerScreens(c.M.Grid, c.M.Research, c.M.Power), Fades: true),
        new(c => new Features.Computer.ComputerScreens(c.M.Grid, c.M.Power), Fades: true),
        new(c => new Features.Power.ChargerLights(c.M.Grid, c.M.Items, c.M.Power), Fades: true),
        new(c => new Features.Power.StoreLights(c.M.Grid, c.M.Power), Fades: true),
        new(c => new Features.Power.PlatformLamps(c.M.Grid, c.M.Power), Fades: true),
        // A seated hero behind its pod's porthole, then the pod's lamps and the glint on its glass over it.
        new(c => new Features.Crew.SeatedHeroes(c.M.Grid, c.M.Landing, c.M.Players), Fades: true),
        new(c => new Features.Landing.PodSeats(c.M.Grid, c.M.Landing), Fades: true),
        new(c => new Features.Atmosphere.TankLooks(c.M.Grid, c.M.Atmosphere), Fades: true),
        new(c => new PodView(c.M.Grid, c.M.Crafting, c.M.Items, c.M.Crew), Fades: true),
        new(c => new StationView(c.M.Grid, c.M.Crafting, c.M.Items), Fades: true),
        new(c => new BenchView(c.M.Grid, c.M.Items), Fades: true),
        new(Drones),
        // Every peer's shots but this player's own, which its hero's hands draw ahead of the sim's (shots-are-the-sims).
        new(c => new Features.Creatures.ShotsView(c.M, c.Progress, c.Peer, c.Tick, c.Hosts)),
        // The fires last, before the light Main makes after these: each frame a fire picks its picture, then its light follows it.
        new(c => new FurnaceView(c.M.Grid, c.M.Furnace), Fades: true),
        // A planet's fog at its sides (fog-edges): a screen layer over all of these and the heroes, under what is read.
        new(c => FogEdges.Of(c.M.Grid, c.M.Rules) is { } fog ? new Features.Fog.FogView(fog, PlanetLooks.Of[c.M.Rules.Id].Fog) : null),
    ];

    /// <summary>The drones, a bay's zone shown while its inventory is open or a zoned machine's card is held (0062).</summary>
    private static Node Drones(SurfaceContext c)
    {
        var (m, ui, plan, mouse) = (c.M, c.Hands.Ui, c.Hands.Plan, c.Hands.Mouse);
        return new Features.Logistics.LogisticsView(m.Grid, m.Items, m.Power, m.Logistics, c.Progress,
            () => ui.InventoryFoot.Get() != null && ui.OpenCrate.Get() is int open && m.Grid.MachineById(open) is { } bay && Stillwake.Content.Machines.Is(bay.Kind, Stillwake.Content.MachineTrait.Zone) ? open : null,
            () => ui.Hand.Get() == Holding.Card && plan.Selected.Get() is PlaceTool { Placement.Kind: var kind } && Stillwake.Content.Machines.Is(kind, Stillwake.Content.MachineTrait.Zone) && mouse.Pointer() is { } at ? (at.X, at.Y, kind) : null);
    }
}
