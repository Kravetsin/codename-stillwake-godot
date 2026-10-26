using System.Text.Json;
using System.Text.Json.Nodes;
using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Stillwake.Sim.Creatures;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>
/// What the axe's blade and the drill's bit strike of creatures (0102): their bodies and a margin, wherever the hero stands, never
/// one wholly behind its back; and only strikes the sim would take.
/// </summary>
public class CreatureAimTests
{
    private const int Unit = Hero.Unit;

    /// <summary>A hero whose body is centred on the middle of cell (x, y).</summary>
    private static Hero HeroOn(int x, int y) => new(x * Unit + Unit / 2, y * Unit + Unit / 2 + Hero.MiddleUp, 0, 0, false, 1);

    private static Point ShoulderOf(Hero hero) => new(hero.X, hero.Y - (int)(Rig.Shoulder / Rig.Cell * Unit));

    /// <summary>A room of open cells with lopers standing with their feet's middles at the points given, ids from 1: put in through a save.</summary>
    private static TestWorld With(params (int X, int Y)[] lopers)
    {
        var w = new TestWorld(Layout.Parse([new string('.', 24), .. Enumerable.Repeat(new string('.', 24), 12), new string('#', 24)]));
        return w.Edit(root =>
        {
            var creatures = root["slices"]!["creatures"]!;
            var all = creatures["creatures"]!.AsArray();
            for (int i = 0; i < lopers.Length; i++)
            {
                var c = JsonSerializer.SerializeToNode(new Creature(), w.World.Json)!.AsObject();
                (c["id"], c["species"], c["x"], c["y"]) = (i + 1, Spec.Creatures.Loper.Id, lopers[i].X, lopers[i].Y);
                all.Add(c);
            }
            creatures["nextId"] = lopers.Length + 1;
        });
    }

    private static List<int> Struck(TestWorld w, Hero hero, Point at) => Aiming.StruckCreatures(w.M, hero, at).Select(c => c.Id).ToList();

    [Fact]
    public void TheAxeStrikesALoperItsBladeMeetsOnTheGroundAndNotOneBehindTheHerosBack()
    {
        var hero = HeroOn(6, 10);
        var shoulder = ShoulderOf(hero);
        var ahead = shoulder with { X = shoulder.X + 3_000, Y = shoulder.Y + 1_000 };
        // A loper beside the hero's feet ahead, its body a cell and a half off: struck, swung at it.
        var w = With((hero.X + 1_500 + Unit, 13 * Unit));
        Assert.Equal([1], Struck(w, hero, ahead));
        Assert.Equal([new Strike(1, Spec.Weapons.Axe.Item)], Aiming.StrikesOf(w.M, 0, Aiming.StruckCreatures(w.M, hero, ahead), Spec.Weapons.Axe.Item));
        // Six cells off: the blade cuts air.
        Assert.Empty(Struck(With((hero.X + 6_000, 13 * Unit)), hero, ahead));
        // Wholly behind its back: never, though swung the other way it is struck.
        var behind = With((hero.X - 1_300, 13 * Unit));
        Assert.Empty(Struck(behind, hero, ahead));
        Assert.Equal([1], Struck(behind, hero with { Facing = -1 }, shoulder with { X = shoulder.X - 3_000, Y = shoulder.Y + 1_000 }));
    }

    [Fact]
    public void TheDrillsBitStrikesALoperItTouchesWhereverTheHeroStandsAndTheSimRefusesAFlatOne()
    {
        var hero = HeroOn(6, 10);
        var at = new Point(hero.X + 2_000, 12 * Unit);
        var w = With((hero.X + 1_200 + Unit, 13 * Unit));
        Assert.Equal([1], Aiming.TouchedCreatures(w.M, hero, at).Select(c => c.Id));
        Assert.Empty(Aiming.TouchedCreatures(With((hero.X + 5_000, 13 * Unit)).M, hero, at));
        // No battery on the belt: the bit touches it, but no strike goes, as the sim would refuse it (items.flat).
        Assert.Empty(Aiming.StrikesOf(w.M, 0, Aiming.TouchedCreatures(w.M, hero, at), Spec.Weapons.Drill.Item));
    }
}
