using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// A window drawn as the drone's hologram (holo windows), the build menu's, the Handbook's, the board's and the game menu's: its
/// screen this panel, the drone's blue glass with a machine window's buzzing frame round it, no bezel and no glare. It fades in where
/// it rests and its screen comes on as <see cref="TabletMotion"/> says; where it rests is its owner's, above the keys (<see cref="TabletPeek.Floor"/>).
/// </summary>
public partial class Tablet : Panel
{
    private static readonly Color Ground = new(HoloLook.Drone.Ground);

    /// <summary>The tablet's text: the strips' size, and the hint's along the foot.</summary>
    public const int FontSize = 12, HintSize = 11;

    private readonly TabletMotion _motion = new();
    /// <summary>Round the glass, behind it: it takes the clicks and the wheel at the frame's edge as the glass does.</summary>
    private readonly Control _edge = new() { MouseFilter = MouseFilterEnum.Stop, ShowBehindParent = true };
    /// <summary>The frame's light, added over the glass, under what it shows.</summary>
    private readonly BuzzFrame _frame = new();
    /// <summary>What the screen shows, faded in as it comes on, and its holder.</summary>
    private readonly List<CanvasItem> _shown = [];
    private readonly Control _onScreen = new() { MouseFilter = MouseFilterEnum.Ignore };

    /// <summary>How far round the glass its edge still takes the pointer.</summary>
    private const int EdgeReach = 6;

    public Tablet()
    {
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
        AddThemeStyleboxOverride("panel", Style.Box(Ground with { A = HoloLook.GroundAlpha }, Ground with { A = HoloLook.GroundAlpha }, radius: 0));
        AddChild(_edge);
        AddChild(_frame);
        AddChild(_onScreen);
    }

    /// <summary>The edge round the glass: it takes the clicks and the wheel as the glass does, its owner listening to both.</summary>
    public Control Bezel => _edge;

    /// <summary>Where the window lies on the screen, its edge too: what the crew's markers keep out of.</summary>
    public Rect2 Covers => _edge.GetGlobalRect();

    /// <summary>
    /// The drone's windows, for a tablet the drone projects (drone-windows): it comes and goes by the drone's corners
    /// (<see cref="CornerClock"/>), handing its place to them. Null for one that is not the drone's, the game menu's.
    /// </summary>
    public DroneWindows? Corners { get; set; }

    private readonly CornerClock _clock = new();

    /// <summary>How far it has come (<see cref="TabletMotion.Up"/>, or its corners' screen): 0 gone, 1 whole.</summary>
    public double Up => Corners != null ? _clock.Screen : _motion.Up;

    /// <summary>Whether any of it is on the screen: out, or still fading.</summary>
    public bool Shown => Corners != null ? _clock.Screen > 0 : _motion.Shown;

    /// <summary>Puts <paramref name="part"/> on the screen, over the glass, faded in with it as it comes on.</summary>
    public void OnScreen(Control part)
    {
        _onScreen.AddChild(part);
        _shown.Add(part);
    }

    /// <summary>
    /// One frame of <paramref name="seconds"/>, the window out or not: faded and its screen lit (<see cref="TabletMotion"/>).
    /// Put away, it takes no more input at once, though it shows while it fades.
    /// </summary>
    public void Step(bool open, double seconds)
    {
        _motion.Step(open, seconds);
        if (Corners != null)
        {
            // Its owner lays it only once it shows: handed from then on, the corners flying to where it was laid.
            _clock.Step(seconds, open);
            if (GetGlobalRect() is { Size: { X: > 0, Y: > 0 } } at) Corners.Hand(this, (at.Position.X, at.Position.Y, at.Size.X, at.Size.Y), _clock);
        }
        Visible = Shown;
        var mouse = open ? MouseBehaviorRecursiveEnum.Inherited : MouseBehaviorRecursiveEnum.Disabled;
        if (MouseBehaviorRecursive != mouse) MouseBehaviorRecursive = mouse;
        Modulate = Colors.White with { A = (float)Up };
        float power = _frame.Alpha = Corners != null ? _clock.Screen : (float)_motion.Power;
        foreach (var part in _shown) part.Modulate = Colors.White with { A = power };
    }

