using Stillwake.Content;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Creatures;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Furnace;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Hazards;
using Stillwake.Sim.Items;
using Stillwake.Sim.Knowledge;
using Stillwake.Sim.Landing;
using Stillwake.Sim.Logistics;
using Stillwake.Sim.Pipes;
using Stillwake.Sim.Players;
using Stillwake.Sim.Power;
using Stillwake.Sim.Research;
using Stillwake.Sim.Rooms;
using Stillwake.Sim.Trade;

namespace Stillwake.Sim;

/// <summary>
/// Every module of one world, wired: each takes what it depends on through its constructor, and
/// <see cref="All"/> lists them in run order, every module after its dependencies (MODULES in the
/// TypeScript sim). A world gets a registry of its own: modules hold its state. Each surface of a world (0076) has a
/// registry, sharing the world's globals, Players and Knowledge: the ship's lists them first in <see cref="All"/>, so the
/// ship's slices save them; another surface's leaves them out.
/// </summary>
public sealed class Registry
{
    /// <summary>The world's, one for all its surfaces: suit colours.</summary>
    public PlayersModule Players { get; }
    /// <summary>The world's, one for all its surfaces: what is granted and analysed.</summary>
    public KnowledgeModule Knowledge { get; }
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
    public LandingModule Landing { get; }
    public DebrisModule Debris { get; }
    public FurnaceModule Furnace { get; }
    public LogisticsModule Logistics { get; }
    public CreaturesModule Creatures { get; }
    public TradeModule Trade { get; }

    /// <summary>The rules of the kind of surface this registry runs (0076): what the client draws and allows by.</summary>
    public Surfaces.Kind Rules => Grid.Rules;

    /// <summary>In run order: on the ship, the globals first.</summary>
    public IModules All { get; }

    /// <summary>The registry of the ship's surface, whose globals this one shares; itself on the ship.</summary>
    private readonly Registry _ship;

    /// <summary>A world's ship: its globals made here.</summary>
    public Registry() : this(null) { }

    private Registry(Registry? ship)
    {
        _ship = ship ?? this;
        Players = ship?.Players ?? new PlayersModule();
        Knowledge = ship?.Knowledge ?? new KnowledgeModule(ResearchModule.Ids);
        Grid = new GridModule();
        Rooms = new RoomsModule(Grid);
        Items = new ItemsModule(Grid, Rooms, Knowledge);
        Power = new PowerModule(Grid, Rooms, Items, Items);
        Research = new ResearchModule(Items, Items, Knowledge, Knowledge);
        Crafting = new CraftingModule(Grid, Items, Items, Research, Power, Power);
        Pipes = new PipesModule(Grid, Items, Items);
        Atmosphere = new AtmosphereModule(Grid, Items, Items, Rooms, Pipes, Power);
        Crew = new CrewModule(Atmosphere, Atmosphere, Grid, Items, Items, Players, Players);
        // Building reads where heroes are and the pipes only as its commands apply, never in a system, so it keeps its place in
        // the run order and the saves', before pipes and crew (0077: a wall does not finish on a hero; 2026-10-29-typed-ports).
        Building = new BuildingModule(Grid, Grid, Items, Items, Power, Power, Research, Knowledge, Crafting, Crew, Crew, Pipes);
        Hazards = new HazardsModule(Grid, Grid, Power, Power, Research);
        Debris = new DebrisModule(Grid, Crew, Rooms, Items, Items);
        Furnace = new FurnaceModule(Grid, Items, Items, Atmosphere, Atmosphere, Power, Power, Crafting, Crafting);
        Logistics = new LogisticsModule(Grid, Atmosphere, Items, Items, Crafting);
        // Last, after crew, as its system reads where heroes are; saved last, so its slice is the newest.
        Creatures = new CreaturesModule(Grid, Crew, Crew, Items, Items, Debris, Debris, Building, Building, Rooms);
        // The launch platform, opened with power; new to format 30, run and saved after the rest.
        Landing = new LandingModule(Grid, Grid, Power, Crew, Items, Items);
        Trade = new TradeModule(Grid, Items, Items, Knowledge);
        Module[] surface = [Grid, Rooms, Items, Power, Research, Crafting, Building, Pipes, Atmosphere, Crew, Hazards, Debris, Furnace, Logistics, Creatures, Landing, Trade];
        All = new Set(this, ship == null ? [Players, Knowledge, .. surface] : surface);
    }

    /// <summary>The registry surface <paramref name="surface"/> of <paramref name="world"/> runs: the world must be made of registries.</summary>
    public static Registry On(World world, int surface) =>
        world.ModulesOn(surface) is Set set ? set.Owner : throw new InvalidOperationException("This world is not made of registries.");

    /// <summary>A registry's modules, which make another surface's registry of the same world.</summary>
    private sealed class Set(Registry owner, Module[] modules) : IModules
    {
        public Registry Owner => owner;
        public Module this[int index] => modules[index];
        public int Count => modules.Length;
        public IModules Another() => new Registry(owner._ship).All;
        public IEnumerator<Module> GetEnumerator() => ((IEnumerable<Module>)modules).GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => modules.GetEnumerator();
    }

    /// <summary>
    /// The wreck as content draws it, a character a 16 px cell, doubled to the 8 px cells (0104), with no zone and no space
    /// round it but its own: for dev worlds and tests whose cells are the drawing's, doubled.
    /// </summary>
    public static readonly Layout WreckMap =
        Layout.Parse(Content.Wreck.Rows, Content.Wreck.Under, Content.Wreck.Cables).Doubled() with { Known = Content.Research.StartKnown, DebrisAtStart = Content.Debris.AtStart };

    /// <summary>The world every new game starts in: the wreck centred in a world of <c>Content.World</c>'s size, inside its build zone (0048).</summary>
    public static readonly Layout Wreck = WreckMap.InWorld() with { Meteors = true };
}
