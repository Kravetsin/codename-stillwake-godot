using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Stillwake.Game.I18n;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Research;
using static Stillwake.Game.I18n.En;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Shared;

/// <summary>
/// An item's card, from content: its name and kind; a tool's numbers, each a row with its sign and group (2026-11-07-tool-card-rows) — a weapon's (the fire axe's,
/// the drill's) damage, block damage and seconds between hits, read from the sim's own fold (<see cref="ToolStats"/>),
/// and with modules fitted (<see cref="Fitted"/>, 0098) each that differs marked by how; the harpoon's range, reload and reel speed; a
/// gun's damage and block damage with its first ammo, its reload and what it keeps through what it pierces, and its ammo's share
/// of those (2026-10-27-shots-pierce); food's and worn gear's rows, what each guarantees (2026-11-14-food-and-gear-cards). Its words
/// from the i18n table.
/// </summary>
public static class ItemCards
{
    /// <summary>Whether <paramref name="item"/> is a tool: a weapon (the fire axe, the drill), a gun (the nailer) or the harpoon.</summary>
    public static bool IsTool(string item) => Spec.Weapons.Of(item) != null || Spec.Shots.GunOf(item) != null || item == Spec.Harpoon.Item || item == Spec.Grapple.Item;

    /// <summary>Whether <paramref name="item"/> is some gun's ammo (the nail): its card has tiles too, though it is no tool.</summary>
    public static bool IsAmmo(string item) => Spec.Shots.Guns.Any(gun => gun.Ammo.Contains(item));

    /// <summary>The card of <paramref name="item"/>.</summary>
    public static ItemCard Of(string item) => Card(item) with { Draw = Spec.Items.Get(item).Draw };

