using System;
using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Shared;

/// <summary>
/// A feature's hold on the one tooltip (<see cref="Ui.Tip"/>): the controls it says something of, found
/// under the pointer by a hit test every frame, since Godot sends no mouse_entered to a control rebuilt
/// under a still pointer, nor while a button is held. It takes down only the tip it set itself.
/// </summary>
public sealed class TipSpots(Store<Tip?> shown)
{
    private readonly List<(Control Control, Func<Tip?> Tip)> _spots = [];
    private Tip? _mine;

    /// <summary>Says <paramref name="tip"/> while the pointer is on <paramref name="control"/>; a freed control drops out.</summary>
    public void Add(Control control, Func<Tip?> tip)
    {
        _spots.RemoveAll(s => !GodotObject.IsInstanceValid(s.Control));
        _spots.Add((control, tip));
    }

    /// <summary>Forgets every control, before they are built again.</summary>
    public void Clear() => _spots.Clear();

    /// <summary>Shows the tip of the control under <paramref name="pointer"/>, on the screen; none off the window or under the menu.</summary>
    public void Frame(Vector2? pointer) => Show(pointer is { } at ? Under(at) : null);

    /// <summary>Shows <paramref name="tip"/>, or with none takes down this feature's own.</summary>
    public void Show(Tip? tip)
    {
        if (tip != null)
        {
            _mine = tip;
            shown.Set(tip);
            return;
        }
        if (_mine != null && Equals(shown.Get(), _mine)) shown.Set(null);
        _mine = null;
    }

    private Tip? Under(Vector2 at)
    {
        for (int i = _spots.Count - 1; i >= 0; i--)
        {
            var (control, tip) = _spots[i];
            if (GodotObject.IsInstanceValid(control) && control.IsVisibleInTree() && control.GetGlobalRect().HasPoint(at)) return tip();
        }
        return null;
    }
}
