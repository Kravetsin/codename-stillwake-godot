using Stillwake.Sim;

namespace Stillwake.Net.Tests;

/// <summary>The first check at which clients differed: their hashes then, and what the host's resyncs reported.</summary>
public sealed record Found(int Tick, IReadOnlyList<ulong> Hashes, IReadOnlyList<DesyncReport> Reports);

public sealed record DesyncRun(int Ticks, int Commands, int Refused, Found? Desync);

/// <summary>
/// The desync fuzz (packages/net/src/testing/desync.ts): a host and its clients in one process, random
/// commands from every module's fuzz generator sent by random clients, clients updating out of step
/// and a few ticks at a time as over a real network, and their hashes compared at every check. A
/// resync counts as a desync, so the host mending a client cannot hide a determinism bug.
/// </summary>
public static class Desync
{
    /// <param name="worldEvery">One command in this many is the world's own (0076), a surface opened or a hero moved; none at 0.</param>
    public static DesyncRun Fuzz(
        Definition definition,
        uint seed,
        int ticks = 600,
        int clients = 3,
        int checkEvery = 20,
        Layout? layout = null,
        int? joinAt = null,
        Action<int, IReadOnlyList<Client>>? tamper = null,
        int worldEvery = 40)
    {
        var made = definition.Make();
        int fuzzable = made.Count(m => m.Fuzzes);
        var unfuzzed = made.Where(m => m.HasCommands && !m.Fuzzes).Select(m => $"'{m.Id}'").ToList();
        if (unfuzzed.Count > 0)
            throw new InvalidOperationException(
                $"Modules with commands but no fuzz generator: {string.Join(", ", unfuzzed)}. Override Fuzz.");

        var peers = new List<Client>();
        int refused = 0, commands = 0;
        // Refusals are counted on the first client only; every client sees the same ones.
        void Update(Client client, int limit = int.MaxValue)
        {
            var reports = client.Update(limit);
            if (client == peers[0]) refused += reports.Sum(r => r.Refused.Count);
        }

        var host = new Host(0, new Source("fuzz", () =>
        {
            Update(peers[0]);
            return peers[0].World.Save();
        }));
        for (int i = 0; i < clients; i++)
        {
            var (toClient, toHost) = Loopback.Create<ToClient, ToHost>();
            int id = host.Connect(toClient);
            peers.Add(new Client(definition.Create(seed, layout), toHost, id, definition.Load));
        }
        var rng = new Rng(new RngState { S = seed ^ 0x5eed });
        // How many ticks each update may run, drawn apart so the command stream stays each seed's own.
        var pace = new Rng(new RngState { S = seed ^ 0x9ace });

        Found? Check()
        {
            foreach (var client in peers) Update(client);
            var hashes = peers.Select(c => c.World.Hash()).ToList();
            // The hashes just sent may have earned a resync: taken now, running no tick.
            foreach (var client in peers) Update(client, 0);
            // The host resyncs a drifted peer on its own; a resync is a desync all the same.
            var reports = peers.SelectMany(c => c.TakeDesyncs()).ToList();
            return hashes.Distinct().Count() == 1 && reports.Count == 0 ? null : new Found(peers[0].World.Tick, hashes, reports);
        }

        for (int tick = 0; tick < ticks; tick++)
        {
            for (uint n = fuzzable > 0 ? rng.Below(4) : 0; n > 0; n--)
            {
                var sender = peers[(int)rng.Below((uint)peers.Count)];
                // Now and then the world's own: a surface opened, a hero moved between them (0076).
                var modules = definition.ModulesOf(sender.World, sender.Id).Where(m => m.Fuzzes).ToList();
                var pick = worldEvery > 0 && rng.Below((uint)worldEvery) == 0 ? sender.World.Fuzz(rng, sender.Id) : modules[(int)rng.Below((uint)modules.Count)].Fuzz(rng, sender.Id);
                if (pick == null) continue;
                sender.Send(pick with { From = 0 });
                commands += 1;
            }
            // After this tick's commands: the newcomer joins with commands waiting in the host.
            if (tick == joinAt)
            {
                var (toClient, toHost) = Loopback.Create<ToClient, ToHost>();
                host.Admit(toClient);
                Client.Join(definition.Load, toHost, "fuzz", peers.Add, why => throw why);
            }
            host.Advance();
            // As a peer playing ticks out at its own pace: a few at a time, none, or all that wait.
            foreach (var client in peers)
            {
                if (rng.Below(3) == 0) continue;
                int limit = (int)pace.Below(5);
                Update(client, limit == 4 ? int.MaxValue : limit);
            }
            tamper?.Invoke(tick, peers);

            if ((tick + 1) % checkEvery == 0 && Check() is Found found)
                return new DesyncRun(tick + 1, commands, refused, found);
        }
        return new DesyncRun(ticks, commands, refused, Check());
    }

    private sealed class Source(string build, Func<string> save) : IWorldSource
    {
        public string Build => build;
        public string Save() => save();
    }
}
