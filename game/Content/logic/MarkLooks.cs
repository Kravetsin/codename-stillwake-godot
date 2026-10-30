using System.Collections.Generic;
using Stillwake.Game.Shared;
using static Stillwake.Game.IconLooks;

namespace Stillwake.Game;

/// <summary>
/// The marks of what a station does with an item (0097), in the module technologies' line language (0086) but in a ring
/// rather than their hexagon (<see cref="IconLooks.Ring"/>): a hexagon is a module, a ring a station's work. Each its
/// glyph, its colours and its motion: in an inventory slot's corner while the station is open (<see cref="SlotMark"/>),
/// before "Burns here" and the like on a card, and as the faint ghost in the station's own empty slot that takes it.
/// </summary>
public static class MarkLooks
{
    /// <summary>What a fire burns: a flame, its tip bent, a tongue at its side and a lick in it, orange-rimmed and white-hot, flickering.</summary>
    public static readonly Look Burns = new(Ring,
        [
            new Stroke([(.5f, .77f), (.39f, .735f), (.325f, .645f), (.33f, .53f), (.385f, .44f), (.415f, .5f), (.445f, .37f),
                (.515f, .27f), (.54f, .17f), (.6f, .29f), (.665f, .42f), (.69f, .55f), (.66f, .665f), (.6f, .74f), (.5f, .77f)]),
            new Stroke([(.5f, .705f), (.44f, .655f), (.445f, .565f), (.505f, .47f), (.56f, .57f), (.56f, .655f), (.5f, .705f)], Weight: 0.8f),
        ],
        Core: 0xfff4d6, Rim: 0xff8a3a, Glow: 0xff6a1a, IconMotion.Flicker, Pivot: (.5f, .77f));

    /// <summary>What a station smelts: an ingot, hot from the melt, gold-rimmed and white-hot, its heat rising off it in two wisps.</summary>
    public static readonly Look Smelts = new(Ring,
        [
            new Stroke([(.22f, .7f), (.78f, .7f), (.68f, .53f), (.32f, .53f), (.22f, .7f)], Moves: false),
            new Stroke([(.41f, .44f), (.39f, .375f), (.42f, .31f), (.4f, .245f)], Weight: 0.7f),
            new Stroke([(.59f, .44f), (.57f, .375f), (.6f, .31f), (.58f, .245f)], Weight: 0.7f),
        ],
        Core: 0xfff7e0, Rim: 0xffc147, Glow: 0xff9a24, IconMotion.Simmer);
    /// <summary>What a station grows: a sprout of two leaves on the ground, green-rimmed, swaying.</summary>
    public static readonly Look Grows = new(Ring,
        [
            new Stroke([(.5f, .77f), (.5f, .62f), (.515f, .48f), (.5f, .36f)]),
            new Stroke([(.5f, .59f), (.42f, .49f), (.27f, .45f), (.31f, .57f), (.41f, .625f), (.5f, .59f)], Weight: 0.85f),
            new Stroke([(.505f, .43f), (.565f, .32f), (.7f, .27f), (.69f, .395f), (.6f, .46f), (.505f, .43f)], Weight: 0.85f),
            new Stroke([(.33f, .77f), (.67f, .77f)], Moves: false, Weight: 0.8f),
        ],
        Core: 0xf0fff2, Rim: 0x5fdc7c, Glow: 0x2fbf5a, IconMotion.Sway, Pivot: (.5f, .77f));

    /// <summary>What a charging station takes: a battery, charge's green, its three bars lit from the bottom one by one.</summary>
    public static readonly Look Charges = new(Ring,
        [
            new Stroke([(.37f, .3f), (.63f, .3f), (.63f, .77f), (.37f, .77f), (.37f, .3f)], Moves: false),
            new Stroke([(.46f, .23f), (.54f, .23f)], Moves: false),
            new Stroke([(.445f, .67f), (.555f, .67f)], Weight: 0.9f, Step: 1),
            new Stroke([(.445f, .54f), (.555f, .54f)], Weight: 0.9f, Step: 2),
            new Stroke([(.445f, .41f), (.555f, .41f)], Weight: 0.9f, Step: 3),
        ],
        Core: 0xf2ffee, Rim: 0x7ee36b, Glow: 0x4cc23a, IconMotion.Fill);

    /// <summary>Where a drone docks: a quad from above, its lamp's sky, the blades in its four rotors' rings spinning.</summary>
    public static readonly Look Docks = new(Ring,
        [
            new Stroke([(.45f, .45f), (.405f, .405f)], Moves: false, Weight: 0.8f),
            new Stroke([(.55f, .45f), (.595f, .405f)], Moves: false, Weight: 0.8f),
            new Stroke([(.55f, .55f), (.595f, .595f)], Moves: false, Weight: 0.8f),
            new Stroke([(.45f, .55f), (.405f, .595f)], Moves: false, Weight: 0.8f),
            new Stroke(Circle(.5f, .5f, .065f, 10), Moves: false),
            new Stroke(Circle(.33f, .33f, .105f, 12), Moves: false, Weight: 0.7f),
            new Stroke(Circle(.67f, .33f, .105f, 12), Moves: false, Weight: 0.7f),
            new Stroke(Circle(.67f, .67f, .105f, 12), Moves: false, Weight: 0.7f),
            new Stroke(Circle(.33f, .67f, .105f, 12), Moves: false, Weight: 0.7f),
            new Stroke([(.27f, .33f), (.39f, .33f)], Weight: 0.75f),
            new Stroke([(.61f, .33f), (.73f, .33f)], Weight: 0.75f),
            new Stroke([(.61f, .67f), (.73f, .67f)], Weight: 0.75f),
            new Stroke([(.27f, .67f), (.39f, .67f)], Weight: 0.75f),
        ],
        Core: 0xeefaff, Rim: 0x7dd3fc, Glow: 0x38bdf8, IconMotion.Spin);
    public static readonly IReadOnlyDictionary<SlotMark, Look> All = new Dictionary<SlotMark, Look>
    {
        [SlotMark.Smelts] = Smelts,
        [SlotMark.Burns] = Burns,
        [SlotMark.Grows] = Grows,
        [SlotMark.Charges] = Charges,
        [SlotMark.Docks] = Docks,
    };

    /// <summary>Mark <paramref name="mark"/>'s icon; throws for <see cref="SlotMark.None"/> and for <see cref="SlotMark.Fits"/>, whose icon is its module's tech's.</summary>
    public static Look Of(SlotMark mark) => All[mark];
}
