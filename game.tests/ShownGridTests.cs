using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Xunit;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Game.Tests;

/// <summary>"the grid as this peer shows it" of session/predicted.ts, ported.</summary>
public class ShownGridTests
{
    private const string Map = "#D#\n#D#\n...";
    private static readonly int[] Open = [Structure.DoorOpen, Structure.DoorOpen];
    private static readonly int[] Shut = [Structure.DoorClosed, Structure.DoorClosed];

    /// <summary>A guest whose host is not here: the test sends its commands and releases its ticks by hand.</summary>
    private sealed class Guest
    {
        private readonly Fed _link = new();
        public Registry M { get; } = new();
        public Client Client { get; }

        public Guest() => Client = new Client(World.Create(1, Layout.Parse(Map), M.All), _link, 1);

        public ShownGrid Shown => Predicted.Grid(Client, M.Grid, M.Pipes);

        /// <summary>Releases the next tick with these commands, stamped as this guest's, and runs it.</summary>
        public void Run(params Command[] commands)
        {
            _link.Receive(new TickMessage(Client.World.Tick, commands.Select(c => new Stamped(1, Client.World.Encode(c))).ToList()));
            Client.Update();
        }
    }

    private static int[] Doorway(ShownGrid shown, IGridQueries grid) => [shown.StructureAt(grid, 1), shown.StructureAt(grid, 4)];

    [Fact]
    public void IsTheWorldAsItIsWithNothingInFlight()
    {
        var g = new Guest();
        Assert.Null(g.Shown.Structure);
        Assert.Equal(Structure.DoorClosed, g.Shown.StructureAt(g.M.Grid, 4));
    }

    [Fact]
    public void ShowsADoorToggleAtOnceTheWholeDoorwayWhileTheWorldWaitsForItsTick()
    {
        var g = new Guest();
        g.Client.Send(new SetDoor(4, true));
        Assert.Equal(Open, Doorway(g.Shown, g.M.Grid));
        Assert.Equal(Shut, new[] { g.M.Grid.StructureAt(1), g.M.Grid.StructureAt(4) });
        Assert.Equal(Structure.DoorOpen, g.Shown.StructureAt(g.M.Grid, 1));
    }

    [Fact]
    public void ShowsTheLastOfTwoSetsInFlightAndOneTheSimWouldRefuseNotAtAll()
    {
        var g = new Guest();
        g.Client.Send(new SetDoor(1, true));
        g.Client.Send(new SetDoor(4, false));
        g.Client.Send(new SetDoor(7, true));
        Assert.Equal(Shut, Doorway(g.Shown, g.M.Grid));
        Assert.Equal(Structure.Empty, g.Shown.StructureAt(g.M.Grid, 7));
    }

    [Fact]
    public void ShowsTheSameOnceTheTickRunsItWithNothingInFlight()
    {
        var g = new Guest();
        g.Client.Send(new SetDoor(1, true));
        var before = g.Shown;
        g.Run(new SetDoor(1, true));
        Assert.Empty(g.Client.InFlight);
        Assert.Equal(Doorway(before, g.M.Grid), Doorway(g.Shown, g.M.Grid));
        Assert.NotEqual(before.Key, g.Shown.Key);
    }

    [Fact]
    public void IsMadeOnceForTheSameWorldAndTheSameToggles()
    {
        var g = new Guest();
        g.Client.Send(new SetDoor(1, true));
        Assert.Same(g.Shown, g.Shown);
    }
}
