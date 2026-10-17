using Stillwake.Sim.Items;
using CrewSpec = Stillwake.Content.Crew;
using ItemSpecs = Stillwake.Content.Items;

namespace Stillwake.Game.Features.Items;

/// <summary>A stack picked up to be put elsewhere: only on screen, the items stay in their slot till then.</summary>
public readonly record struct Picked(Place Place, int Count);

/// <summary>A click on a slot: what it holds now, and the crate open beside the inventory, if any.</summary>
/// <param name="FuelSlot">The slot the open crate burns fuel in, when it is a furnace.</param>
public sealed record SlotClick(Place Place, Stack? Stack, bool Right, bool Shift, int? Crate, int? FuelSlot = null);

/// <summary>What a click leaves picked, the move it sends, if any, and whether it breathes a canister (0047).</summary>
public readonly record struct Clicked(Picked? Picked, Move? Command = null, bool Breathes = false);

/// <summary>The rules of clicks on slots (apps/game/src/features/items/click.ts).</summary>
public static class Clicks
{
    /// <summary>
    /// Terraria's way: a left click picks a whole stack and puts it on the next slot clicked, which swaps
    /// it with another item there; a right click picks half, rounded up, or with a stack picked puts one
    /// of it; a shift-click sends a stack to the other side of an open crate, a fuel into a furnace's
    /// fuel slot. A click on the picked slot puts it back. A right click on a canister of the hero's own,
    /// nothing picked, breathes it into the suit (0047) rather than picking half.
    /// </summary>
    public static Clicked On(Picked? picked, SlotClick click)
    {
        var (place, stack) = (click.Place, click.Stack);
        if (click.Shift && !click.Right)
        {
            if (stack == null || click.Crate is not int crate) return new Clicked(picked);
            bool burns = ItemSpecs.Get(stack.Item).BurnTicks != null;
            var other = place.Crate != null
                ? new Target()
                : new Target(burns ? click.FuelSlot : null, crate);
            return new Clicked(null, new Move(place, other, stack.Count));
        }
        if (click.Right)
        {
            if (picked is { } p && p.Place != place)
            {
                int left = p.Count - 1;
                return new Clicked(left > 0 ? p with { Count = left } : null, new Move(p.Place, place, 1));
            }
            if (picked == null && place.Crate == null && stack?.Item == CrewSpec.Canister) return new Clicked(null, Breathes: true);
            return new Clicked(stack != null ? new Picked(place, (stack.Count + 1) / 2) : null);
        }
        if (picked is not { } held) return new Clicked(stack != null ? new Picked(place, stack.Count) : null);
        if (held.Place == place) return new Clicked(null);
        return new Clicked(null, new Move(held.Place, place, held.Count));
    }
}
