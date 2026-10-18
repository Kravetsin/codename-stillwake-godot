using Stillwake.Content;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Furnace;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Hazards;
using Stillwake.Sim.Items;
using Stillwake.Sim.Logistics;
using Stillwake.Sim.Pipes;
using Stillwake.Sim.Power;
using Stillwake.Sim.Research;
using Stillwake.Sim.Rooms;

namespace Stillwake.Sim;

/// <summary>
/// Every module of one world, wired: each takes what it depends on through its constructor, and
/// <see cref="All"/> lists them in run order, every module after its dependencies (MODULES in the
/// TypeScript sim). A world gets a registry of its own: modules hold its state.
/// </summary>
public sealed class Registry
{
    public GridModule Grid { get; }
    public RoomsModule Rooms { get; }
    public ItemsModule Items { get; }
    public PowerModule Power { get; }
    public ResearchModule Research { get; }
    public CraftingModule Crafting { get; }
    public BuildingModule Building { get; }
    public PipesModule Pipes { get; }
    public AtmosphereModule Atmosphere { get; }
    public CrewModule Crew { get; }
    public HazardsModule Hazards { get; }
    public DebrisModule Debris { get; }
    public FurnaceModule Furnace { get; }
    public LogisticsModule Logistics { get; }

    /// <summary>In run order.</summary>
    public IReadOnlyList<Module> All { get; }

    public Registry()
    {
        Grid = new GridModule();
        Rooms = new RoomsModule(Grid);
        Items = new ItemsModule(Grid, Rooms);
        Power = new PowerModule(Grid, Rooms, Items, Items);
        Research = new ResearchModule(Grid, Items, Power, Power);
        Crafting = new CraftingModule(Grid, Items, Items, Research, Power, Power);
        Building = new BuildingModule(Grid, Grid, Items, Items, Power, Research, Crafting);
        Pipes = new PipesModule(Grid);
        Atmosphere = new AtmosphereModule(Grid, Items, Items, Rooms, Pipes, Power);
        Crew = new CrewModule(Atmosphere, Grid, Items, Items);
        Hazards = new HazardsModule(Grid, Grid);
        Debris = new DebrisModule(Grid, Crew, Rooms, Items, Items);
        Furnace = new FurnaceModule(Grid, Items, Items, Atmosphere, Atmosphere, Power, Crafting, Crafting);
        Logistics = new LogisticsModule(Grid, Atmosphere, Items, Items, Crafting);
        All = [Grid, Rooms, Items, Power, Research, Crafting, Building, Pipes, Atmosphere, Crew, Hazards, Debris, Furnace, Logistics];
    }

    /// <summary>
    /// The wreck as content draws it, with no zone and no space round it but its own: for dev worlds and
    /// tests whose cells are the drawing's.
    /// </summary>
    public static readonly Layout WreckMap =
        Layout.Parse(Content.Wreck.Rows, Content.Wreck.Under, Content.Wreck.Cables) with { Known = Content.Research.StartKnown, DebrisAtStart = Content.Debris.AtStart };

    /// <summary>The world every new game starts in: the wreck centred in a world of <c>Content.World</c>'s size, inside its build zone (0048).</summary>
    public static readonly Layout Wreck = WreckMap.InWorld();
}
