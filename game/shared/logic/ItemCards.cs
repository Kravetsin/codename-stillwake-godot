using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Research;
using static Stillwake.Game.I18n.En;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Shared;

/// <summary>
/// An item's card, from content: its name and kind; a tool's tiles and effects — a weapon's (the fire axe's,
/// the drill's) damage a second, damage, block damage and seconds between hits, read from the sim's own fold (<see cref="ToolStats"/>),
/// and with modules fitted (<see cref="Fitted"/>, 0098) each that differs marked by how; the harpoon's range, reload and reel speed; a
/// gun's damage and block damage with its first ammo, its reload and what it keeps through what it pierces, and its ammo's share
/// of those (2026-10-27-shots-pierce); a worn item's one number, the helmet lamp's reach. Its words from the i18n table.
/// </summary>
public static class ItemCards
{
    /// <summary>Whether <paramref name="item"/> is a tool: a weapon (the fire axe, the drill), a gun (the nailer) or the harpoon.</summary>
    public static bool IsTool(string item) => Spec.Weapons.Of(item) != null || Spec.Shots.GunOf(item) != null || item == Spec.Harpoon.Item;

    /// <summary>Whether <paramref name="item"/> is some gun's ammo (the nail): its card has tiles too, though it is no tool.</summary>
    public static bool IsAmmo(string item) => Spec.Shots.Guns.Any(gun => gun.Ammo.Contains(item));

    /// <summary>
    /// Whether <paramref name="item"/> shows the violet "?" and its card the band: it needs analysis, being in
    /// some cost (<see cref="Analysable"/>), and is not among <paramref name="analysed"/>; never in creative mode.
    /// </summary>
    public static bool Unknown(string item, bool creative, IEnumerable<string> analysed) =>
        !creative && Analysable.NeedsAnalysis(item) && !analysed.Contains(item);

    /// <summary>The card of <paramref name="item"/>; <paramref name="unknown"/> while not analysed.</summary>
    public static ItemCard Of(string item, bool unknown) => Card(item, unknown) with { Draw = Spec.Items.Get(item).Draw };

    private static ItemCard Card(string item, bool unknown)
    {
        string name = T($"item.{item}"), kind = T($"item.{item}.kind");
        // Anything used by a key of its own shows the key, and no words on what it does: the player finds out (0047).
        var key = Table.ContainsKey($"item.{item}.key") ? new CardKey(T($"item.{item}.key")) : null;
        if (IsAmmo(item)) return new ItemCard(item, name, kind, AmmoTiles(item), [], key, unknown);
        if (!IsTool(item))
        {
            var card = new ItemCard(item, name, kind, null, [], key, unknown);
            if (Spec.Modules.TryGet(item, out var module)) card = card with { Tech = TechOf(module) };
            return Spec.Items.Get(item).Equip == null ? card : Worn(card);
        }
        if (item == Spec.Harpoon.Item) return new ItemCard(item, name, kind, HarpoonTiles(), [], key, unknown);
        if (Spec.Shots.GunOf(item) is { } gun) return new ItemCard(item, name, kind, GunTiles(gun), [], key, unknown);
        var weapon = Spec.Weapons.Of(item)!;
        return new ItemCard(
            item, name, kind, WeaponTiles(item, []),
            weapon.Effects.Select(e => new CardEffect(T($"effect.{e.Id}"), T($"effect.{e.Id}.about"), e.Colour, e.Ground)).ToList(),
            key, unknown);
    }

    /// <summary>
    /// <paramref name="card"/>, a tool's, with <paramref name="modules"/> fitted into it (0098): its tiles and draw the fold's for
    /// them (<see cref="ToolStats"/>), each that differs from the plain tool's marked (<see cref="CardBoost"/>), and the modules
    /// named for its picture and its strips. The card itself for none, or for an item that is no tool.
    /// </summary>
    public static ItemCard Fitted(ItemCard card, IReadOnlyList<Spec.ModuleSpec> modules)
    {
        if (modules.Count == 0 || !card.Tool) return card;
        var fitted = card with { Fitted = [.. modules.Select(m => m.Item)] };
        if (Spec.Weapons.Of(card.Item) != null) fitted = fitted with { Tiles = WeaponTiles(card.Item, modules) };
        if (card.Draw == null) return fitted;
        var (plain, stats) = (ToolStats.Of(card.Item, []), ToolStats.Of(card.Item, modules));
        return fitted with { Draw = stats.Draw, DrawBoost = BoostOf(stats.Draw, plain.Draw, modules, m => m.Adds(card.Item, Spec.Stat.Draw)) };
    }

