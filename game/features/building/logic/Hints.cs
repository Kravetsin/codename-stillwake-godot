using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Building;

/// <summary>
/// What the open plan shows of its hand now, for its hint (build-hints): the mode shown, whether it has both layers to
/// turn between (aboard, a surface with back walls), the focus, the card in hand, the search typed in, and the builder
/// to take for the plans that stand, if any do.
/// </summary>
public sealed record HandNow(PlanMode Mode, bool Layers, Focus Focus, Tool? Held, bool Typing, bool Planned, Builder? Builder);

/// <summary>The builder a player carries: its item and its hotbar slot from 1, none while it is in the bag.</summary>
public sealed record Builder(string Item, int? Slot);

/// <summary>What the pointer is over, for the hint beside it.</summary>
public enum Over
{
    Nothing,
    /// <summary>A plan of the focused layer: the right cancels it.</summary>
    Plan,
    /// <summary>Something built whose card is known: Drop takes that card.</summary>
    Built,
}

/// <summary>
/// The plan's hint (build-hints): a few short lines, a press and a few words each, chosen by what is in hand, so a line
/// says only what can be done now. Its words are i18n keys.
/// </summary>
public static class Hints
{
    /// <summary>The lines under the cards: at most four.</summary>
    public static IReadOnlyList<KeyHint> LinesOf(HandNow now)
    {
        var lines = new List<KeyHint>();
        if (now.Typing)
            return [Line("hint.typed", new CapPress("Enter")), Line("hint.clear", new CapPress("Esc"))];
        switch (now.Mode)
        {
            case PlanMode.Wiring:
                lines.Add(Line("hint.cable", Mouse.LeftDrag));
                lines.Add(Line("hint.peg", new CapPress("Ctrl"), new MousePress(Mouse.LeftDrag)));
                lines.Add(Line("hint.cut", Mouse.Right));
                break;
            case PlanMode.Piping when now.Held is HandCard { Id: "valve" }:
                lines.Add(Line("hint.valve", Mouse.Left));
                lines.Add(Line("hint.valveOff", Mouse.Right));
                break;
            case PlanMode.Piping:
                lines.Add(Line("hint.pipe", Mouse.LeftDrag));
                lines.Add(Line("hint.pipeOff", Mouse.Right));
                break;
            default:
                if (now.Held is { } card)
                {
                    lines.Add(Line("hint.place", Mouse.Left));
                    if (DragSays(card) is { } drag) lines.Add(Line(drag, Mouse.LeftDrag));
                }
                else lines.Add(Line("hint.pick", Mouse.Left));
                // With a card in hand the right lets it go; with none it takes down (right-lets-go).
                lines.Add(now.Held != null ? Line("hint.letGo", Mouse.Right) : Line("hint.takeDown", Mouse.RightDrag));
                if (now.Held == null && now.Layers)
                    lines.Add(Line(now.Focus == Focus.Front ? "hint.toBack" : "hint.toFront", Mouse.Middle));
                break;
        }
        lines.Add(now.Planned ? BuildLine(now.Builder) : Line("hint.close", new KeyPress(Bind.Build)));
        return lines;
    }

    /// <summary>
    /// The line beside the pointer for what it is over, building; none over nothing it acts on, nor wiring or piping. A plan
    /// is cancelled by the right only with no card in hand: <paramref name="holding"/> one, the right lets it go instead.
    /// </summary>
    public static KeyHint? PointerOf(PlanMode mode, Over over, bool holding) => (mode, over) switch
    {
        (PlanMode.Build, Over.Plan) when !holding => Line("hint.cancel", Mouse.Right),
        (PlanMode.Build, Over.Built) => Line("hint.same", new KeyPress(Bind.Drop)),
        _ => null,
    };

    /// <summary>
    /// The builder to take from <paramref name="inventory"/>, slots in order with the hotbar's first: the power driver before
    /// the screwdriver (it works faster), the hotbar's before the bag's; none when neither is carried.
    /// </summary>
    public static Builder? BuilderOf(IReadOnlyList<string?> inventory, int hotbar)
    {
        foreach (string item in (string[])[Content.Screwdrivers.PowerDriver.Item, Content.Screwdrivers.Basic.Item])
        {
            int at = inventory.ToList().IndexOf(item);
            if (at >= 0) return new Builder(item, at < hotbar ? at + 1 : null);
        }
        return null;
    }

    /// <summary>What a drag of <paramref name="card"/> does, if more than a click: fill a rectangle, or lay a row.</summary>
    private static string? DragSays(Tool card) => Drag.ShapeOf(card) switch
    {
        DragShape.Area => "hint.fill",
        DragShape.Pieces or DragShape.Line => "hint.row",
        _ => null,
    };

    /// <summary>Take the builder (its slot's digit puts the plan away) and hold the left on a plan; red when none is carried.</summary>
    private static KeyHint BuildLine(Builder? builder) => builder switch
    {
        null => new KeyHint([new ItemPress(Content.Screwdrivers.Basic.Item)], "hint.noBuilder", Short: true),
        { Slot: int slot } => new KeyHint([new CapPress(slot.ToString(System.Globalization.CultureInfo.InvariantCulture)), new ItemPress(builder.Item), new MousePress(Mouse.LeftHold)], "hint.build"),
        _ => new KeyHint([new ItemPress(builder.Item), new MousePress(Mouse.LeftHold)], "hint.build"),
    };

    private static KeyHint Line(string says, Mouse button) => new([new MousePress(button)], says);

    private static KeyHint Line(string says, params Press[] presses) => new(presses, says);
}
