using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Handbook;

/// <summary>
/// A section of the Handbook: its name and icon in the side list, and its body, a control the feature it speaks of
/// builds and keeps current while it is shown (the debris feature's <c>HandbookPage</c>).
/// </summary>
public sealed record HandbookSection(string Name, Texture2D Icon, Control Body);

/// <summary>
/// The Handbook (0066): a big window in the middle of the screen, J opening and closing it, Esc, Tab or E closing it;
/// on its left the sections, one a row with its icon and name, on its right the one chosen. Read only: it changes
/// nothing in the world, which runs on behind it. Opening it puts away what is open or in hand, as <c>Main</c> says
/// (<see cref="Opening"/>), so no feature reaches into another's.
/// </summary>
public partial class HandbookPanel : CanvasLayer
{
    private static readonly StyleBoxFlat Frame = Style.Box(Style.Slate900 with { A = 0.97f }, Style.Slate700, 12);
    private static readonly StyleBoxFlat Chosen = Style.Box(Style.Slate700, Style.Slate400, 6, 3);
    private static readonly StyleBoxFlat Unchosen = Style.Box(Colors.Transparent, Colors.Transparent, 6, 3);
    private static readonly StyleBoxFlat Hovered = Style.Box(Style.Slate800, Style.Slate600, 6, 3);
    private static readonly StyleBoxFlat Keycap = Keycapped();
    private static readonly LabelSettings TitleText = Style.Text(Style.Bold, 14, Style.Slate100);
    private static readonly LabelSettings SectionText = Style.Text(Style.Sans, 13, Style.Slate100);

    private readonly Ui _ui;
    private readonly Func<bool> _inWorld;
    private readonly IReadOnlyList<HandbookSection> _sections;
    private readonly List<PanelContainer> _rows = [];
    private readonly PanelContainer _window = new() { MouseFilter = Control.MouseFilterEnum.Stop };
    private int _chosen;

    /// <param name="ui">The hand, freed as it opens, and the menu, under which it takes no key.</param>
    /// <param name="inWorld">Whether a world is played: on the title J does nothing.</param>
    /// <param name="sections">Its sections, in the side list's order: the first shown first.</param>
    public HandbookPanel(Ui ui, Func<bool> inWorld, IReadOnlyList<HandbookSection> sections)
    {
        (_ui, _inWorld, _sections) = (ui, inWorld, sections);
        Layer = ScreenLayers.Handbook;
        Visible = false;
        var centre = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        centre.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(centre);
        _window.AddThemeStyleboxOverride("panel", Frame);
        centre.AddChild(_window);
        var column = Style.Gap(new VBoxContainer(), 10);
        _window.AddChild(column);
        column.AddChild(Head());
        var split = Style.Gap(new HBoxContainer(), 14);
        var list = Style.Gap(new VBoxContainer { CustomMinimumSize = new Vector2(150, 0) }, 4);
        for (int i = 0; i < sections.Count; i++) list.AddChild(Row(i));
        split.AddChild(list);
        split.AddChild(new ColorRect { Color = Style.Slate800, CustomMinimumSize = new Vector2(1, 0), MouseFilter = Control.MouseFilterEnum.Ignore });
        foreach (var section in sections)
        {
            section.Body.Visible = false;
            split.AddChild(section.Body);
        }
        column.AddChild(split);
        Choose(0);
    }

    /// <summary>What opening it puts away first: the inventory and the machine open, the plan. Set by <c>Main</c>.</summary>
    public Action? Opening { get; set; }

    /// <summary>Whether it is open: what Esc closes before it opens the menu.</summary>
    public bool Shown => Visible;

    /// <summary>Opens it on the section chosen last, putting away what is open or in hand.</summary>
    public void Open()
    {
        if (Visible) return;
        Opening?.Invoke();
        _ui.Hand.Set(Holding.Free);
        Visible = true;
    }

    /// <summary>Closes it: J again, Esc through the menu's <c>Escape</c>, Tab or E.</summary>
    public void Close() => Visible = false;

    /// <summary>The window's header: the book, its title, and the key that opens it.</summary>
    private static HBoxContainer Head()
    {
        var head = Style.Gap(new HBoxContainer(), 8);
        head.AddChild(Picture(AsciiArt.Book(), 24));
        head.AddChild(new Label { Text = T("handbook.title"), LabelSettings = TitleText, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center });
        var key = new PanelContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, MouseFilter = Control.MouseFilterEnum.Ignore };
        key.AddThemeStyleboxOverride("panel", Keycap);
        key.AddChild(new Label { Text = "J", LabelSettings = Style.Text(Style.Bold, 12, Style.Slate300) });
        head.AddChild(key);
        return head;
    }

    /// <summary><c>border border-slate-600 border-b-2 rounded px-1</c>: a key drawn as a keycap.</summary>
    private static StyleBoxFlat Keycapped()
    {
        var box = Style.Box(Colors.Transparent, Style.Slate600, 0, 3);
        box.BorderWidthBottom = 2;
        (box.ContentMarginLeft, box.ContentMarginRight, box.ContentMarginTop, box.ContentMarginBottom) = (6, 6, 0, 1);
        return box;
    }

    /// <summary>Section <paramref name="index"/>'s row in the side list: its icon and name, chosen by a click.</summary>
    private PanelContainer Row(int index)
    {
        var section = _sections[index];
        var row = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Stop, MouseDefaultCursorShape = Control.CursorShape.PointingHand };
        var line = Style.Gap(new HBoxContainer(), 8);
        line.AddChild(Picture(section.Icon, 24));
        line.AddChild(new Label { Text = section.Name, LabelSettings = SectionText, VerticalAlignment = VerticalAlignment.Center });
        row.AddChild(line);
        row.GuiInput += e =>
        {
            if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) Choose(index);
        };
        row.MouseEntered += () => Paint(row, index, true);
        row.MouseExited += () => Paint(row, index, false);
        _rows.Add(row);
        return row;
    }

    private void Choose(int index)
    {
        if (_sections.Count == 0) return;
        _chosen = index;
        for (int i = 0; i < _sections.Count; i++)
        {
            _sections[i].Body.Visible = i == index;
            Paint(_rows[i], i, false);
        }
    }

    private void Paint(PanelContainer row, int index, bool hovered) =>
        row.AddThemeStyleboxOverride("panel", index == _chosen ? Chosen : hovered ? Hovered : Unchosen);

    /// <summary>A picture <paramref name="size"/> wide and high at most, its pixels square, centred.</summary>
    private static TextureRect Picture(Texture2D texture, int size) => new()
    {
        Texture = texture,
        CustomMinimumSize = new Vector2(size, size),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

    /// <summary>J opens and closes it, in a world and with the menu shut; Tab and E close it and go on to what they do.</summary>
    public override void _Input(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false } key || !Visible) return;
        // Not taken: Tab opens the inventory, E works what is in reach.
        if (key.PhysicalKeycode is Key.Tab or Key.E) Close();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.J }) return;
        if (!_inWorld() || _ui.MenuOpen.Get()) return;
        if (Visible) Close();
        else Open();
        GetViewport().SetInputAsHandled();
    }
}