    /// <summary>
    /// A weapon's tiles, <paramref name="modules"/> fitted (none for the plain one): its damage a second, a hit's damage off a body
    /// and off a block, and the seconds between hits, all from <see cref="ToolStats"/>, each that differs from the plain weapon's marked.
    /// </summary>
    private static List<CardTile> WeaponTiles(string item, IReadOnlyList<Spec.ModuleSpec> modules)
    {
        var (plain, stats) = (ToolStats.Of(item, []), ToolStats.Of(item, modules));
        var (block, plainBlock) = (ToolStats.BlockDamageOf(item, modules), ToolStats.BlockDamageOf(item, []));
        double every = (double)stats.Pace / World.TicksPerSecond, was = (double)plain.Pace / World.TicksPerSecond;
        int Damage(Spec.ModuleSpec m) => m.Adds(item, Spec.Stat.Damage);
        return
        [
            new(Number(stats.Damage / every), T("card.dps"), BoostOf(stats.Damage / every, plain.Damage / was, modules, Damage)),
            new(Number(stats.Damage), T("card.damage"), BoostOf(stats.Damage, plain.Damage, modules, Damage)),
            new(Number(block), T("card.block"), BoostOf(block, plainBlock, modules, Damage)),
            new(T("card.seconds", ("n", Number(every))), T("card.hit"), BoostOf(every, was, modules, _ => 0)),
        ];
    }

    /// <summary>
    /// A gun's tiles: a hit's damage off a body and off a block with the first of its ammo, the seconds between shots, and the share
    /// of its damage a shot keeps through each thing it pierces (<see cref="Spec.Gun.PierceKeep"/>); and for a gun that takes the
    /// belt's charge, the power-ticks a shot (2026-10-27-the-railgun).
    /// </summary>
    private static List<CardTile> GunTiles(Spec.Gun gun)
    {
        List<CardTile> tiles =
        [
            new(Number(Spec.Shots.DamageOf(gun.Item, gun.Ammo[0])), T("card.damage")),
            new(Number(Spec.Shots.BlockDamageOf(gun.Item, gun.Ammo[0])), T("card.block")),
            new(T("card.seconds", ("n", Number((double)gun.Pace / World.TicksPerSecond))), T("card.reload")),
            new(T("card.percent", ("n", Number(gun.PierceKeep / 10.0))), T("card.pierce")),
        ];
        if (gun.Energy > 0) tiles.Add(new(Number(gun.Energy), T("card.energy")));
        return tiles;
    }

    /// <summary>Ammo's tiles: what it adds to its gun's damage off a body and off a block, "+2".</summary>
    private static List<CardTile> AmmoTiles(string item)
    {
        var projectile = Spec.Shots.Get(item);
        return [new($"+{projectile.Damage}", T("card.damage")), new($"+{projectile.BlockDamage}", T("card.block"))];
    }

    /// <summary>
    /// How <paramref name="now"/> differs from the plain tool's <paramref name="was"/>: up or down, by how many times, in the tech
    /// of the first of <paramref name="modules"/> that <paramref name="adds"/> to it, else the first's; null where they are alike.
    /// </summary>
    private static CardBoost? BoostOf(double now, double was, IReadOnlyList<Spec.ModuleSpec> modules, Func<Spec.ModuleSpec, int> adds)
    {
        if (now == was || was == 0 || modules.Count == 0) return null;
        var by = modules.FirstOrDefault(m => adds(m) != 0) ?? modules[0];
        return new CardBoost(by.Tech, now > was, T("card.many", ("n", Number(now / was))));
    }

    /// <summary>The arc's violet-white on a deep violet: a module's socket and strip, as its arcs look (0086).</summary>
    public const string ModuleColour = "d9c8ff", ModuleGround = "221a3d";

    /// <summary>
    /// A module's strip at the bottom of its card (0086): its technology's icon and what it does, a line for each group of its
    /// hosts (0100), put together from its effects, "×2 speed · ×2 power", "×5 charging · ×5 power"; a line said twice, once.
    /// Never where it goes, for modules will be many and so will what takes them.
    /// </summary>
    private static CardTech TechOf(Spec.ModuleSpec module) =>
        new(module.Tech, string.Join("\n", module.On.Select(LineOf).Where(line => line.Length > 0).Distinct()));

