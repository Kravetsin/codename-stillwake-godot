using Stillwake.Sim;

namespace Stillwake.Net;

/// <summary>
/// A host and its player's client in the same process, joined by a loopback
/// (packages/net/src/local.ts): single-player, and the hosting side of co-op. A peer joining later is
/// welcomed with this client's world, brought up to the host's tick first, so <c>build</c> must name
/// the build that runs it.
/// </summary>
public static class LocalSession
{
    public static (Host Host, Client Client) Create(World world, string build = "")
    {
        var source = new OwnWorld(build);
        var host = new Host(world.Tick, source);
        var (toClient, toHost) = Loopback.Create<ToClient, ToHost>();
        int id = host.Connect(toClient);
        var client = new Client(world, toHost, id);
        source.Bind(host, client);
        return (host, client);
    }

    private sealed class OwnWorld(string build) : IWorldSource
    {
        private Host _host = null!;
        private Client _client = null!;

        public string Build => build;

        public void Bind(Host host, Client client) => (_host, _client) = (host, client);

        public string Save()
        {
            _client.Update();
            // Our bug if it is not: a joiner or a resynced peer would start on another tick.
            if (_client.World.Tick != _host.Tick)
                throw new InvalidOperationException($"Saving tick {_client.World.Tick} for a host on tick {_host.Tick}.");
            return _client.World.Save();
        }
    }
}
