using System.Text.Json.Serialization;
using Stillwake.Content;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;

namespace Stillwake.Sim.Site;

/// <summary>SPIKE (planet): the sender's hero beams to site <c>Site</c> (1 to <c>Site.Count</c>), or home with 0.</summary>
public sealed record Beam(int Site) : Command;

/// <summary>SPIKE: a hit with weapon <c>Tool</c> on the ground at <c>Cell</c> of the sender's site.</summary>
public sealed record Dig(int Cell, string Tool) : Command;

/// <summary>SPIKE: the sender picks up drop <c>Id</c> lying on its site.</summary>
public sealed record Gather(int Id) : Command;

/// <summary>A cell of ground hit and not broken yet: the damage it has taken.</summary>
public sealed record Hits(int Cell, int Damage);

/// <summary>What a broken cell gave that the digger had no room for, lying where it fell.</summary>
public sealed record Drop(int Id, int Cell, string Item, int Count);

/// <summary>A site made: its seed, and every change from the ground the seed makes.</summary>
public sealed class Ground
{
    [JsonInclude] public int Site { get; internal set; }
    [JsonInclude] public uint Seed { get; internal set; }
    /// <summary>The cells dug out, ascending: all the save needs to make the ground again.</summary>
    [JsonInclude] public List<int> Dug { get; internal set; } = [];
    [JsonInclude] public List<Hits> Hit { get; internal set; } = [];
    [JsonInclude] public List<Drop> Drops { get; internal set; } = [];
}

public sealed class SiteState
{
    /// <summary>Every site made and kept, by site number.</summary>
    [JsonInclude] internal List<Ground> Sites { get; set; } = [];
    [JsonInclude] internal int NextDrop { get; set; } = 1;
}

/// <summary>
/// SPIKE (planet): sorties down to planet sites. A site is ground only — no rooms, air, pipes, power, machines or
/// plans: cells to break, what breaking them gives, and heroes walking it (their bodies and suits are the crew's,
/// their items the items'). Its ground is derived, made from its seed with the dug cells taken out, never saved.
/// Nothing on a site moves with time, so a site no one is on simply waits: no catch-up.
/// </summary>
public sealed class SiteModule : Module<SiteState>
{
    private readonly ICrewQueries _crew;
    private readonly ICrewOps _crewOps;
    private readonly IItemsQueries _items;
    private readonly IItemsOps _itemsOps;
    /// <summary>Derived: each kept site's ground as a grid of its own, and where heroes beam down on it.</summary>
    private readonly Dictionary<int, (GridModule Grid, int Spawn)> _grounds = new();

    public override string Id => "site";

    /// <summary>Whether a site keeps its dug cells while no one is on it; otherwise forgotten when the last hero leaves.</summary>
    public bool Kept { get; init; } = Content.Site.Kept;

    public SiteModule(ICrewQueries crew, ICrewOps crewOps, IItemsQueries items, IItemsOps itemsOps)
    {
        (_crew, _crewOps, _items, _itemsOps) = (crew, crewOps, items, itemsOps);
        Handles<Beam>(BeamProblem, ApplyBeam);
        Handles<Dig>(DigProblem, ApplyDig);
        Handles<Gather>(GatherProblem, ApplyGather);
        // A site left by its last hero, one who beamed up or passed out there, is forgotten unless sites are kept.
        System(Phase.Events, () =>
        {
            if (Kept) return;
            for (int i = State.Sites.Count - 1; i >= 0; i--)
                if (!Visited(State.Sites[i].Site))
                {
                    _grounds.Remove(State.Sites[i].Site);
                    State.Sites.RemoveAt(i);
                }
        });
    }

    protected override SiteState Init(Layout layout) => new();

    public override void Rebuild()
    {
        _grounds.Clear();
        foreach (var g in State.Sites) Make(g);
    }

    private (GridModule Grid, int Spawn) Make(Ground g)
    {
        var (layout, spawn) = Terrain.Make(g.Seed);
        var grid = Module.Standalone(new GridModule(), layout);
        foreach (int cell in g.Dug) grid.Clear(cell, "structure");
        return _grounds[g.Site] = (grid, spawn);
    }

    // Queries.

    /// <summary>Site <paramref name="site"/>'s ground as cells, null while it is not made: what the client draws.</summary>
    public GridModule? GroundOf(int site) => _grounds.TryGetValue(site, out var g) ? g.Grid : null;

    /// <summary>Where heroes beam down on site <paramref name="site"/>; -1 while it is not made.</summary>
    public int SpawnOf(int site) => _grounds.TryGetValue(site, out var g) ? g.Spawn : -1;

    /// <summary>What lies on site <paramref name="site"/>.</summary>
    public IReadOnlyList<Drop> DropsOn(int site) => State.Sites.Find(g => g.Site == site)?.Drops ?? [];

    /// <summary>Damage taken by the cell at <paramref name="cell"/> of site <paramref name="site"/>, of what breaks it.</summary>
    public (int Damage, int Hp) DamageAt(int site, int cell)
    {
        var g = State.Sites.Find(s => s.Site == site);
        var grid = GroundOf(site);
        if (g == null || grid?.WallAt(cell) is not { } wall) return (0, 0);
        return (g.Hit.Find(h => h.Cell == cell)?.Damage ?? 0, Content.Site.HpOf(wall.Id));
    }

    /// <summary>The sites made and kept, by number.</summary>
    public IReadOnlyList<Ground> Sites => State.Sites;