    /// <summary>
    /// What a module does in the hosts of <paramref name="on"/>, its trades in their order as times, "×2 speed · ×2 power"; at a
    /// charging station the draw is what it charges (0053), so a draw trade there says both, "×5 charging · ×5 power".
    /// </summary>
    private static string LineOf(Spec.HostEffects on)
    {
        bool charges = on.Hosts.Any(host => Spec.Machines.TryGet(host, out var machine) && machine.Charges);
        var words = new List<string>();
        foreach (var trade in on.Effects.OfType<Spec.Trade>())
        {
            string times = Number((1000 + trade.Add) / 1000.0);
            if (trade.Stat == Spec.Stat.Draw && charges) words.Add(T("card.trade.charging", ("n", times)));
            words.Add(trade.Stat switch
            {
                Spec.Stat.Speed => T("card.trade.speed", ("n", times)),
                Spec.Stat.Draw => T("card.trade.power", ("n", times)),
                Spec.Stat.Damage => T("card.trade.damage", ("n", times)),
                // Heat is whole levels, not thousandths (0086).
                _ => T("card.trade.heat", ("n", trade.Add)),
            });
        }
        return string.Join(" · ", words);
    }

    /// <summary>
    /// A worn item's card: the helmet lamp's reach in cells and its beam across, in degrees, from the light it gives
    /// (<see cref="Light.HeadLampReach"/>); the cape's share off enemies' blows and off the cold, in percent; a battery's number
    /// is its charge, set per stack; the jetpack has none.
    /// </summary>
    private static ItemCard Worn(ItemCard card)
    {
        if (card.Item == Spec.Cape.Item)
            return card with
            {
                Worn = true,
                Headline = new CardTile((Spec.Cape.Guard / 10).ToString(CultureInfo.InvariantCulture), T("card.guard")),
                Detail = T("card.warmth", ("n", Spec.Cape.Warmth / 10)),
            };
        if (card.Item != Spec.HelmetLamp.Item) return card with { Worn = true };
        int beam = (int)Math.Round(Light.HeadLampHalfAngle * 2 * 180 / Math.PI);
        return card with
        {
            Worn = true,
            Headline = new CardTile(Light.HeadLampReach.ToString(CultureInfo.InvariantCulture), T("card.reach")),
            Detail = T("card.beam", ("n", beam)),
        };
    }

    /// <summary>
    /// What fed <paramref name="station"/> makes of one <paramref name="item"/>: its yields, and the time one takes there,
    /// <c>SmeltTicks</c> at the station's speed, in seconds or past a minute in minutes, one decimal at most; null for an
    /// item it makes nothing of.
    /// </summary>
    public static CardOutput? OutputAt(string item, Spec.Station station)
    {
        var spec = Spec.Items.Get(item);
        if (spec.Yields is not { Count: > 0 } yields || spec.SmeltTicks is not int ticks) return null;
        // Speed is in thousandths of full speed, as a craft's time at a station is worked out.
        double seconds = (double)ticks * 1000 / station.Speed / World.TicksPerSecond;
        string time = seconds < 60 ? T("card.seconds", ("n", Number(seconds))) : T("card.minutes", ("n", Number(seconds / 60)));
        return new CardOutput(yields, time);
    }

    /// <summary>The harpoon's tiles: how far its rod flies, in cells; the seconds till it fires again; the cells a second it reels.</summary>
    private static List<CardTile> HarpoonTiles() =>
    [
        new(Number(Spec.Harpoon.Range / 1000.0), T("card.range")),
        new(T("card.seconds", ("n", Number((double)Spec.Harpoon.CooldownTicks / World.TicksPerSecond))), T("card.reload")),
        new(T("card.perSecond", ("n", Number(Spec.Harpoon.ReelSpeed * World.TicksPerSecond / 1000.0))), T("card.reel")),
    ];

    /// <summary>A craft's name, its item; a blueprint's, its tool and material: "Wall: iron".</summary>
    public static string NameOf(string id, Placement? placement) => placement is { } p
        ? p.Material is { } m ? $"{T($"tool.{p.Kind}")}: {T($"material.{m}")}" : T($"tool.{p.Kind}")
        : T($"item.{id}");

    /// <summary><paramref name="n"/> with one decimal at most, half rounded up: 4, 5.3, 0.5.</summary>
    public static string Number(double n) =>
        Math.Round(n, 1, MidpointRounding.AwayFromZero).ToString("0.#", CultureInfo.InvariantCulture);
}
