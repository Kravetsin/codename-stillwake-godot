using Stillwake.Content;
using L = Stillwake.Content.Vessels.Liner;

namespace Stillwake.Sim;

public static partial class Shipyard
{
    /// <summary>
    /// A liner (big-vessels) from <paramref name="seed"/>. Stream 0: its decks and length drawn, then its blocks: the upper band's
    /// aft block (the way in over engine rooms), the service block, blocks of cabins and now and then a public one (infirmary, lounge or
    /// storage), the pool's and the restaurant's two decks tall with a lounge and a galley over them and an infirmary's public block at
    /// their places along it, the forward block under the bridge; the bottom band's fuel tank by the engines, holds, the hangar at the
    /// bow; the raised bridge, the promenade and its dome. Then laid as every big class is (<see cref="Lay"/>).
    /// </summary>
    private static Vessel Liner(VesselClass cls, uint seed)
    {
        var rng = Draws(cls, seed, 0);
        int decks = Between(rng, cls.Decks), low = decks - L.LowDecks, run = Between(rng, L.Run);
        int a = cls.ArmourDepth;
        int start = Vessels.NozzleSize.W + a;

        Stacked Cabin(int deck) => new(deck, 1, VesselRole.Quarters, "cabin");

        var upper = new List<Block>();
        var aft = Made(0, [RoomOf(rng, 0, VesselRole.Quarters), .. Enumerable.Range(1, low - 1).Select(d => RoomOf(rng, d, VesselRole.Engine, main: true))], past: 0);
        var service = Service(rng, Between(rng, L.Service), low);
        upper.Add(aft);
        upper.Add(service);
        aft.Left = start;
        service.Left = start + aft.Width + 2;
        int x = service.Left + service.Width + 2;

        int forwardWidth = Between(rng, L.Forward), end = start + run - forwardWidth;
        int poolAt = start + run * Between(rng, L.PoolAt) / 100, infirmaryAt = start + run * Between(rng, L.InfirmaryAt) / 100,
            restaurantAt = start + run * Between(rng, L.RestaurantAt) / 100;
        bool pool = false, infirmary = false, restaurant = false;
        Block Tall(VesselRole role, VesselRole over)
        {
            int w = Between(rng, L.Tall);
            return Made(w, [new(0, 1, over, ""), new(1, 2, role, ""), .. Enumerable.Range(3, Math.Max(0, low - 3)).Select(Cabin)]);
        }
        Block Public(bool sick)
        {
            int w = Between(rng, L.Public);
            return Made(w, [Cabin(0), .. Enumerable.Range(1, low - 1).Select(d => sick && d == 1 ? new Stacked(d, 1, VesselRole.Infirmary, "")
                : (int)rng.Below(3) switch { 0 => new Stacked(d, 1, VesselRole.Infirmary, ""), 1 => new Stacked(d, 1, VesselRole.Lounge, ""), _ => RoomOf(rng, d, VesselRole.Hold) })]);
        }
        for (int guard = 0; guard < 90; guard++)
        {
            Block b;
            if (!pool && x >= poolAt) (b, pool) = (Tall(VesselRole.Pool, VesselRole.Lounge), true);
            else if (!infirmary && x >= infirmaryAt) (b, infirmary) = (Public(sick: true), true);
            else if (!restaurant && x >= restaurantAt) (b, restaurant) = (Tall(VesselRole.Restaurant, VesselRole.Galley), true);
            else if (rng.Below(100) < L.PublicPercent) b = Public(sick: false);
            else b = Made(Between(rng, L.Cabins), [.. Enumerable.Range(0, low).Select(Cabin)]);
            if (x + b.Width + 2 > end) break;
            b.Left = x;
            upper.Add(b);
            x += b.Width + 2;
        }
        var fwd = Made(forwardWidth, [RoomOf(rng, 0, VesselRole.Quarters), .. Enumerable.Range(1, low - 1).Select(Cabin)]);
        fwd.Left = x;
        fwd.Width = Math.Max(fwd.Width, start + run - x);
        upper.Add(fwd);
        run = fwd.Left + fwd.Width - start;
        foreach (var role in (VesselRole[])[VesselRole.Pool, VesselRole.Restaurant, VesselRole.Infirmary])
            if (!upper.Any(b => b.Stack.Any(s => s.Role == role))) throw new InvalidOperationException($"liner {seed}: no {role}");

        // The bottom band: the fuel tank under the engines, holds, the hangar at the bow at least so wide.
        var lower = new List<Block> { new(aft.Width, [new(low, L.LowDecks, VesselRole.FuelBay, "")]) { Left = start } };
        int bx = start + aft.Width;
        for (int guard = 0; guard < 90; guard++)
        {
            int w = Between(rng, L.Hold);
            if (start + run - (bx + 2 + w + 2) < L.HangarMin) break;
            lower.Add(new(w, [new(low, L.LowDecks, VesselRole.Hold, "")]) { Left = bx + 2 });
            bx += 2 + w;
        }
        lower.Add(new(start + run - bx - 2, [new(low, L.LowDecks, VesselRole.Hangar, "")]) { Left = bx + 2 });

        // Raised: the bridge over the forward block, its housing behind it, the promenade over the first block well past the service
        // block, short of the housing, and the dome on its stern end.
        var bridgeRoom = Furnish(rng, VesselRole.Bridge, main: false);
        int forward = fwd.Left, bridgeLeft = forward - a;
        int serviceEnd = service.Left + service.Width;
        int housing = bridgeLeft - Vessels.HousingWidth >= serviceEnd ? bridgeLeft - Vessels.HousingWidth : -1;
        var first = upper.First(b => b.Left > serviceEnd + L.PromenadePast);
        int promenadeWidth = Math.Min(Between(rng, L.Promenade), (housing >= 0 ? housing : bridgeLeft) - L.PromenadeGap - a - first.Left);
        var raised = new List<Raised> { new(Vessel.SectionKind.Bridge, forward, bridgeRoom.Width, 1, VesselRole.Bridge, bridgeRoom.Interior) };
        if (promenadeWidth >= L.PromenadeMin)
        {
            raised.Add(new(Vessel.SectionKind.Tier, first.Left, promenadeWidth, 1, VesselRole.Lounge, ""));
            raised.Add(new(Vessel.SectionKind.Tier, first.Left, Math.Min(L.DomeMax, promenadeWidth - L.DomeShort), 2, VesselRole.Lounge, ""));
        }
        return Lay(cls, seed, rng, decks, [upper, lower], service, raised, housing, L.BulgeDown, tankLadders: false);
    }
}
