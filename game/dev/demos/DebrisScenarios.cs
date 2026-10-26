using System;
using System.Linq;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Debris;
using Stillwake.Net;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

// Out among the debris: the hero far out of the wreck, played by script unless --manual.

[Dev("--debris", "the wreck with debris about, a crate caught with --autopilot (DebrisDemo)")]
public sealed class DebrisScenario : DevScenario
{
    public override World Seed(World world, DevSeed seed) => DebrisDemo.Seed(world, seed.Modules);

    public override bool Hosted => true;

    public override void Attach(DevContext c) => c.Add(new DebrisDemo(c.Debris, c.Ui));
}

[Dev("--fly", "the hero flown far out (FarDemo)")]
public sealed class FlyScenario : DevScenario
{
    public override Layout? Map => Registry.Wreck;

    public override World Seed(World world, DevSeed seed)
    {
        if (Args.Arg("--bow") is { } bow) FarDemo.Bow = int.Parse(bow);
        return FarDemo.Seed(world, seed.M, seed.Modules, int.Parse(Args.Value("--far") ?? "0"));
    }

    public override HeroAutopilot? Script => Args.Has("--manual") ? null : HeroAutopilot.Fly();

    public override Func<Registry, int>? Start => FarDemo.StartOf;

    public override void Attach(DevContext c) => c.Add(new FarDemo(c.Debris, c.World));
}

[Dev("--asteroid", "two asteroids drilled and chopped out in the band (AsteroidDemo)")]
public sealed class AsteroidScenario : DevScenario
{
    public override Layout? Map => Registry.Wreck;

    public override World Seed(World world, DevSeed seed) => AsteroidDemo.Seed(world, seed.M, seed.Modules, ArcDrill);

    public override Func<Registry, int>? Start => FarDemo.StartOf;

    public override void Attach(DevContext c)
    {
        if (!Args.Has("--manual")) c.Add(new AsteroidDemo(c.Debris, c.Ui, c.World));
    }
}

[Dev("--sortie", "the hero far out among one of each derelict and asteroid, an engine block drilled (SortieDemo)")]
public sealed class SortieScenario : DevScenario
{
    public override Layout? Map => Registry.Wreck;

    public override World Seed(World world, DevSeed seed) => SortieDemo.Seed(world, seed.M, seed.Modules);

    public override Func<Registry, int>? Start => SortieDemo.StartOf;

    public override void Attach(DevContext c)
    {
        if (!Args.Has("--manual")) c.Add(new SortieDemo(c.Debris, c.Ui, c.World));
    }
}

[Dev("--sentry", "the sortie's derelicts, a sentry let out on the engine block beside the hero: watching, alerted, pouncing (0102); =fight: its line reeled and cut, struck once, its wreck hewn apart under it; =crawl: the hero jets off out of its notice and it crawls its perch",
    Shape = DevShape.Either)]
public sealed class SentryScenario : DevScenario
{
    private bool _let;
    private string _was = "";
    private int? _sentry;
    private bool _breaking;

    /// <summary>The fight's ticks: the hero reels the sentry's wreck (it cuts the line), strikes it once, then hews its wreck apart under it.</summary>
    private const int ReelAt = 50, StrikeAt = 92, BreakFrom = 100;

    public override Layout? Map => Registry.Wreck;

    public override World Seed(World world, DevSeed seed) => SortieDemo.Seed(world, seed.M, seed.Modules);

    public override Func<Registry, int>? Start => SortieDemo.StartOf;

    public override HeroAutopilot? Script => Args.Given("--sentry") == "crawl" ? HeroAutopilot.Away() : null;

    public override void Frame(Client client, Registry m, Action<string> log)
    {
        if (!_let && client.World.Tick >= 5 && m.Debris.Pieces.FirstOrDefault(p => p.Item == "engineBlock") is { } block)
        {
            _let = true;
            client.Send(new Sim.Creatures.Release(Content.Creatures.Sentry.Id, Sim.Debris.DebrisModule.CellOf(block.X, block.Y, m.Grid.Width)));
        }
        if (client.World.Tick >= 8 && m.Creatures.Creatures.Count > 0) _sentry ??= m.Creatures.Creatures.Max(c => c.Id);
        if (Args.Given("--sentry") == "fight" && m.Creatures.Creatures.FirstOrDefault(c => c.Id == _sentry) is { } sentry
            && m.Creatures.ClingsTo(sentry) is int piece && m.Debris.PieceById(piece) is { } wreck)
        {
            int tick = client.World.Tick;
            // Reeled toward the hero, the reel sent again as the harpoon follows its hero, till the sentry has sawn the line through.
            if (tick >= ReelAt && tick < ReelAt + 30 && (tick - ReelAt) % 4 == 0 && Sim.Creatures.CreaturesModule.LastCut(sentry) == 0
                && m.Crew.HeroOf(client.Id) is { } hero)
                client.Send(new Sim.Debris.Reel(piece, hero.Cell % m.Grid.Width * 1000 + 500, hero.Cell / m.Grid.Width * 1000 + 500));
            if (tick == StrikeAt) client.Send(new Sim.Creatures.Strike(sentry.Id, Content.Weapons.Axe.Item));
            // Its wreck hewn apart under it, once it is clamped on again: it goes with its wreck.
            _breaking |= tick >= BreakFrom && Sim.Creatures.CreaturesModule.Clamped(sentry);
            if (_breaking)
                for (int i = 0; i < 30; i++) client.Send(new Sim.Debris.HewWrecks(Content.Weapons.Axe.Item, wreck.X, wreck.Y, 0));
        }
        foreach (var c in m.Creatures.Creatures)
        {
            var spec = Content.Creatures.Get(c.Species);
            string now = c.Act is { } act ? $"act {act.Phase}" : $"{spec.Brain[c.Drive].GetType().Name} {c.Step}";
            if (now == _was) continue;
            log($"sentry: tick {client.World.Tick}, {now} at ({c.X}, {c.Y}), velocity ({c.VX}, {c.VY})");
            _was = now;
        }
    }
}
