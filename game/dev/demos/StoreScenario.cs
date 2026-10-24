using System.Collections.Generic;
using Stillwake.Game.Features.Power;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

[Dev("--store", "a capacitor bank filling from a panel, then emptying into an arc smelter (StoreDemo)")]
public sealed class StoreScenario : DevScenario
{
    public override Layout? Map => StoreWorld.Map;

    public override World? Create(IReadOnlyList<Module> modules) => StoreWorld.Create(modules);

    public override void Attach(DevContext c) => c.Add(new StoreDemo(c.World));
}