    private static ItemCard Card(string item)
    {
        int? price = Spec.Market.PriceOf(item);
        string name = T($"item.{item}"), kind = T($"item.{item}.kind");
        // Anything used by a key of its own shows the key, and no words on what it does: the player finds out (0047).
        // Food says it is eaten; worn gear what its key does where that is not plain, the jetpack's "in space".
        string words = Spec.Foods.Of(item) != null ? T("card.eat") : Table.ContainsKey($"item.{item}.use") ? T($"item.{item}.use") : "";
        var key = Table.ContainsKey($"item.{item}.key") ? new CardKey(T($"item.{item}.key"), words) : null;
        // A thrown charge is its own gun and ammo: its card is its blast, not what it adds to a gun.
        if (Spec.Shots.GunOf(item) is { } thrown && Spec.Shots.Get(thrown.Ammo[0]).Bursts is { } blast)
            return new ItemCard(item, name, kind, BlastTiles(thrown, blast), key, price);
        if (IsAmmo(item)) return new ItemCard(item, name, kind, AmmoTiles(item), key, price);
        if (!IsTool(item))
        {
            var card = new ItemCard(item, name, kind, null, key, price);
            if (Table.ContainsKey($"item.{item}.about")) card = card with { About = T($"item.{item}.about") };
            if (Spec.Modules.TryGet(item, out var module)) card = card with { Tech = TechOf(module) };
            if (Spec.Foods.Of(item) is { } food) return card with { Food = true, Rows = FoodRows(food, null) };
            return Spec.Items.Get(item).Equip == null ? card : Worn(card);
        }
        if (item == Spec.Harpoon.Item) return new ItemCard(item, name, kind, HarpoonTiles(), key, price);
        if (item == Spec.Grapple.Item) return new ItemCard(item, name, kind, GrappleTiles(), key, price);
        if (Spec.Shots.GunOf(item) is { } gun) return new ItemCard(item, name, kind, GunTiles(gun), key, price);
        return new ItemCard(item, name, kind, WeaponTiles(item, []), key, price);
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
    /// A weapon's numbers, <paramref name="modules"/> fitted (none for the plain one), as rows (2026-11-07-tool-card-rows): a hit's
    /// damage off a wreck and off rock, and the seconds between hits, from <see cref="ToolStats"/>, each that differs from the plain
    /// weapon's marked; its swing's width in degrees and its reach in cells; what its hits do besides, the cutter's loot. Its draw
    /// is the card's own (<see cref="ItemCard.Draw"/>). No damage a second: it hid what sets the tools apart.
    /// </summary>
    private static List<CardTile> WeaponTiles(string item, IReadOnlyList<Spec.ModuleSpec> modules)
    {
        var weapon = Spec.Weapons.Of(item)!;
        var (plain, stats) = (ToolStats.Of(item, []), ToolStats.Of(item, modules));
        var (block, plainBlock) = (ToolStats.BlockDamageOf(item, modules), ToolStats.BlockDamageOf(item, []));
        double every = (double)stats.Pace / World.TicksPerSecond, was = (double)plain.Pace / World.TicksPerSecond;
        int Damage(Spec.ModuleSpec m) => m.Adds(item, Spec.Stat.Damage);
        List<CardTile> tiles =
        [
            Hits(Number(stats.Damage), BoostOf(stats.Damage, plain.Damage, modules, Damage), CardGroup.Hit),
            Rock(block, BoostOf(block, plainBlock, modules, Damage), CardGroup.Hit),
            new CardTile(T("card.seconds", ("n", Number(every))), T("card.hit"), BoostOf(every, was, modules, _ => 0)) { Sign = CardSign.Pace },
            // A turn is 256 of the arc's units (the fan of rays).
            new CardTile(T("card.degrees", ("n", Math.Round(weapon.Arc * 360 / 256.0, MidpointRounding.AwayFromZero))), T("card.arc"))
            { Sign = CardSign.Arc, Group = CardGroup.Reach },
            new CardTile(T("card.cells", ("n", Number(weapon.Range / 1000.0))), T("card.range")) { Sign = CardSign.Reach, Group = CardGroup.Reach },
        ];
        // What of a broken cell's surplus goes on to the one behind (unified-damage), shown as a gun's pierce.
        if (weapon.Carries > 0)
            tiles.Insert(3, new CardTile(T("card.percent", ("n", Number(weapon.Carries))), T("card.pierce")) { Sign = CardSign.Pierce, Group = CardGroup.Hit });
        if (Spec.Weapons.Salvages(weapon))
            tiles.Add(new CardTile(T("card.more", ("n", (Spec.Weapons.CutBonus - 1000) / 10)), T("card.loot")) { Sign = CardSign.Loot, Group = CardGroup.Special });
        return tiles;
    }

    /// <summary>A hit's damage off a wreck or a creature, <paramref name="value"/> as written, in <paramref name="group"/>.</summary>
    private static CardTile Hits(string value, CardBoost? boost, CardGroup group) =>
        new(value, T("card.damage"), boost) { Sign = CardSign.Damage, Group = group, Note = T("card.onWrecks") };

    /// <summary>A hit's damage off rock, walls and plants, its tenths shown as whole points; for none, the words that it cuts no rock, dimmed.</summary>
    private static CardTile Rock(int block, CardBoost? boost, CardGroup group) =>
        new(block > 0 ? Number(block / 10.0) : T("card.noRock"), T("card.block"), boost)
        { Sign = CardSign.Block, Group = group, Note = T("card.onRock"), Off = block == 0 };

    /// <summary>
    /// A gun's rows: a hit's damage off a body and off a block with the first of its ammo, the seconds between shots, and the share
    /// of its damage a shot keeps through each thing it pierces (<see cref="Spec.Gun.PierceKeep"/>); and for a gun that takes the
    /// belt's charge, the power-ticks a shot (2026-10-27-the-railgun).
    /// </summary>
    private static List<CardTile> GunTiles(Spec.Gun gun)
    {
        List<CardTile> tiles =
        [
            Hits(Number(Spec.Shots.DamageOf(gun.Item, gun.Ammo[0])), null, CardGroup.Shot),
            Rock(Spec.Shots.BlockDamageOf(gun.Item, gun.Ammo[0]), null, CardGroup.Shot),
            new CardTile(T("card.seconds", ("n", Number((double)gun.Pace / World.TicksPerSecond))), T("card.reload")) { Sign = CardSign.Pace, Group = CardGroup.Shot },
            new CardTile(T("card.percent", ("n", Number(gun.PierceKeep / 10.0))), T("card.pierce")) { Sign = CardSign.Pierce, Group = CardGroup.Shot },
        ];
        if (gun.Energy > 0) tiles.Add(new CardTile(Number(gun.Energy), T("card.energy")) { Sign = CardSign.Battery, Group = CardGroup.Special });
        return tiles;
    }

    /// <summary>
    /// A thrown charge's rows, under its blast: what it takes off each wreck within its reach, its power on rock and walls in the
    /// tenths a tool's block damage is shown in, its reach in cells, and the seconds between throws (<see cref="Spec.Blast"/>).
    /// </summary>
    private static List<CardTile> BlastTiles(Spec.Gun gun, Spec.Blast blast) =>
    [
        Hits(Number(blast.WreckDamage), null, CardGroup.Blast),
        Rock(blast.Power, null, CardGroup.Blast),
        new CardTile(T("card.cells", ("n", Number(blast.Reach))), T("card.radius")) { Sign = CardSign.Reach, Group = CardGroup.Blast },
        new CardTile(T("card.seconds", ("n", Number((double)gun.Pace / World.TicksPerSecond))), T("card.reload")) { Sign = CardSign.Pace, Group = CardGroup.Blast },
    ];

    /// <summary>Ammo's rows: what it adds to its gun's damage off a body and off a block, "+2".</summary>
    private static List<CardTile> AmmoTiles(string item)
    {
        var projectile = Spec.Shots.Get(item);
        return
        [
            Hits($"+{projectile.Damage}", null, CardGroup.Shot),
            new CardTile($"+{Number(projectile.BlockDamage / 10.0)}", T("card.block")) { Sign = CardSign.Block, Group = CardGroup.Shot, Note = T("card.onRock") },
        ];
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
    /// A meal's rows, under its effect (2026-11-14-food-and-gear-cards): its calm, work and deep rates as whole percent, rounded down
    /// (50 thousandths is 5%), then how long it lasts; a food with no rates a dimmed row saying it only stills hunger. With
    /// <paramref name="left"/>, the HUD's meal: the ticks it has left in place of how long, out of how long. Only what it gives,
    /// never what that comes to in air or time.
    /// </summary>
    public static List<CardTile> FoodRows(Spec.Food food, int? left)
    {
        var rates = food.Rates;
        var rows = new List<CardTile>();
        if (rates.Calm > 0) rows.Add(Effect(CardSign.Calm, T("card.less", ("n", rates.Calm / 10)), "card.calm"));
        if (rates.Work > 0) rows.Add(Effect(CardSign.Work, T("card.more", ("n", rates.Work / 10)), "card.work"));
        if (rates.Deep > 0) rows.Add(Effect(CardSign.Deep, T("card.more", ("n", rates.Deep / 10)), "card.deep"));
        if (rows.Count == 0) rows.Add(Effect(CardSign.Calm, T("card.onlyFeeds"), "card.effect") with { Off = true });
        rows.Add(left is int ticks
            ? Effect(CardSign.Pace, Clock(ticks), "card.left") with { Note = T("card.outOf", ("t", Clock(food.Lasts))) }
            : Effect(CardSign.Pace, Clock(food.Lasts), "card.lasts"));
        return rows;
    }

    private static CardTile Effect(CardSign sign, string value, string label) => Row(sign, CardGroup.Effect, value, label);

    /// <summary>
    /// The HUD's meal of <paramref name="food"/> with <paramref name="left"/> ticks to go: its food card, the time left in place of
    /// how long it lasts, no key and no price. Its name and kind given, pod paste being no item.
    /// </summary>
    public static ItemCard Meal(string food, int left, string name, string kind) =>
        new(food, name, kind, null, null, null) { Food = true, Rows = FoodRows(Spec.Foods.Of(food)!, left) };

    /// <summary>Ticks as minutes and seconds, "15:00", a part second counted whole: a meal's last tick still shows 0:01.</summary>
    public static string Clock(int ticks)
    {
        int seconds = (ticks + World.TicksPerSecond - 1) / World.TicksPerSecond;
        return $"{seconds / 60}:{seconds % 60:00}";
    }

    /// <summary>The steps a second the hero controller takes, which the jetpack's speeds are counted in (<see cref="Spec.Hero"/>).</summary>
    private const int HeroSteps = 60;

    /// <summary>
    /// A worn item's card and its rows (2026-11-14-food-and-gear-cards), what it guarantees in the units the HUD shows: a helmet's
    /// air in O₂; a body's coldest it withstands (2026-11-14-temperature-in-degrees), the regen suit's air made a second and the charge
    /// each unit costs; the cape's share off blows in percent and the degrees it adds; the jetpack's top speed and the air its thrust spends a second; the helmet lamp's reach,
    /// its beam across (<see cref="Light.HeadLampReach"/>) and its draw. A battery's number is its charge, set per stack.
    /// </summary>
    private static ItemCard Worn(ItemCard card)
    {
        var spec = Spec.Items.Get(card.Item);
        var rows = new List<CardTile>();
        if (spec.Tank > 0) rows.Add(Row(CardSign.Tank, CardGroup.Air, T("card.oxygen", ("n", spec.Tank)), "card.tank"));
        if (spec.Low is int low) rows.Add(Row(CardSign.Cold, CardGroup.Guard, T("card.degrees", ("n", Degrees(low))), "card.withstands"));
        if (card.Item == Spec.Suit.Regen)
        {
            double made = (double)Spec.Suit.RegenMost * World.TicksPerSecond / Spec.Crew.BreathEvery;
            rows.Add(Row(CardSign.Regen, CardGroup.Special, T("card.oxygenUpTo", ("n", Number(made))), "card.regen"));
            rows.Add(Row(CardSign.Battery, CardGroup.Special, Number(Spec.Suit.RegenCost), "card.regenCost") with { Note = T("card.perOxygen"), Bolt = true });
        }
        if (card.Item == Spec.Cape.Item)
        {
            rows.Add(Row(CardSign.Guard, CardGroup.Guard, T("card.less", ("n", Spec.Cape.Guard / 10)), "card.blows"));
            rows.Add(Row(CardSign.Cold, CardGroup.Guard, "+" + T("card.degrees", ("n", Spec.Cape.Warmth)), "card.warmth"));
        }
        if (card.Item == Spec.Jetpack.Item)
        {
            rows.Add(Row(CardSign.Speed, CardGroup.Move, T("card.cellsPerSecond", ("n", Number(Spec.Jetpack.Top * HeroSteps / 1000.0))), "card.topSpeed"));
            double spent = (double)World.TicksPerSecond / Spec.Jetpack.TicksPerUnit;
            rows.Add(Row(CardSign.Tank, CardGroup.Move, T("card.oxygenPerSecond", ("n", Number(spent))), "card.thrustAir"));
        }
        if (card.Item == Spec.HelmetLamp.Item)
        {
            int beam = (int)Math.Round(Light.HeadLampHalfAngle * 2 * 180 / Math.PI);
            rows.Add(Row(CardSign.Light, CardGroup.Light, T("card.cells", ("n", Light.HeadLampReach)), "card.range"));
            rows.Add(Row(CardSign.Arc, CardGroup.Light, T("card.degrees", ("n", beam)), "card.beamWidth"));
            // What it draws, nothing: a bolt and 0 says it is free.
            rows.Add(Row(CardSign.Battery, CardGroup.Special, (spec.Draw ?? 0).ToString(CultureInfo.InvariantCulture), "card.draw") with { Bolt = true });
        }
        return card with { Worn = true, Rows = rows };
    }

    private static CardTile Row(CardSign sign, CardGroup group, string value, string label) => new(value, T(label)) { Sign = sign, Group = group };

    /// <summary>
    /// What fed <paramref name="station"/> makes of one smelt of <paramref name="item"/>: what it takes, its yields and
    /// byproducts, and the time one takes there, <c>SmeltTicks</c> at the station's speed, in seconds or past a minute in
    /// minutes, one decimal at most; null for an item it makes nothing of.
    /// </summary>
    public static CardOutput? OutputAt(string item, Spec.Station station) => OutputAt(Spec.Items.Get(item), station);

    /// <summary>What fed <paramref name="station"/> makes of one smelt of the item <paramref name="spec"/> is, as <see cref="OutputAt(string, Spec.Station)"/>.</summary>
    public static CardOutput? OutputAt(Spec.ItemSpec spec, Spec.Station station)
    {
        if (spec.Yields is not { Count: > 0 } yields || spec.SmeltTicks is not int ticks) return null;
        // Speed is in thousandths of full speed, as a craft's time at a station is worked out.
        double seconds = (double)ticks * 1000 / station.Speed / World.TicksPerSecond;
        string time = seconds < 60 ? T("card.seconds", ("n", Number(seconds))) : T("card.minutes", ("n", Number(seconds / 60)));
        var chances = spec.Byproducts?.Select(b => (b.Item, b.Max, b.OneIn ?? 1)).ToList();
        return new CardOutput(yields, time, spec.SmeltTakes, chances);
    }

    /// <summary>The harpoon's tiles: how far its rod flies, in cells; the seconds till it fires again; the cells a second it reels.</summary>
    private static List<CardTile> HarpoonTiles() => LineTiles(Spec.Harpoon.CooldownTicks, Spec.Harpoon.ReelSpeed, "card.reel");

    /// <summary>The grappling gun's tiles (grapple-climbs): how far its hook flies, in cells; the seconds till it fires again; the cells a second it pulls.</summary>
    private static List<CardTile> GrappleTiles() => LineTiles(Spec.Grapple.CooldownTicks, Spec.Grapple.PullSpeed, "card.pull");

    /// <summary>A line's rows, the harpoon's and the grapple's: the seconds till it fires again; how far it flies and how fast it winds in.</summary>
    private static List<CardTile> LineTiles(int cooldown, int speed, string wind) =>
    [
        new CardTile(T("card.seconds", ("n", Number((double)cooldown / World.TicksPerSecond))), T("card.reload")) { Sign = CardSign.Pace, Group = CardGroup.Shot },
        new CardTile(T("card.cells", ("n", Number(Spec.Harpoon.Range / 1000.0))), T("card.range")) { Sign = CardSign.Reach, Group = CardGroup.Reach },
        new CardTile(T("card.perSecond", ("n", Number(speed * World.TicksPerSecond / 1000.0))), T(wind)) { Sign = CardSign.Reel, Group = CardGroup.Reach },
    ];

    /// <summary>A craft's name, its item; a blueprint's, its tool and material: "Wall: iron".</summary>
    public static string NameOf(string id, Placement? placement) => NameIn(Speaking, id, placement);

    /// <summary><see cref="NameOf"/> in <paramref name="language"/>: the board's search finds a thing by its name in either.</summary>
    public static string NameIn(Language language, string id, Placement? placement) => placement is { } p
        ? p.Material is { } m ? $"{In(language, $"tool.{p.Kind}")}: {In(language, $"material.{m}")}" : In(language, $"tool.{p.Kind}")
        : In(language, $"item.{id}");

    /// <summary><paramref name="n"/> with one decimal at most, half rounded up: 4, 5.3, 0.5.</summary>
    public static string Number(double n) =>
        Math.Round(n, 1, MidpointRounding.AwayFromZero).ToString("0.#", CultureInfo.InvariantCulture);

    /// <summary>Degrees as the cards write them, a minus sign rather than a hyphen below zero: "−40".</summary>
    public static string Degrees(int degrees) => degrees < 0 ? "−" + (-degrees).ToString(CultureInfo.InvariantCulture) : degrees.ToString(CultureInfo.InvariantCulture);
}
