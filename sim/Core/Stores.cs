namespace Stillwake.Sim;

/// <summary>The liquid one machine holds, by its machine id: a cistern's water, a fuel store's biofuel.</summary>
public sealed class Stored
{
    public int Machine { get; set; }
    public int Amount { get; set; }
}

/// <summary>
/// The liquid stores a module keeps (fuel-liquid), shared by Water and Fuel: a list of <see cref="Stored"/> in machine id order,
/// none holding nothing, and the line that pours from one to the next (tanks-in-a-line). The module says which machines give and
/// take, and how much each holds full.
/// </summary>
public static class Stores
{
    /// <summary>What machine <paramref name="machine"/> holds in <paramref name="stores"/>; none for one never filled.</summary>
    public static int AmountIn(List<Stored> stores, int machine) => stores.Find(s => s.Machine == machine)?.Amount ?? 0;

    /// <summary>Adds <paramref name="amount"/> to machine <paramref name="machine"/>'s store, listing it in id order if new.</summary>
    public static void Add(List<Stored> stores, int machine, int amount)
    {
        if (amount <= 0) return;
        if (stores.Find(s => s.Machine == machine) is { } held)
        {
            held.Amount += amount;
            return;
        }
        int at = stores.FindIndex(s => s.Machine > machine);
        stores.Insert(at < 0 ? stores.Count : at, new Stored { Machine = machine, Amount = amount });
    }

    /// <summary>Takes up to <paramref name="amount"/> out of machine <paramref name="machine"/>'s store, unlisting it once empty; returns what it took.</summary>
    public static int Take(List<Stored> stores, int machine, int amount)
    {
        int at = stores.FindIndex(s => s.Machine == machine);
        if (at < 0 || amount <= 0) return 0;
        int taken = Math.Min(amount, stores[at].Amount);
        stores[at].Amount -= taken;
        if (stores[at].Amount == 0) stores.RemoveAt(at);
        return taken;
    }

    /// <summary>
    /// A tick of the lines (tanks-in-a-line): each of <paramref name="givers"/>, in the order given (id order), holding any, pours up to
    /// <paramref name="flow"/> of its own into <paramref name="takersOf"/> it, in their order, as much as each has room below
    /// <paramref name="capacityOf"/>, never into itself. A later giver pours on what it was given this tick, so a line fills from its
    /// last store. Nothing is made or lost.
    /// </summary>
    public static void Pour(List<Stored> stores, IEnumerable<int> givers, Func<int, IEnumerable<int>> takersOf, Func<int, int> capacityOf, int flow)
    {
        foreach (int giver in givers)
        {
            int left = Math.Min(AmountIn(stores, giver), flow);
            if (left == 0) continue;
            foreach (int taker in takersOf(giver))
            {
                if (left == 0) break;
                if (taker == giver) continue;
                // A store loaded fuller than it holds from an old save takes nothing till it drains below.
                int poured = Math.Min(left, capacityOf(taker) - AmountIn(stores, taker));
                if (poured <= 0) continue;
                Add(stores, taker, poured);
                Take(stores, giver, poured);
                left -= poured;
            }
        }
    }
}
