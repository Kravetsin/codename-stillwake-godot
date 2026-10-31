using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Creatures;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// A creature with no art yet (<see cref="CreatureLooks.Boxes"/>), drawn where the sim put it as a plain box whose colour says what it
/// does: dark asleep, yellow up, red crouched, white flying, teal stuck.
/// </summary>
public partial class PlaceholderBodies : Node2D
{
    private const float Cell = ShipCamera.Cell;
    private const float Unit = CreaturesModule.Unit;

    private static readonly Color Asleep = new(0.2f, 0.2f, 0.25f), Up = new(0.95f, 0.8f, 0.2f), Crouched = new(0.9f, 0.2f, 0.15f),
        Flying = new(0.95f, 0.95f, 0.95f), Stuck = new(0.2f, 0.75f, 0.7f);

    private readonly List<(Rect2 Box, Color Colour)> _boxes = [];

    /// <summary>Shows <paramref name="creatures"/> as they are this frame.</summary>
    public void Frame(IReadOnlyList<Creature> creatures)
    {
        if (creatures.Count == 0 && _boxes.Count == 0) return;
        _boxes.Clear();
        foreach (var c in creatures)
        {
            var body = Spec.Creatures.Get(c.Species).Body;
            var size = new Vector2(body.Width, body.Height) * Cell;
            var feet = new Vector2(c.X, c.Y) / Unit * Cell;
            _boxes.Add((new Rect2(feet - new Vector2(size.X / 2, size.Y), size), ColourOf(c)));
        }
        QueueRedraw();
    }

    private static Color ColourOf(Creature c) =>
        CreaturesModule.Asleep(c) ? Asleep
        : CreaturesModule.Stuck(c) ? Stuck
        : c.Posture == Posture.WindUp ? Crouched
        : c.Posture == Posture.Charge ? Flying
        : Up;

    public override void _Draw()
    {
        foreach (var (box, colour) in _boxes) DrawRect(box, colour);
    }
}
