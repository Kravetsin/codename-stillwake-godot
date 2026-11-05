using System.Text.Json;
using System.Text.Json.Nodes;
using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Stillwake.Sim.Creatures;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>
/// What a stroke's blade strikes of creatures (0102, melee-edge): their bodies and a margin, wherever the hero stands, never
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

    private static readonly Func<Sim.Debris.Piece, double, double, bool> Clear = (_, _, _) => false;

    /// <summary>The creatures a stroke of <paramref name="tool"/>, the axe unless said, by <paramref name="hero"/> toward <paramref name="at"/> sweeps through (melee-edge).</summary>
    private static List<Creature> Swept(TestWorld w, Hero hero, Point at, string? tool = null) =>
        [.. ToolEdges.Swept(w.M, Aiming.StrokeEdges(hero, at, tool ?? Spec.Weapons.Axe.Item), outside: false, 0, Clear).Creatures];

    private static List<int> Struck(TestWorld w, Hero hero, Point at, string? tool = null) => [.. Swept(w, hero, at, tool).Select(c => c.Id)];

    [Fact]
    public void TheAxeStrikesALoperItsBladeMeetsOnTheGroundAndNotOneBehindTheHerosBack()
    {
        var hero = HeroOn(6, 10);
        var shoulder = ShoulderOf(hero);
        var ahead = shoulder with { X = shoulder.X + 3_000, Y = shoulder.Y + 1_000 };
        // A loper beside the hero's feet ahead, its body a cell and a half off: struck, swung at it.
        var w = With((hero.X + 1_500 + Unit, 13 * Unit));
        Assert.Equal([1], Struck(w, hero, ahead));
        Assert.Equal([new Strike(1, Spec.Weapons.Axe.Item)], Aiming.StrikesOf(w.M, 0, Swept(w, hero, ahead), Spec.Weapons.Axe.Item));
        // Six cells off: the blade cuts air.
        Assert.Empty(Struck(With((hero.X + 6_000, 13 * Unit)), hero, ahead));
        // Wholly behind its back: never, though swung the other way it is struck.
        var behind = With((hero.X - 1_300, 13 * Unit));
        Assert.Empty(Struck(behind, hero, ahead));
        Assert.Equal([1], Struck(behind, hero with { Facing = -1 }, shoulder with { X = shoulder.X - 3_000, Y = shoulder.Y + 1_000 }));
    }

    [Fact]
    public void TheSpearsPointStrikesALoperAtFullStretchAndTheSimRefusesAFlatDrill()
    {
        var hero = HeroOn(6, 10);
        var at = new Point(hero.X + 4_000, 12 * Unit);
        var w = With((hero.X + 2_500 + Unit, 13 * Unit));
        string spear = Spec.Weapons.Spear.Item;
        Assert.Equal([1], Struck(w, hero, at, spear));
        // Ten cells off its point touches nothing.
        Assert.Empty(Struck(With((hero.X + 10_000, 13 * Unit)), hero, at, spear));
        // No battery on the belt: an edge touches it, but no strike goes, as the sim would refuse it (items.flat).
        Assert.Empty(Aiming.StrikesOf(w.M, 0, Swept(w, hero, at, spear), Spec.Weapons.Drill.Item));
    }
}