    /// <summary>Lays the window for this frame, its screen at (<paramref name="left"/>, <paramref name="top"/>), <paramref name="width"/> by <paramref name="tall"/>.</summary>
    public void Lay(float left, float top, float width, float tall)
    {
        Position = new Vector2(left, top);
        Size = new Vector2(width, tall);
        _onScreen.Position = Vector2.Zero;
        _onScreen.Size = Size;
        _edge.Position = -Vector2.One * EdgeReach;
        _edge.Size = Size + Vector2.One * 2 * EdgeReach;
    }

    /// <summary>A line of the tablet's text: monospaced, <paramref name="size"/> px, centred down its line.</summary>
    public static Label Text(string text, Color colour, int size = FontSize) => new()
    {
        Text = text,
        LabelSettings = new LabelSettings { Font = Style.Mono, FontSize = size, FontColor = colour },
        MouseFilter = MouseFilterEnum.Ignore,
        VerticalAlignment = VerticalAlignment.Center,
    };

    /// <summary>A tab of the strip at <paramref name="x"/>: its name and, when it has one, its count; painted by <see cref="PaintTab"/>.</summary>
    public static PanelContainer Tab(string name, string? count, float x)
    {
        var row = Style.Gap(new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore }, 6);
        row.AddChild(Text(name, Style.Slate300));
        float width = 6 + Style.Mono.GetStringSize(name, fontSize: FontSize).X + 6;
        if (count != null)
        {
            row.AddChild(Text(count, Style.Slate500));
            width += Style.Mono.GetStringSize(count, fontSize: FontSize).X + 6;
        }
        var box = new PanelContainer { MouseFilter = MouseFilterEnum.Pass, Position = new Vector2(x, 0) };
        box.AddChild(row);
        box.CustomMinimumSize = new Vector2(Mathf.Ceil(width), TabletLayout.TabHeight);
        return box;
    }

    /// <summary>A tab lit sky when <paramref name="on"/>, a shade lighter under the pointer, its name white and its count pale sky when lit.</summary>
    public static void PaintTab(PanelContainer tab, bool on, bool hovered)
    {
        var style = Style.Box(on ? Style.Sky700 : hovered ? Style.Slate700 : Style.Slate800, on ? Style.Sky700 : Style.Slate800);
        (style.ContentMarginLeft, style.ContentMarginRight) = (6, 6);
        tab.AddThemeStyleboxOverride("panel", style);
        var labels = tab.GetChild(0).GetChildren();
        ((Label)labels[0]).LabelSettings.FontColor = on ? Colors.White : Style.Slate300;
        if (labels.Count > 1) ((Label)labels[1]).LabelSettings.FontColor = on ? Style.Sky200 : Style.Slate500;
    }

    /// <summary>A scrollbar's track and its thumb in it, the thumb shown only while there is more to scroll to.</summary>
    public static (Panel Track, Panel Thumb) ScrollBar()
    {
        var track = new Panel { MouseFilter = MouseFilterEnum.Ignore };
        var thumb = new Panel { MouseFilter = MouseFilterEnum.Ignore };
        track.AddThemeStyleboxOverride("panel", Style.Box(Style.Slate800, Style.Slate800, radius: 3));
        thumb.AddThemeStyleboxOverride("panel", Style.Box(Style.Slate600, Style.Slate600, radius: 3));
        track.AddChild(thumb);
        return (track, thumb);
    }

    /// <summary>Lays <paramref name="thumb"/> at <paramref name="at"/> down its track (<see cref="TabletLayout.Thumb"/>), or hides it with none.</summary>
    public static void PlaceThumb(Panel thumb, (float Top, float Length)? at)
    {
        thumb.Visible = at != null;
        if (at is not { } bar) return;
        thumb.Position = new Vector2(0, Mathf.Round(bar.Top));
        thumb.Size = new Vector2(TabletLayout.BarWidth, Mathf.Round(bar.Length));
    }
}
