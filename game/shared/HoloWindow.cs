using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// A window drawn as a hologram (<see cref="MachineHolo"/>): a machine's frame or the paired bag. It has no ground of its own;
/// its owner shows its controls as the hologram reveals them (the grid's wipe from the near edge, their alpha) and carries it
/// off its place by the sway (<see cref="HoloSway"/>), the controls themselves, so clicks land on what is drawn. Its margins
/// are the wrist screen's, so every slot keeps its place (0074).
/// </summary>
public partial class HoloWindow : PanelContainer
{
    private Container? _sorter;
    /// <summary>How far in the wipe has come while it clips, from <see cref="_from"/>; null with the whole window shown.</summary>
    private float? _reach;
    private HoloEdge _from;
    /// <summary>Where it last put itself: a container laying it out moves it elsewhere.</summary>
    private Vector2 _placed;

    public HoloWindow()
    {
        SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        MouseFilter = MouseFilterEnum.Stop;
        var margins = new StyleBoxEmpty();
        margins.SetContentMarginAll(8);
        margins.ContentMarginLeft = margins.ContentMarginRight = Slots.ScreenPadding;
        AddThemeStyleboxOverride("panel", margins);
    }

    /// <summary>How far it is carried off the place its parent gave it, in pixels.</summary>
    public Vector2 Offset { get; private set; }

    /// <summary>Its controls as the hologram shows them: their alpha, and the wipe clipping them while it crosses the window.</summary>
    public void Reveal(HoloReveal reveal)
    {
        Modulate = Colors.White with { A = reveal.Alpha };
        float? reach = reveal.Alpha > 0 && reveal.Reach < HoloPen.Depth(new HoloRect(0, 0, Size.X, Size.Y), reveal.From) + HoloLook.WipePast / 2
            ? reveal.Reach : null;
        if (reach == _reach && reveal.From == _from) return;
        (_reach, _from) = (reach, reveal.From);
        // A clip renders its children apart: only while the wipe crosses the window.
        ClipChildren = reach == null ? ClipChildrenMode.Disabled : ClipChildrenMode.Only;
        QueueRedraw();
    }

    /// <summary>Carried <paramref name="offset"/> off its place from now on.</summary>
    public void Sway(Vector2 offset)
    {
        if (offset == Offset) return;
        Moved(Position + offset - Offset);
        Offset = offset;
    }

    /// <summary>Its place, <paramref name="at"/>, for an owner that places it itself rather than a container.</summary>
    public void Place(Vector2 at) => Moved(at + Offset);

    /// <summary>Whether <paramref name="control"/> is in a hologram window: the pointer over it calms the sway.</summary>
    public static bool Holds(Control? control)
    {
        for (Node? node = control; node != null; node = node.GetParent())
            if (node is HoloWindow) return true;
        return false;
    }

    public override void _EnterTree()
    {
        if (GetParent() is not Container sorter) return;
        _sorter = sorter;
        sorter.SortChildren += Sorted;
    }

    public override void _ExitTree()
    {
        if (_sorter != null) _sorter.SortChildren -= Sorted;
        _sorter = null;
    }

    /// <summary>Laid out again by its container, back at its place: carried off it again. One it did not move (hidden) keeps where it is.</summary>
    private void Sorted()
    {
        if (Position != _placed) Moved(Position + Offset);
    }

    private void Moved(Vector2 to) => Position = _placed = to;

    public override void _Draw()
    {
        // The mask its controls are clipped to: the part the wipe has crossed.
        if (_reach is not float reach) return;
        var shown = HoloPen.Band(new HoloRect(0, 0, Size.X, Size.Y), _from, -HoloLook.WipePast, reach, HoloLook.WipePast);
        if (shown.W > 0 && shown.H > 0) DrawRect(new Rect2(shown.X, shown.Y, shown.W, shown.H), Colors.White);
    }
}
