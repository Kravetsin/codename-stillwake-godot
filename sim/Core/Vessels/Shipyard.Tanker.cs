using Stillwake.Content;
using T = Stillwake.Content.Vessels.Tanker;

namespace Stillwake.Sim;

public static partial class Shipyard
{
    /// <summary>
    /// A tanker (big-vessels) from <paramref name="seed"/>, one band over every deck. Stream 0: its decks and length drawn, then its
    /// blocks: the aft block (the way in over engine rooms, two columns past its drawings for the hatches down them, since the fuel
    /// bay beside it walls them off), the fuel bay (a pipeway over one fuel tank from the second deck to the last), the service block, then tank bays (a pipeway, tanks two decks tall, a pump room on a deck left over) and blocks of pump
    /// rooms by turns, now and then a hangar (a pipeway and pump rooms over a hangar on the last two decks) where it was drawn along the
    /// run, the forward block of storage; then the raised bridge over the stern, its hatch where the airlock's drawing leaves room for
    /// its ladder. Then laid as every big class is (<see cref="Lay"/>), a ladder up each tank's side.
    /// </summary>
    private static Vessel Tanker(VesselClass cls, uint seed)
    {
        var rng = Draws(cls, seed, 0);
        int decks = Between(rng, cls.Decks), run = Between(rng, T.Run);
        int start = Vessels.NozzleSize.W + cls.ArmourDepth;
        Stacked Pipeway() => new(0, 1, VesselRole.Pipeway, "");
        Stacked Pumps(int deck) => new(deck, 1, VesselRole.PumpRoom, "");

        var aft = Made(0, [RoomOf(rng, 0, VesselRole.Quarters), .. Enumerable.Range(1, decks - 1).Select(d => RoomOf(rng, d, VesselRole.Engine, main: true))]);
        aft.Left = start;
        var fuel = new Block(Between(rng, T.FuelBay), [Pipeway(), new(1, decks - 1, VesselRole.FuelBay, "")]) { Left = aft.Left + aft.Width + 2 };
        var service = Service(rng, Between(rng, T.Service), decks);
        service.Left = fuel.Left + fuel.Width + 2;
        var band = new List<Block> { aft, fuel, service };
        int x = service.Left + service.Width + 2;

        int forwardWidth = Between(rng, T.Forward), end = start + run - forwardWidth;
        int hangarAt = rng.Below(100) < T.HangarPercent ? x + (end - x) * Between(rng, T.HangarAt) / 100 : int.MaxValue;
        bool bay = true;
        for (int guard = 0; guard < 90; guard++)
        {
            Block b;
            if (x >= hangarAt)
            {
                b = new(Between(rng, T.Hangar), [Pipeway(), .. Enumerable.Range(1, decks - 3).Select(Pumps), new(decks - 2, 2, VesselRole.Hangar, "")]);
                hangarAt = int.MaxValue;
            }
            else if (bay)
            {
                var stack = new List<Stacked> { Pipeway() };
                int d = 1;
                for (; d + 1 < decks; d += 2) stack.Add(new(d, 2, VesselRole.TankBay, ""));
                if (d < decks) stack.Add(Pumps(d));
                b = new(Between(rng, T.Bay), stack);
                bay = false;
            }
            else
            {
                b = new(Between(rng, T.Pumps), [Pipeway(), .. Enumerable.Range(1, decks - 1).Select(Pumps)]);
                bay = true;
            }
            if (x + b.Width + 2 > end) break;
            b.Left = x;
            band.Add(b);
            x += b.Width + 2;
        }
        var fwd = Made(forwardWidth, [.. Enumerable.Range(0, decks).Select(d => RoomOf(rng, d, VesselRole.Hold))]);
        fwd.Left = x;
        fwd.Width = Math.Max(fwd.Width, start + run - x);
        band.Add(fwd);

        // The bridge over the stern, from the first columns past BridgeFrom the airlock's drawing keeps clear for the ladder under its hatch.
        var bridgeRoom = Furnish(rng, VesselRole.Bridge, main: false);
        var airlock = new Vessel.Compartment(0, start, aft.Width, 0, VesselRole.Quarters, aft.Stack[0].Interior);
        int bridge = start + T.BridgeFrom;
        while (!Clear(airlock, bridge, below: true)) bridge++;
        Raised[] raised = [new(Vessel.SectionKind.Bridge, bridge, bridgeRoom.Width, 1, VesselRole.Bridge, bridgeRoom.Interior)];
        return Lay(cls, seed, rng, decks, [band], service, raised, housing: -1, bulgeDown: int.MaxValue, tankLadders: true);
    }
}
