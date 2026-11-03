using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// A key on the tablet's top edge (<see cref="TabletEdge"/>) whose cap is a frosted lens, the research key's, its motif a shade in the
/// glass (2026-11-03-edge-lenses): brighter under the pointer, sunk a pixel while held, lit sky from within while what it switches is
/// on, its shade sunk into dark glass and deaf to a press while what it does cannot be done (2026-11-03-the-regen-suit).
/// </summary>
public partial class EdgeKey : WristKey
{
    private readonly string _name;
    private readonly Action _pressed;
    private readonly Func<bool> _on;
    private bool _wasOn, _wasDim;

    /// <param name="name">Its pictures' name: <c>key_&lt;name&gt;_&lt;state&gt;</c>, drawn by <c>tools/anime-art/subjects/edgekeys.js</c>.</param>
    /// <param name="pressed">What a press does.</param>
    /// <param name="on">Whether what it switches is on; a key that switches nothing has no lit picture and passes none.</param>
    /// <param name="why">Why it can do nothing now, an i18n key its tip says under its name; null, or none given, while it can.</param>
    public EdgeKey(string name, Action pressed, Func<bool>? on = null, Func<string?>? why = null) : base(new Vector2(WristLook.TabHeight, WristLook.TabHeight))
    {
        (_name, _pressed, _on, Why) = (name, pressed, on ?? (() => false), why ?? (() => null));
        TextureFilter = TextureFilterEnum.Nearest;
    }

    /// <summary>Why it can do nothing now, an i18n key; null while it can.</summary>
    public Func<string?> Why { get; }

    protected override void Pressed()
    {
        if (Why() == null) _pressed();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        bool on = _on(), dim = Why() != null;
        if (on == _wasOn && dim == _wasDim) return;
        (_wasOn, _wasDim) = (on, dim);
        QueueRedraw();
    }

    protected override void DrawCap(Rect2 face)
    {
        string state = State switch
        {
            _ when _wasDim => "dim",
            KeyState.Down => "down",
            _ when _wasOn => "on",
            KeyState.Hover => "hover",
            _ => "idle",
        };
        DrawRect(face.Grow(1), new Color("0e1014"));
        DrawTextureRect(PixelArt.Picture($"key_{_name}_{state}"), face, false);
    }
}
