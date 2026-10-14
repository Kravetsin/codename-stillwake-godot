using Stillwake.Content;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Furnace;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Hazards;
using Stillwake.Sim.Items;
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
    public BuildingModule Building { get; }
    public PipesModule Pipes { get; }
    public AtmosphereModule Atmosphere { get; }
    public CrewModule Crew { get; }
    public HazardsModule Hazards { get; }
    public DebrisModule Debris { get; }
    public FurnaceModule Furnace { get; }

    /// <summary>In run order.</summary>
    public IReadOnlyList<Module> All { get; }

    public Registry()
    {
        Grid = new GridModule();
        Rooms = new RoomsModule(Grid);
        Items = new ItemsModule(Grid, Rooms);
        Power = new PowerModule(Grid, Rooms, Items, Items);
        Research = new ResearchModule(Grid, Items, Items, Power, Power);
        Building = new BuildingModule(Grid, Grid, Items, Items, Power, Research);
        Pipes = new PipesModule(Grid);
        Atmosphere = new AtmosphereModule(Grid, Items, Items, Rooms, Pipes, Power);
        Crew = new CrewModule(Atmosphere);
        Hazards = new HazardsModule(Grid, Grid);
        Debris = new DebrisModule(Grid, Crew, Rooms, Items);
        Furnace = new FurnaceModule(Grid, Items, Items, Atmosphere, Atmosphere);
        All = [Grid, Rooms, Items, Power, Research, Building, Pipes, Atmosphere, Crew, Hazards, Debris, Furnace];
    }

    /// <summary>The wreck the game starts in.</summary>
    public static Layout Wreck =>
        Layout.Parse(Content.Wreck.Rows, Content.Wreck.Under, Content.Wreck.Cables) with { Known = Content.Research.StartKnown, DebrisAtStart = Content.Debris.AtStart };
}
