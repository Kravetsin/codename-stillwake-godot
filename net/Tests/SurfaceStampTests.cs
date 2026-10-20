using Stillwake.Sim;
using Xunit;

namespace Stillwake.Net.Tests;

/// <summary>A command goes out stamped with the surface its sender is on (0076).</summary>
public class SurfaceStampTests
{
    private readonly Definition _game = Definition.Board();

    [Fact]
    public void ACommandSentIsStampedWithTheSendersSurfaceAndRunsThere()
    {
        var (host, client) = LocalSession.Create(_game.Create(1));
        client.Send(new Write("hello"));
        Assert.Equal(0, Assert.Single(client.InFlight).Surface);
        host.Advance();
        Assert.Empty(Assert.Single(client.Update()).Refused);
        Assert.Equal(["0:hello"], _game.Module<BoardModule>(client.World).Notes);
    }
}