    private bool Visited(int site)
    {
        foreach (var h in _crew.Heroes)
            if (h.Site == site) return true;
        return false;
    }

    // Commands.

    private string? BeamProblem(Beam c)
    {
        var hero = _crew.HeroOf(c.From);
        if (hero == null) return "crew.noHero";
        if (hero.Out > 0) return "site.out";
        if (c.Site < 0 || c.Site > Content.Site.Count) return "site.noSite";
        return hero.Site == c.Site ? "site.there" : null;
    }

    private void ApplyBeam(Beam c)
    {
        if (c.Site == 0)
        {
            _crewOps.Send(c.From, 0, _crew.Spawn);
            return;
        }
        var ground = State.Sites.Find(g => g.Site == c.Site);
        if (ground == null)
        {
            // A new site's seed is a draw of the world's: the same on every peer.
            ground = new Ground { Site = c.Site, Seed = Random.Below(1UL << 32) };
            State.Sites.Add(ground);
            State.Sites.Sort((a, b) => a.Site.CompareTo(b.Site));
            Make(ground);
        }
        _crewOps.Send(c.From, c.Site, _grounds[c.Site].Spawn);
    }

    private string? DigProblem(Dig c)
    {
        if (Weapons.Of(c.Tool) == null) throw new InvalidOperationException($"site.dig: {c.Tool} deals no damage");
        if (_crew.HeroOf(c.From) is not { Site: > 0 } hero) return "site.aboard";
        if (hero.Out > 0) return "site.out";
        if (GroundOf(hero.Site)!.WallAt(c.Cell) == null) return "site.nothing";
        return Content.Items.Get(c.Tool).Draw != null && _items.Running(c.From) != c.Tool ? "site.notRunning" : null;
    }

    private void ApplyDig(Dig c)
    {
        int site = _crew.HeroOf(c.From)!.Site;
        var g = State.Sites.Find(s => s.Site == site)!;
        var grid = _grounds[site].Grid;
        var wall = grid.WallAt(c.Cell)!;
        int at = g.Hit.FindIndex(h => h.Cell >= c.Cell);
        int damage = (at >= 0 && g.Hit[at].Cell == c.Cell ? g.Hit[at].Damage : 0) + Weapons.Of(c.Tool)!.Damage;
        if (damage < Content.Site.HpOf(wall.Id))
        {
            if (at >= 0 && g.Hit[at].Cell == c.Cell) g.Hit[at] = new Hits(c.Cell, damage);
            else g.Hit.Insert(at < 0 ? g.Hit.Count : at, new Hits(c.Cell, damage));
            return;
        }
        if (at >= 0 && g.Hit[at].Cell == c.Cell) g.Hit.RemoveAt(at);
        grid.Clear(c.Cell, "structure");
        int dug = g.Dug.BinarySearch(c.Cell);
        g.Dug.Insert(~dug, c.Cell);
        var (item, count) = Content.Site.YieldOf(wall.Id);
        if (_items.FullFor(c.From, [(item, count)]) == null) _itemsOps.Give(c.From, [(item, count)]);
        else g.Drops.Add(new Drop(State.NextDrop++, Fallen(grid, c.Cell), item, count));
    }

    /// <summary>Where something let go at <paramref name="cell"/> comes to rest: straight down onto ground.</summary>
    private static int Fallen(GridModule grid, int cell)
    {
        int width = grid.Width;
        while (cell + width < width * grid.Height && grid.StructureAt(cell + width) == Structure.Empty) cell += width;
        return cell;
    }

    private string? GatherProblem(Gather c)
    {
        if (_crew.HeroOf(c.From) is not { Site: > 0 } hero) return "site.aboard";
        var drop = DropsOn(hero.Site).FirstOrDefault(d => d.Id == c.Id);
        if (drop == null) return "site.gone";
        return _items.FullFor(c.From, [(drop.Item, drop.Count)]);
    }

    private void ApplyGather(Gather c)
    {
        var g = State.Sites.Find(s => s.Site == _crew.HeroOf(c.From)!.Site)!;
        var drop = g.Drops.Find(d => d.Id == c.Id)!;
        _itemsOps.Give(c.From, [(drop.Item, drop.Count)]);
        g.Drops.Remove(drop);
    }

    /// <summary>
    /// A beam now and then (refused ones too), else a hit on the ground near the sender's hero with any weapon,
    /// or a gather of a drop that is there or not.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        var hero = _crew.HeroOf(from);
        uint kind = rng.Below(16);
        // Aboard, a beam now and then; down, home in some 50 of its turns: heroes spend most of a run aboard, as the ship's suites expect.
        if (hero is not { Site: > 0 }) return rng.Below(16) == 0 ? new Beam((int)rng.Below(Content.Site.Count + 2)) { From = from } : null;
        if (kind < 2) return new Beam(rng.Below(4) == 0 ? (int)rng.Below(Content.Site.Count + 2) : 0) { From = from };
        var grid = GroundOf(hero.Site);
        if (kind == 2 || grid == null)
            return new Gather((int)rng.Below((uint)State.NextDrop + 1)) { From = from };
        int x = hero.Cell % grid.Width + (int)rng.Below(9) - 4, y = hero.Cell / grid.Width + (int)rng.Below(9) - 2;
        var tool = Weapons.All[(int)rng.Below((uint)Weapons.All.Count)].Item;
        return new Dig(y * grid.Width + x, tool) { From = from };
    }
}
