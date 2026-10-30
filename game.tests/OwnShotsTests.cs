using Stillwake.Game.Features.Creatures;
using Stillwake.Sim.Creatures;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Which creatures this player's own shots hit (2026-10-29-kills-crumble): heard as its own blows, from afar.</summary>
public class OwnShotsTests
{
    private static ShotHit Hit(Ref by, ShotTarget what, int id) => new(1, by, "nail", "nailer", 0, (0, 0), what, 3, false, id);

    [Fact]
    public void OnlyTheCreaturesThisPlayersHeroShotAreItsOwn()
    {
        var into = new HashSet<int> { 99 };
        OwnShots.Of([
            Hit(Ref.Hero(2), ShotTarget.Creature, 5),
            Hit(Ref.Hero(1), ShotTarget.Creature, 6),
            Hit(Ref.Hero(2), ShotTarget.Wreck, 7),
            Hit(Ref.Hero(2), ShotTarget.Creature, 8),
        ], peer: 2, into);
        Assert.Equal([5, 8], into.Order());
    }
}
