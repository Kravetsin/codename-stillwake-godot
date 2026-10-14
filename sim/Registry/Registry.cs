using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Pipes;
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
    public PipesModule Pipes { get; }

    /// <summary>In run order.</summary>
    public IReadOnlyList<Module> All { get; }

    public Registry()
    {
        Grid = new GridModule();
        Rooms = new RoomsModule(Grid);
        Pipes = new PipesModule(Grid);
        All = [Grid, Rooms, Pipes];
    }

    /// <summary>The wreck the game starts in.</summary>
    public static Layout Wreck => Layout.Parse(Content.Wreck.Rows, Content.Wreck.Under);
}
