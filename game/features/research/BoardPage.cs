using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.I18n;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using static Stillwake.Game.Features.Research.StripParts;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The tablet's first page: the board as fixed district islands (board-islands). Every thing has its frame in its district's block from the
/// start (<see cref="BoardLayout"/>), unknown, almost or known (<see cref="BoardView"/>); hovering lights its ties, a toggle its whole chain. Finds arrive as
/// batches in the list beside it (<see cref="BoardSeen"/>), new frames glowing until hovered. A click picks and says what is known of it in the strip below,
/// whose pin button makes it the goal: its chain glows and its missing pieces are listed (<see cref="BoardChain"/>). Dragged and zoomed
/// (<see cref="TechCamera"/>), never rearranged. It buys nothing, for it takes no way to send a command.
/// </summary>
public sealed class BoardPage : ITabletPage
{
    private static readonly Color Screen = new("0d1626"), PanelFill = new(9 / 255f, 17 / 255f, 31 / 255f, 0.94f), LineColour = new("22385a"), LineStrong = new("35557f"),
        Bright = new("d3e4ff"), Dim = new("7d93b5"), Faint = new("44597a"), Accent = new("7cc7ff"), Out = new("b9a2ff"), Amber = new("ffb547"), Ok = new("6fe3a1"),
        Chain = new("a6f26b"), GridLine = new(120 / 255f, 175 / 255f, 240 / 255f, 0.07f), KnownFill = new(18 / 255f, 36 / 255f, 62 / 255f, 0.92f),
        AlmostFill = new(14 / 255f, 28 / 255f, 48 / 255f, 0.7f), UnknownEdge = new(80 / 255f, 110 / 255f, 150 / 255f, 0.28f),
        MarketEdge = new(185 / 255f, 162 / 255f, 255 / 255f, 0.28f), Mark = new(110 / 255f, 140 / 255f, 180 / 255f, 0.32f), Veil = new(7 / 255f, 14 / 255f, 26 / 255f, 0.55f),
        Dimmed = new(1, 1, 1, 0.3f);

    /// <summary>Each district's hue, in degrees.</summary>
    private static readonly Dictionary<string, float> Hues = new(StringComparer.Ordinal)
    {
        ["raw"] = 28,
        ["smelt"] = 4,
        ["parts"] = 205,
        ["machines"] = 265,
        ["walls"] = 222,
        ["farm"] = 128,
        ["gear"] = 172,
    };

    /// <summary>A silhouette: the picture's shape alone, in the screen's blue.</summary>
    private static readonly Shader Silhouette = new()
    {
        Code = """
            shader_type canvas_item;
            void fragment() {
                COLOR = vec4(0.373, 0.502, 0.678, texture(TEXTURE, UV).a * COLOR.a * 0.85);
            }
            """,
    };

    /// <summary>The zooms from which names show, captions sit at a block's corner, column heads show, and frames are more than marks.</summary>
    private const double WordsFrom = 0.58, CaptionsFrom = 0.42, MarksBelow = 0.24;

    /// <summary>The room round the blocks, the list of finds' width, the goal's, and the gap to the board's edge.</summary>
    private const float Margin = 60, SideWide = 300, GoalWide = 280, Edge = 10;

    /// <summary>Seconds of a glide, and the zoom it comes to at least, names showing.</summary>
    private const double Glide = 0.45, Close = 0.9;

    /// <summary>A frame on the board: what it is drawn with, kept so a read restyles only what changed.</summary>
    private sealed class FrameView(string id, BoardFrame frame, Control root, TextureRect picture, Label name, Label need)
    {
        public string Id => id;
        public BoardFrame Frame => frame;
        public Control Root => root;
        public TextureRect Picture => picture;
        public Label Name => name;
        public Label Need => need;
        public BoardState State { get; set; } = (BoardState)(-1);
        public string? NeedOf { get; set; }
        public (int Had, int Of) Parts { get; set; }
        public bool Glows { get; set; }
    }

    private readonly Control _view;
    private readonly StripParts _strip;
    private readonly Action _readSoon;
    private readonly Func<uint> _worldId;
    private readonly Func<World?> _world;
    private readonly BoardKeep _keep;
    private readonly BoardLayout _layout = BoardLayout.Main;
    private readonly TechCamera _camera = new() { Floor = 0.12 };
    // The board's part of the view, left of the finds: nothing on it shows past it.
    private readonly Control _board = new() { MouseFilter = Control.MouseFilterEnum.Ignore, ClipContents = true, Visible = false };
    // The screen, the grid, the blocks and their words, and the ties: under the frames.
    private readonly Control _back = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly Control _frames = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly Dictionary<string, FrameView> _views = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StyleBoxFlat> _blockBoxes = new(StringComparer.Ordinal);
    private readonly StyleBoxFlat _frameBox = new(), _glowBox = new(), _veilBox = new() { BgColor = Veil };
    // The finds beside the board.
    private readonly PanelContainer _side = new() { MouseFilter = Control.MouseFilterEnum.Stop, Visible = false };
    private readonly VBoxContainer _rows = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly ScrollContainer _scroll = new() { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
    // The goal pinned, over the board's top left; the chain toggle and the legend at its bottom left.
    private readonly PanelContainer _goal = new() { MouseFilter = Control.MouseFilterEnum.Stop, Visible = false };
    private readonly PanelContainer _legend = new() { MouseFilter = Control.MouseFilterEnum.Stop, Visible = false };
    private readonly Button _wholeChain;
    private readonly BoardFind _find;
    private BoardRead? _read;
    private object? _knowledge;
    private string? _key;
    private (double X, double Y, double Zoom)? _placedAt;
    private double? _sizedAt;
    private Vector2 _viewWas;
    private bool _out, _pressed, _framed, _built, _looked;
    private float _moved;
    private string? _picked, _hover;
    private double _time;
    private Tween? _glide;
    private int _rowsFor = -1;
    private int _unreadFor = -1;
    private string? _goalFor;

    /// <summary>What the search's open list shows: lit on the board, the rest dimmed; none while it is closed.</summary>
    private IReadOnlyCollection<string>? _listed;

    /// <summary>The board's part of the view, left of the finds.</summary>
    private float Wide => Math.Max(0, _view.Size.X - SideWide);

    /// <summary>The board's own size, its blocks and the room round them.</summary>
    private double MapWide => _layout.Wide + 2 * Margin;

    private double MapTall => _layout.Tall + 2 * Margin;

    /// <param name="view">The tablet's view, which it draws into.</param>
    /// <param name="strip">The strip below and its words.</param>
    /// <param name="readSoon">Has the tablet read on its next frame.</param>
    /// <param name="worldId">Which world is played: what the board is kept under.</param>
    /// <param name="world">The world played, all its surfaces: a machine on any counts; null with none.</param>
    /// <param name="keep">What is kept of the board, shared with the tablet's key.</param>
    public BoardPage(Control view, StripParts strip, Action readSoon, Func<uint> worldId, Func<World?> world, BoardKeep keep)
    {
        (_view, _strip, _readSoon, _worldId, _world, _keep) = (view, strip, readSoon, worldId, world, keep);
        _back.Draw += DrawBack;
        _board.AddChild(_back);
        _board.AddChild(_frames);
        view.AddChild(_board);
        foreach (var block in _layout.Blocks)
        {
            float hue = Hues.GetValueOrDefault(block.District);
            _blockBoxes[block.District] = new StyleBoxFlat { BgColor = Hsl(hue, 0.55f, 0.45f, 0.075f), BorderColor = Hsl(hue, 0.6f, 0.62f, 0.32f), AntiAliasing = true };
            _blockBoxes[block.District].SetBorderWidthAll(1);
        }
        _frameBox.SetBorderWidthAll(1);
        _frameBox.AntiAliasing = true;
        _glowBox.DrawCenter = false;
        _glowBox.SetBorderWidthAll(2);
        _glowBox.ShadowColor = Amber with { A = 0.6f };
        BuildSide();
        view.AddChild(_side);
        _goal.AddThemeStyleboxOverride("panel", Box(PanelFill, Chain, 10, 10, Chain with { A = 0.12f }));
        view.AddChild(_goal);
        _wholeChain = ScreenButton(T("research.board.chain"), () => _back.QueueRedraw());
        _wholeChain.ToggleMode = true;
        BuildLegend();
        view.AddChild(_legend);
        _find = new BoardFind(Findable, _strip.Picture, Find, Listed) { Visible = false };
        view.AddChild(_find);
        view.MouseExited += () => Hover(null);
    }

    /// <summary>The list of finds, built once: its rows change as batches come.</summary>
    private void BuildSide()
    {
        _side.AddThemeStyleboxOverride("panel", Box(PanelFill, LineColour, 0, 0));
        var column = Style.Gap(new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 0);
        var head = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 8);
        var title = Tablet.Text(T("research.finds").ToUpperInvariant(), Accent, 12);
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        title.VerticalAlignment = VerticalAlignment.Center;
        head.AddChild(title);
        head.AddChild(ScreenButton(T("research.finds.readAll"), ReadAll));
        var top = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        foreach (string side in (ReadOnlySpan<string>)["left", "right", "top", "bottom"]) top.AddThemeConstantOverride($"margin_{side}", side == "bottom" ? 8 : 12);
        top.AddChild(head);
        column.AddChild(top);
        var rule = new ColorRect { Color = LineColour, CustomMinimumSize = new Vector2(0, 1), MouseFilter = Control.MouseFilterEnum.Ignore };
        column.AddChild(rule);
        var inside = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        foreach (string side in (ReadOnlySpan<string>)["left", "right", "top", "bottom"]) inside.AddThemeConstantOverride($"margin_{side}", 8);
        Style.Gap(_rows, 6);
        _rows.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        inside.AddChild(_rows);
        _scroll.AddChild(inside);
        column.AddChild(_scroll);
        _side.AddChild(column);
    }

    /// <summary>The chain toggle and what the colours of the ties mean.</summary>
    private void BuildLegend()
    {
        _legend.AddThemeStyleboxOverride("panel", Box(PanelFill with { A = 0.7f }, LineColour, 6, 6));
        var line = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 12);
        line.AddChild(_wholeChain);
        foreach (var (key, colour) in (ReadOnlySpan<(string, Color)>)[("research.board.legendFrom", Accent), ("research.board.legendInto", Out), ("research.board.legendGoal", Chain), ("research.board.legendNew", Amber)])
        {
            var one = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 4);
            one.AddChild(new ColorRect { Color = colour, CustomMinimumSize = new Vector2(18, 3), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, MouseFilter = Control.MouseFilterEnum.Ignore });
            var words = Tablet.Text(T(key), Dim, Tablet.HintSize);
            words.VerticalAlignment = VerticalAlignment.Center;
            one.AddChild(words);
            line.AddChild(one);
        }
        _legend.AddChild(line);
    }

    /// <summary>A button of the screen's look.</summary>
    private static Button ScreenButton(string text, Action pressed)
    {
        var button = new Button { Text = text, FocusMode = Control.FocusModeEnum.None };
        button.AddThemeStyleboxOverride("normal", Box(new Color("12233b"), LineStrong, 6, 4));
        button.AddThemeStyleboxOverride("hover", Box(new Color("12233b"), Accent, 6, 4));
        button.AddThemeStyleboxOverride("pressed", Box(new Color("173458"), Accent, 6, 4));
        button.AddThemeStyleboxOverride("hover_pressed", Box(new Color("173458"), Accent, 6, 4));
        button.AddThemeFontOverride("font", Style.Clear);
        button.AddThemeFontSizeOverride("font_size", 13);
        foreach (string state in (ReadOnlySpan<string>)["font_color", "font_hover_color", "font_pressed_color", "font_hover_pressed_color"]) button.AddThemeColorOverride(state, Bright);
        button.Pressed += pressed;
        return button;
    }

    private static StyleBoxFlat Box(Color fill, Color edge, int radius, int pad, Color? shadow = null)
    {
        var box = new StyleBoxFlat { BgColor = fill, BorderColor = edge, AntiAliasing = true };
        box.SetBorderWidthAll(1);
        box.SetCornerRadiusAll(radius);
        box.SetContentMarginAll(pad);
        (box.ContentMarginLeft, box.ContentMarginRight) = (pad + 4, pad + 4);
        if (shadow is { } glow) (box.ShadowColor, box.ShadowSize) = (glow, 24);
        return box;
    }

    /// <summary>A colour by hue, saturation and lightness, as the mockup gave them.</summary>
    private static Color Hsl(float hue, float saturation, float lightness, float alpha)
    {
        float c = (1 - Math.Abs(2 * lightness - 1)) * saturation, h = hue / 60f, x = c * (1 - Math.Abs(h % 2 - 1)), m = lightness - c / 2;
        var (r, g, b) = h switch { < 1 => (c, x, 0f), < 2 => (x, c, 0f), < 3 => (0f, c, x), < 4 => (0f, x, c), < 5 => (x, 0f, c), _ => (c, 0f, x) };
        return new Color(r + m, g + m, b + m, alpha);
    }

    /// <summary>Every frame, made once: a read only restyles one whose state changed.</summary>
    private void BuildFrames()
    {
        _built = true;
        foreach (var (id, frame) in _layout.Frames)
        {
            var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
            var picture = _strip.Picture(id, 46);
            picture.CustomMinimumSize = Vector2.Zero;
            picture.TextureFilter = CanvasItem.TextureFilterEnum.Linear;
            root.AddChild(picture);
            var name = Lines(Bright);
            root.AddChild(name);
            var need = Lines(Dim);
            root.AddChild(need);
            _frames.AddChild(root);
            var view = _views[id] = new FrameView(id, frame, root, picture, name, need);
            root.Draw += () => DrawFrame(view);
            _strip.Tips.Add(root, () => TipOver(id));
        }
        _sizedAt = null;
    }

    /// <summary>A label of two lines at most, centred, its size set by the zoom.</summary>
    private static Label Lines(Color colour)
    {
        var label = Text("", colour, 12);
        label.LabelSettings.LineSpacing = -2;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.ClipText = true;
        label.MaxLinesVisible = 2;
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        return label;
    }

    public Vector2 ScreenOf(string id) => _view.GlobalPosition + ScreenAt(id);

    /// <summary>Where the first row of the finds is on the screen: what a recording points at.</summary>
    public Vector2 FirstFindAt => _rows.GetChildCount() > 0 ? ((Control)_rows.GetChild(0)).GetGlobalRect().GetCenter() : _side.GetGlobalRect().GetCenter();

    public void Show(bool shown)
    {
        _out = _board.Visible = shown;
        if (shown) _strip.Action.Visible = false;
        else
        {
            (_picked, _hover) = (null, null);
            _find.Drop();
            _find.Visible = _side.Visible = _goal.Visible = _legend.Visible = false;
            _glide?.Kill();
        }
        _key = null;
        _framed = false;
    }

    public void Step(double delta)
    {
        if (!_out || !_built) return;
        _time += delta;
        if (_view.Size != _viewWas) Lay();
        _camera.Fit(Wide, _view.Size.Y, MapWide, MapTall);
        var at = (_camera.X, _camera.Y, _camera.Zoom);
        if (at != _placedAt)
        {
            _placedAt = at;
            Place();
        }
        // Glows breathe: only what glows is drawn again, and the board under a pinned chain.
        foreach (var view in _views.Values)
            if (view.Glows) view.Root.QueueRedraw();
        if (_keep.Pinned != null) _back.QueueRedraw();
    }

    /// <summary>The board, the finds, the goal, the legend and the search where the view has room for them.</summary>
    private void Lay()
    {
        _viewWas = _view.Size;
        _board.Size = new Vector2(Wide, _view.Size.Y);
        _back.Size = _board.Size;
        _side.Position = new Vector2(Wide, 0);
        _side.Size = new Vector2(SideWide, _view.Size.Y);
        _goal.Position = new Vector2(Edge, Edge);
        _goal.Size = new Vector2(GoalWide, 0);
        _legend.Position = new Vector2(Edge, _view.Size.Y - Edge - 34);
        _find.Lay(Wide);
        _placedAt = null;
    }

    /// <summary>Everything where the camera has it now: only when it has moved.</summary>
    private void Place()
    {
        if (_sizedAt != _camera.Zoom) SetSizes();
        foreach (var view in _views.Values) view.Root.Position = (ScreenAt(view.Id) - FrameSize / 2).Round();
        _back.QueueRedraw();
    }

    private Vector2 FrameSize => new Vector2((float)BoardLayout.FrameWide, (float)BoardLayout.FrameTall) * (float)_camera.Zoom;

    /// <summary>Where <paramref name="id"/>'s middle is in the view.</summary>
    private Vector2 ScreenAt(string id) => _layout.Frames.TryGetValue(id, out var frame) ? ToScreen(frame.X, frame.Y) : Vector2.Zero;

    /// <summary>A point of the layout in the view.</summary>
    private Vector2 ToScreen(double x, double y) => new((float)(_camera.X + (x + Margin) * _camera.Zoom), (float)(_camera.Y + (y + Margin) * _camera.Zoom));

    /// <summary>Every frame at the size the zoom shows it; words are never scaled, so they stay sharp.</summary>
    private void SetSizes()
    {
        _sizedAt = _camera.Zoom;
        float z = (float)_camera.Zoom;
        bool words = _camera.Zoom >= WordsFrom, marks = _camera.Zoom < MarksBelow;
        // Under the map's captions the frames step back, so the district names read over them.
        _frames.Modulate = new Color(1, 1, 1, _camera.Zoom >= CaptionsFrom ? 1 : 0.3f);
        var size = FrameSize;
        foreach (var view in _views.Values)
        {
            // An almost frame keeps two lines of «нужно» above its progress bar, so its picture is smaller and higher.
            bool almost = view.State == BoardState.Almost;
            float icon = (words ? almost ? 32 : 46 : 62) * z, lift = words ? almost ? 21 : 14 : 0, from = almost ? 1 : 13;
            view.Root.Size = size;
            view.Picture.Size = new Vector2(icon, icon);
            view.Picture.Position = new Vector2((size.X - icon) / 2, size.Y / 2 - lift * z - icon / 2);
            view.Picture.Visible = !marks && view.State != BoardState.Unknown;
            foreach (var (label, bottom, font) in (ReadOnlySpan<(Label, float, float)>)[(view.Name, 2, 12), (view.Need, 13, 10.5f)])
            {
                label.LabelSettings.FontSize = Math.Max(7, Mathf.RoundToInt(font * z));
                label.Position = new Vector2(4 * z, size.Y / 2 + from * z);
                label.Size = new Vector2(size.X - 8 * z, size.Y / 2 - (from + bottom) * z);
            }
            view.Name.Visible = words && view.State == BoardState.Known;
            view.Need.Visible = words && view.State == BoardState.Almost && view.Need.Text.Length > 0;
            view.Root.QueueRedraw();
        }
        foreach (var box in _blockBoxes.Values.Append(_veilBox)) box.SetCornerRadiusAll(Math.Max(1, Mathf.RoundToInt(14 * z)));
        _frameBox.SetCornerRadiusAll(Math.Max(1, Mathf.RoundToInt(10 * z)));
        _glowBox.SetCornerRadiusAll(Math.Max(1, Mathf.RoundToInt(10 * z)));
    }

    /// <summary>A read of the world: nothing done unless a count, a grant or a machine on any surface has moved.</summary>
    public void Read(Registry m)
    {
        if (!_out || _world() is not { } world) return;
        if (!ReferenceEquals(m.Knowledge, _knowledge)) Forget(m);
        string key = BoardRead.KeyOf(world);
        if (_key == key) return;
        _key = key;
        var read = _read = BoardRead.Of(world);
        _keep.Seen.Read(read);
        if (_keep.Pinned is { } pinned && read.Knows(pinned)) _keep.Pinned = null;
        _side.Visible = _legend.Visible = true;
        Restyle();
        FillFinds();
        FillGoal();
        ShowFind();
        Fill();
        _back.QueueRedraw();
        if (!_framed) Frame();
    }

    /// <summary>
    /// Its frames and their pictures made as a surface loads: made on the first opening, they froze it some 200 ms. Before the
    /// first read, which the world's knowledge may not be ready for yet.
    /// </summary>
    public void Warm(Registry m)
    {
        _strip.Learn(m.Research);
        if (!_built) BuildFrames();
    }

    /// <summary>Another world, loaded or resynced: its board read afresh.</summary>
    private void Forget(Registry m)
    {
        _knowledge = m.Knowledge;
        _keep.Of(_worldId());
        _strip.Learn(m.Research);
        if (!_built) BuildFrames();
        (_picked, _hover, _key, _rowsFor, _unreadFor, _goalFor) = (null, null, null, -1, -1, null);
        _framed = false;
        _viewWas = default;
    }

    /// <summary>The camera as the tablet comes out: at the newest unread find, names showing, else the whole board.</summary>
    private void Frame()
    {
        _framed = true;
        Lay();
        _camera.Fit(Wide, _view.Size.Y, MapWide, MapTall);
        var unread = _keep.Seen.Batches.LastOrDefault(_keep.Seen.Unread);
        if (unread != null && FirstOf(unread) is { } id && _layout.Frames.TryGetValue(id, out var frame))
            _camera.Look(frame.X + Margin, frame.Y + Margin, Close);
        else if (!_looked) _camera.Look(MapWide / 2, MapTall / 2, 0);
        _looked = true;
        _placedAt = null;
    }

    /// <summary>What a batch's line flies to: the first thing it brought past its cause, else the cause.</summary>
    private static string? FirstOf(BoardBatch batch) => batch.Things.FirstOrDefault(t => !batch.By.Contains(t)) ?? batch.Things.FirstOrDefault();

    /// <summary>Each frame whose state, need, progress or glow changed since it was last styled.</summary>
    private void Restyle()
    {
        var read = _read!;
        foreach (var view in _views.Values)
        {
            string id = view.Id;
            var state = BoardView.StateOf(id, read);
            var missing = state == BoardState.Almost ? BoardView.MissingOf(id, read) : [];
            string? need = state == BoardState.Almost ? string.Join(',', missing) : null;
            var parts = state == BoardState.Almost ? BoardView.PartsOf(id, read) : default;
            bool glows = _keep.Seen.Unread(id);
            if (state == view.State && need == view.NeedOf && parts == view.Parts && glows == view.Glows) continue;
            if (state != view.State)
            {
                view.Picture.Material = state == BoardState.Almost ? new ShaderMaterial { Shader = Silhouette } : null;
                view.Name.Text = state == BoardState.Known ? _strip.NameOf(id) : "";
                _sizedAt = null;
            }
            if (need != view.NeedOf || state != view.State)
            {
                // A count, never the first name: naming one would read as the last one missing.
                view.Need.Text = state != BoardState.Almost ? "" : missing.Count > 0 ? T("research.board.missing", ("n", missing.Count)) : T("research.board.ready");
                _sizedAt = null;
            }
            (view.State, view.NeedOf, view.Parts, view.Glows) = (state, need, parts, glows);
            view.Root.QueueRedraw();
        }
        ShowListed();
        _placedAt = null;
    }

    /// <summary>The frame of <paramref name="view"/>: its box by its state, the glow of what is new, the goal's chain, what is hovered or picked, a silhouette's progress and a «?».</summary>
    private void DrawFrame(FrameView view)
    {
        var root = view.Root;
        float z = (float)_camera.Zoom, pulse = 0.5f + 0.5f * MathF.Sin((float)_time * 2.6f);
        var size = root.Size;
        var rect = new Rect2(Vector2.Zero, size);
        bool inChain = _keep.Pinned is { } pinned && _read != null && (pinned == view.Id || ChainIds.Contains(view.Id));
        float hue = Hues.GetValueOrDefault(view.Frame.District);
        if (_camera.Zoom < MarksBelow)
        {
            float side = Math.Max(3, 30 * z);
            var colour = view.Glows ? Amber : view.State switch
            {
                BoardState.Known => Hsl(hue, 0.6f, 0.72f, 0.9f),
                BoardState.Almost => Accent with { A = 0.45f },
                _ => new Color(90 / 255f, 120 / 255f, 160 / 255f, 0.22f),
            };
            var mark = new Rect2(size / 2 - new Vector2(side, side) / 2, new Vector2(side, side));
            root.DrawRect(mark, colour);
            if (inChain) root.DrawRect(mark.Grow(2), Chain, false, 1.5f);
            return;
        }
        if (view.Glows)
        {
            _glowBox.BorderColor = Amber with { A = 0.55f + 0.45f * pulse };
            _glowBox.ShadowSize = Mathf.RoundToInt((10 + 14 * pulse) * Math.Max(z, 0.5f));
            root.DrawStyleBox(_glowBox, rect);
        }
        bool market = BoardThings.Market(view.Id);
        switch (view.State)
        {
            case BoardState.Known:
                (_frameBox.BgColor, _frameBox.BorderColor, _frameBox.DrawCenter) = (KnownFill, Hsl(hue, 0.55f, 0.65f, 0.55f), true);
                root.DrawStyleBox(_frameBox, rect);
                break;
            case BoardState.Almost:
                (_frameBox.BgColor, _frameBox.BorderColor, _frameBox.DrawCenter) = (AlmostFill, Colors.Transparent, true);
                root.DrawStyleBox(_frameBox, rect);
                Dashed(root, rect, Accent with { A = 0.4f }, z);
                break;
            default:
                Dashed(root, rect, market ? MarketEdge : UnknownEdge, z);
                break;
        }
        if (inChain) root.DrawRect(rect.Grow(3 * z), Chain with { A = 0.5f + 0.4f * pulse }, false, 1.6f);
        if (view.Id == _picked || view.Id == _hover) root.DrawRect(rect.Grow(1), view.Id == _picked ? Colors.White : Accent, false, view.Id == _picked ? 2 : 1.5f);
        bool words = _camera.Zoom >= WordsFrom;
        if (view.State == BoardState.Almost)
        {
            var (had, of) = view.Parts;
            of = Math.Max(1, of);
            float wide = Math.Min(60 * z, of * 12 * z), gap = 2 * z, piece = (wide - gap * (of - 1)) / of, left = size.X / 2 - wide / 2;
            for (int i = 0; i < of; i++) root.DrawRect(new Rect2(left + i * (piece + gap), size.Y - 9 * z, piece, 3 * z), i < had ? Accent : Accent with { A = 0.18f });
        }
        if (view.State == BoardState.Unknown)
        {
            int big = Math.Max(6, Mathf.RoundToInt(24 * z));
            var font = Style.Bold;
            float ascent = font.GetAscent(big), descent = font.GetDescent(big);
            root.DrawString(font, new Vector2(0, size.Y / 2 - (words && market ? 6 * z : 0) + (ascent - descent) / 2), "?", HorizontalAlignment.Center, size.X, big, market ? Out with { A = 0.45f } : Mark);
        }
        if (market && words)
        {
            int small = Math.Max(6, Mathf.RoundToInt(10 * z));
            float y = view.State == BoardState.Unknown ? size.Y / 2 + 22 * z : 4 * z + Style.Mono.GetAscent(small);
            root.DrawString(Style.Mono, new Vector2(0, y), T("research.board.market"), HorizontalAlignment.Center, size.X, small, Out with { A = 0.7f });
        }
    }

    /// <summary>The ids of the pinned goal's chain, as of the last read.</summary>
    private HashSet<string> ChainIds { get; set; } = new(StringComparer.Ordinal);

    /// <summary>The font size <paramref name="lines"/> take to fill <paramref name="room"/> across and half of <paramref name="tall"/>, at most 40.</summary>
    private static int CaptionFits(string[] lines, float room, float tall)
    {
        float widest = lines.Max(l => Style.Bold.GetStringSize(l, HorizontalAlignment.Left, -1, 40).X);
        return (int)Math.Clamp(Math.Min(40 * room / widest, tall * 0.6f / lines.Length / 1.2f), 7, 40);
    }

    private static void Dashed(Control on, Rect2 rect, Color colour, float z)
    {
        float dash = Math.Max(2, 4 * z);
        var (a, b, c, d) = (rect.Position, rect.Position + new Vector2(rect.Size.X, 0), rect.End, rect.Position + new Vector2(0, rect.Size.Y));
        on.DrawDashedLine(a, b, colour, 1, dash);
        on.DrawDashedLine(b, c, colour, 1, dash);
        on.DrawDashedLine(c, d, colour, 1, dash);
        on.DrawDashedLine(d, a, colour, 1, dash);
    }

    /// <summary>The screen under the frames: its grid, the district blocks with their captions and column heads, and the ties lit.</summary>
    private void DrawBack()
    {
        var size = _back.Size;
        _back.DrawRect(new Rect2(Vector2.Zero, size), Screen);
        DrawGrid(size);
        float z = (float)_camera.Zoom;
        var read = _read;
        foreach (var block in _layout.Blocks)
        {
            var at = ToScreen(block.X, block.Y);
            var rect = new Rect2(at, new Vector2((float)block.Wide, (float)block.Tall) * z);
            if (!rect.Intersects(new Rect2(Vector2.Zero, size))) continue;
            _back.DrawStyleBox(_blockBoxes[block.District], rect);
            float hue = Hues.GetValueOrDefault(block.District);
            string caption = T($"research.district.{block.District}").ToUpperInvariant();
            if (_camera.Zoom >= CaptionsFrom)
            {
                // The caption shrinks to its block's width; the count sits under it.
                int big = Mathf.RoundToInt(30 * z), small = Math.Max(7, Mathf.RoundToInt(12 * z));
                float room = rect.Size.X - 40 * z, wide = Style.Bold.GetStringSize(caption, HorizontalAlignment.Left, -1, big).X;
                if (wide > room) big = Math.Max(7, (int)(big * room / wide));
                float under = 14 * z + Style.Bold.GetAscent(big) + Style.Bold.GetDescent(big) + 4 * z;
                _back.DrawString(Style.Bold, at + new Vector2(20 * z, 14 * z + Style.Bold.GetAscent(big)), caption, HorizontalAlignment.Left, -1, big, Hsl(hue, 0.7f, 0.78f, 0.95f));
                if (read != null)
                {
                    var things = BoardDistricts.All.First(d => d.Id == block.District).Things;
                    string count = T("research.board.known", ("k", things.Count(read.Knows)), ("n", things.Count));
                    _back.DrawString(Style.Mono, at + new Vector2(22 * z, under + Style.Mono.GetAscent(small)), count, HorizontalAlignment.Left, -1, small, Dim);
                }
            }
            else
            {
                _back.DrawStyleBox(_veilBox, rect);
                // One line or two, whichever lets the caption be biggest in its block; the size measured, never guessed.
                float room = rect.Size.X * 0.85f;
                string[] lines = [caption];
                int fits = CaptionFits(lines, room, rect.Size.Y);
                for (int cut = caption.IndexOf(' '); cut > 0; cut = caption.IndexOf(' ', cut + 1))
                {
                    string[] two = [caption[..cut], caption[(cut + 1)..]];
                    if (CaptionFits(two, room, rect.Size.Y) is int bigger && bigger > fits) (lines, fits) = (two, bigger);
                }
                float ascent = Style.Bold.GetAscent(fits), descent = Style.Bold.GetDescent(fits), line = (ascent + descent) * 1.05f;
                float first = rect.GetCenter().Y - line * (lines.Length - 1) / 2 + (ascent - descent) / 2;
                for (int i = 0; i < lines.Length; i++)
                    _back.DrawString(Style.Bold, new Vector2(rect.Position.X, first + i * line), lines[i], HorizontalAlignment.Center, rect.Size.X, fits, Hsl(hue, 0.75f, 0.8f, 0.95f));
            }
        }
        DrawTies();
    }

    /// <summary>The board's pattern on the screen: a big cell under every frame's place and a gutter between, both edges of each gutter drawn.</summary>
    private void DrawGrid(Vector2 size)
    {
        float z = (float)_camera.Zoom, pitch = (float)(BoardLayout.Cell * z), frame = (float)(BoardLayout.FrameWide * z);
        if (pitch < 12) return;
        var line = GridLine with { A = GridLine.A * 1.6f * Math.Clamp((pitch - 12) / 24, 0, 1) };
        float left = (float)(_camera.X + (Margin + BoardLayout.Pad) * z) % pitch, top = (float)(_camera.Y + (Margin + BoardLayout.Head) * z) % pitch;
        for (float x = (left < 0 ? left + pitch : left) - pitch; x < size.X; x += pitch)
            foreach (float at in (ReadOnlySpan<float>)[x, x + frame])
                _back.DrawLine(new Vector2(MathF.Round(at) + 0.5f, 0), new Vector2(MathF.Round(at) + 0.5f, size.Y), line);
        for (float y = (top < 0 ? top + pitch : top) - pitch; y < size.Y; y += pitch)
            foreach (float at in (ReadOnlySpan<float>)[y, y + frame])
                _back.DrawLine(new Vector2(0, MathF.Round(at) + 0.5f), new Vector2(size.X, MathF.Round(at) + 0.5f), line);
    }

    /// <summary>The pinned goal's chain, breathing; and the ties of what is hovered or picked: what it is made of, and what is made of it that the board names.</summary>
    private void DrawTies()
    {
        if (_read is not { } read) return;
        float pulse = 0.5f + 0.5f * MathF.Sin((float)_time * 2.6f);
        if (_keep.Pinned is { } pinned)
            foreach (var (from, to) in BoardChain.Of(pinned, read).Ties) Thread(from, to, Chain with { A = 0.55f + 0.35f * pulse }, 2.2f, true);
        if ((_hover ?? _picked) is not { } focus || BoardView.StateOf(focus, read) == BoardState.Unknown) return;
        if (_wholeChain.ButtonPressed)
            foreach (var (from, to) in BoardChain.Of(focus, read).Ties) Thread(from, to, Accent with { A = 0.75f }, 1.6f, false);
        else
        {
            var link = TechLinks.Of(focus);
            foreach (var (item, _) in link.Takes) Thread(item, focus, Accent, 2, false);
            foreach (string source in link.From) Thread(source, focus, Accent with { A = 0.9f }, 2, true);
            foreach (string station in link.MadeAt) Thread(station, focus, Accent with { A = 0.6f }, 1.2f, true);
        }
        foreach (string used in TechLinks.Of(focus).UsedIn)
            if (BoardView.StateOf(used, read) != BoardState.Unknown) Thread(focus, used, Out with { A = 0.9f }, 1.8f, false);
    }

    /// <summary>A tie from one frame's side to another's, curving out level from each; a dot where it ends.</summary>
    private void Thread(string from, string to, Color colour, float width, bool dashed)
    {
        if (from == to || !_layout.Frames.ContainsKey(from) || !_layout.Frames.ContainsKey(to)) return;
        float z = (float)_camera.Zoom;
        Vector2 a = ScreenAt(from), b = ScreenAt(to);
        float way = b.X >= a.X ? 1 : -1, half = (float)BoardLayout.FrameWide / 2 * z;
        a.X += way * half;
        b.X -= way * half;
        float pull = Math.Max(40 * z, Math.Abs(b.X - a.X) / 2);
        Vector2 c1 = a + new Vector2(way * pull, 0), c2 = b - new Vector2(way * pull, 0);
        const int Steps = 24;
        var points = new Vector2[Steps + 1];
        for (int i = 0; i <= Steps; i++)
        {
            float t = i / (float)Steps, u = 1 - t;
            points[i] = u * u * u * a + 3 * u * u * t * c1 + 3 * u * t * t * c2 + t * t * t * b;
        }
        if (dashed)
            for (int i = 0; i < Steps; i += 2) _back.DrawLine(points[i], points[i + 1], colour, width, true);
        else _back.DrawPolyline(points, colour, width, true);
        _back.DrawCircle(b, 2.6f, colour);
    }

    /// <summary>The list of finds again, once a batch came or what is unread changed: newest first, an unread line marked amber.</summary>
    private void FillFinds()
    {
        var seen = _keep.Seen;
        int unread = seen.UnreadBatches;
        if (seen.Batches.Count == _rowsFor && unread == _unreadFor) return;
        (_rowsFor, _unreadFor) = (seen.Batches.Count, unread);
        StripParts.Clear(_rows);
        if (seen.Batches.Count == 0) _rows.AddChild(Tablet.Text(T("research.finds.none"), Faint));
        for (int i = seen.Batches.Count - 1; i >= 0; i--) _rows.AddChild(RowOf(seen.Batches[i]));
    }

    /// <summary>
    /// A card of the finds: what it opened as pictures, what it brought closer as silhouettes, and small under them what caused it. A click on a picture
    /// flies to it; on the card, to the first opened.
    /// </summary>
    private Control RowOf(BoardBatch batch)
    {
        bool unread = _keep.Seen.Unread(batch);
        var row = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Stop, MouseDefaultCursorShape = Control.CursorShape.PointingHand };
        var box = new StyleBoxFlat { BgColor = unread ? Amber with { A = 0.06f } : Colors.White with { A = 0.02f }, BorderColor = unread ? Amber : LineColour, BorderWidthLeft = 2 };
        (box.CornerRadiusTopRight, box.CornerRadiusBottomRight) = (6, 6);
        box.SetContentMarginAll(8);
        row.AddThemeStyleboxOverride("panel", box);
        var card = Style.Gap(new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 6);
        var opened = batch.Things.Where(t => !batch.By.Contains(t)).ToList();
        card.AddChild(Chips(opened.Count > 0 ? opened : batch.Things, false, 8));
        if (batch.Closer is { Count: > 0 } closer)
        {
            card.AddChild(StripParts.Text(T("research.finds.closer"), Dim, 11));
            card.AddChild(Chips(closer, true, 6));
        }
        string? cause = batch.Cause switch
        {
            BoardSeen.CauseFound => T("research.finds.found", ("what", string.Join(", ", batch.By.Take(3).Select(_strip.NameOf)) + (batch.By.Count > 3 ? " …" : ""))),
            BoardSeen.CauseBuilt => T("research.finds.built", ("what", string.Join(", ", batch.By.Take(3).Select(_strip.NameOf)))),
            _ => null,
        };
        if (cause != null)
        {
            var why = StripParts.Text(cause, Faint, 11);
            why.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            card.AddChild(why);
        }
        row.AddChild(card);
        row.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            row.AcceptEvent();
            if (FirstOf(batch) is { } id) Pick(id, true);
        };
        return row;
    }

    /// <summary>Pictures with their names, at most <paramref name="most"/> and a count of the rest; <paramref name="dim"/> draws them as silhouettes.</summary>
    private Control Chips(IReadOnlyList<string> ids, bool dim, int most)
    {
        var flow = new HFlowContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        flow.AddThemeConstantOverride("h_separation", 4);
        flow.AddThemeConstantOverride("v_separation", 4);
        foreach (string id in ids.Take(most))
        {
            var chip = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Stop, MouseDefaultCursorShape = Control.CursorShape.PointingHand };
            var chipBox = new StyleBoxFlat { BgColor = dim ? AlmostFill : KnownFill, BorderColor = dim ? UnknownEdge : LineColour };
            chipBox.SetBorderWidthAll(1);
            chipBox.SetCornerRadiusAll(4);
            chipBox.SetContentMarginAll(3);
            chipBox.ContentMarginRight = 6;
            chip.AddThemeStyleboxOverride("panel", chipBox);
            var line = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 4);
            var picture = _strip.Picture(id, 20);
            if (dim) picture.Material = new ShaderMaterial { Shader = Silhouette };
            line.AddChild(picture);
            var name = StripParts.Text(_strip.NameOf(id), dim ? Dim : Bright, 12);
            name.VerticalAlignment = VerticalAlignment.Center;
            line.AddChild(name);
            chip.AddChild(line);
            chip.GuiInput += e =>
            {
                if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
                chip.AcceptEvent();
                Pick(id, true);
            };
            flow.AddChild(chip);
        }
        if (ids.Count > most)
        {
            var more = StripParts.Text($"+{ids.Count - most}", Dim, 12);
            more.VerticalAlignment = VerticalAlignment.Center;
            flow.AddChild(more);
        }
        return flow;
    }

    private static string Escape(string text) => text.Replace("[", "[lb]");

    /// <summary>"Read all": every glow out.</summary>
    private void ReadAll()
    {
        _keep.Seen.SeeAll();
        Reread();
    }

    /// <summary>What is unread changed: the glows and the list follow.</summary>
    private void Reread()
    {
        if (_read == null) return;
        Restyle();
        FillFinds();
    }

    /// <summary>The goal's panel: what it still lacks first, each with how to come by it, then what is in hand, ticked.</summary>
    private void FillGoal()
    {
        var read = _read!;
        string? pinned = _keep.Pinned;
        ChainIds = pinned == null ? new(StringComparer.Ordinal) : BoardChain.Of(pinned, read).Ids.ToHashSet(StringComparer.Ordinal);
        _goal.Visible = pinned != null && _out;
        string stamp = pinned == null ? "" : pinned + "|" + _key;
        if (stamp == _goalFor) return;
        _goalFor = stamp;
        StripParts.Clear(_goal);
        if (pinned == null) return;
        var ids = BoardChain.Of(pinned, read).Ids.OrderBy(BoardLayout.DepthOf).ThenBy(_strip.NameOf, StringComparer.Ordinal).ToList();
        var done = ids.Where(id => BoardChain.Done(id, read)).ToList();
        var column = Style.Gap(new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 4);
        var head = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 6);
        var tag = Tablet.Text(T("research.goal").ToUpperInvariant(), Chain, 10);
        tag.VerticalAlignment = VerticalAlignment.Center;
        head.AddChild(tag);
        var name = Text(_strip.NameOf(pinned), Bright, 14);
        name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        name.ClipText = true;
        head.AddChild(name);
        var close = ScreenButton("×", () => Pin(null));
        head.AddChild(close);
        column.AddChild(head);
        column.AddChild(Tablet.Text(T("research.goal.done", ("k", done.Count), ("n", ids.Count)), Dim, Tablet.HintSize));
        var bar = new Control { CustomMinimumSize = new Vector2(0, 4), MouseFilter = Control.MouseFilterEnum.Ignore };
        float share = ids.Count == 0 ? 1 : done.Count / (float)ids.Count;
        bar.Draw += () =>
        {
            bar.DrawRect(new Rect2(Vector2.Zero, bar.Size), LineColour);
            bar.DrawRect(new Rect2(Vector2.Zero, new Vector2(bar.Size.X * share, bar.Size.Y)), Chain);
        };
        column.AddChild(bar);
        foreach (string id in ids.Where(id => !done.Contains(id))) column.AddChild(GoalLine(id, HowOf(id), false));
        foreach (string id in done) column.AddChild(GoalLine(id, "", true));
        _goal.AddChild(column);
    }

    /// <summary>A line of the goal: its name, which flies to it, and how to come by it, or ticked once in hand.</summary>
    private Control GoalLine(string id, string how, bool done)
    {
        var line = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Stop, MouseDefaultCursorShape = Control.CursorShape.PointingHand }, 6);
        var name = Text((done ? "✓ " : "") + _strip.NameOf(id), done ? Faint : Bright, 13);
        name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        name.ClipText = true;
        line.AddChild(name);
        if (how.Length > 0) line.AddChild(Text(how, Dim, 11));
        line.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            line.AcceptEvent();
            Pick(id, true);
        };
        return line;
    }

    /// <summary>How the goal's missing <paramref name="id"/> is come by: a machine built, an item found, the rest made.</summary>
    private static string HowOf(string id) =>
        BoardThings.Machine(id) || (BoardThings.Of(id) is { Stations: null }) ? T("research.goal.build")
        : BoardThings.Of(id) == null && BoardThings.SourcesOf(id).Count == 0 ? T("research.goal.find")
        : T("research.goal.make");

    /// <summary>Pins <paramref name="id"/> as the goal, or none.</summary>
    private void Pin(string? id)
    {
        _keep.Pinned = id;
        _goalFor = null;
        FillGoal();
        Restyle();
        Fill();
        _back.QueueRedraw();
        foreach (var view in _views.Values) view.Root.QueueRedraw();
    }

    /// <summary>The board's pointer: any button drags the board, a click picks what is under it, the wheel zooms; hovering lights ties and reads.</summary>
    public void Pointer(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left or MouseButton.Right or MouseButton.Middle } button:
                if (button.Pressed)
                {
                    _glide?.Kill();
                    (_pressed, _moved) = (true, 0);
                }
                else
                {
                    if (_pressed && _moved < ClickSlack && button.ButtonIndex == MouseButton.Left) Click(button.Position);
                    LetGo();
                }
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown }:
                Zoom(e, _view);
                break;
            case InputEventMouseMotion { ButtonMask: not 0 } drag when _pressed:
                _moved += drag.Relative.Length();
                if (_moved >= ClickSlack) _camera.Drag(drag.Relative.X, drag.Relative.Y);
                break;
            case InputEventMouseMotion motion:
                Hover(motion.Position.X < Wide ? ThingAt(motion.Position) : null);
                break;
        }
    }

    public void LetGo() => _pressed = false;

    public void Zoom(InputEvent e, Control over)
    {
        if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown } wheel) return;
        var at = _view.GetLocalMousePosition();
        _glide?.Kill();
        _camera.Wheel(wheel.ButtonIndex == MouseButton.WheelUp ? 1 : -1, at.X, at.Y);
        over.AcceptEvent();
    }

    /// <summary>The pointer over <paramref name="id"/>, or nothing: its ties lit, and it is read.</summary>
    private void Hover(string? id)
    {
        if (id == _hover) return;
        string? was = _hover;
        _hover = id;
        foreach (string? one in (ReadOnlySpan<string?>)[was, id])
            if (one != null && _views.TryGetValue(one, out var view)) view.Root.QueueRedraw();
        if (id != null && _keep.Seen.See(id)) Reread();
        _back.QueueRedraw();
    }

    /// <summary>A click at a point of the view: picks what is known or almost there; off everything, or on a «?», picks nothing.</summary>
    private void Click(Vector2 at)
    {
        string? id = at.X < Wide ? ThingAt(at) : null;
        if (id != null && _read != null && BoardView.StateOf(id, _read) == BoardState.Unknown) id = null;
        Pick(id, false);
    }

    /// <summary>Picks <paramref name="id"/>, or nothing; with <paramref name="fly"/>, the board glides to it, names showing.</summary>
    private void Pick(string? id, bool fly)
    {
        string? was = _picked;
        _picked = id;
        foreach (string? one in (ReadOnlySpan<string?>)[was, id])
            if (one != null && _views.TryGetValue(one, out var view)) view.Root.QueueRedraw();
        if (id != null && _keep.Seen.See(id)) Reread();
        if (fly && id != null && _layout.Frames.TryGetValue(id, out var frame)) GlideTo(frame.X + Margin, frame.Y + Margin, Math.Max(_camera.Zoom, Close));
        ShowListed();
        Fill();
        _back.QueueRedraw();
    }

    /// <summary>Takes the middle of the board's part of the view to a point of the board at <paramref name="zoom"/>.</summary>
    private void GlideTo(double x, double y, double zoom)
    {
        _glide?.Kill();
        var (fromX, fromY) = _camera.ToMap(Wide / 2, _view.Size.Y / 2);
        double fromZoom = _camera.Zoom;
        _glide = _view.CreateTween();
        _glide.TweenMethod(Callable.From<float>(t =>
            _camera.Look(fromX + (x - fromX) * t, fromY + (y - fromY) * t, fromZoom * Math.Pow(zoom / fromZoom, t))), 0f, 1f, Glide)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
    }

    /// <summary>What frame is at a point of the view; none if none is.</summary>
    private string? ThingAt(Vector2 at)
    {
        var (x, y) = _camera.ToMap(at.X, at.Y);
        (x, y) = (x - Margin, y - Margin);
        foreach (var frame in _layout.Frames.Values)
            if (Math.Abs(frame.X - x) <= BoardLayout.FrameWide / 2 && Math.Abs(frame.Y - y) <= BoardLayout.FrameTall / 2) return frame.Id;
        return null;
    }

    /// <summary>The search over the board while anything on it is known; it lets go of the keys as it goes.</summary>
    private void ShowFind()
    {
        bool shown = _out && _keep.Seen.Any;
        if (!shown && _find.Typing) _find.Drop();
        _find.Visible = shown;
    }

    /// <summary>What the search can find: what the board names, known and almost, never a «?».</summary>
    private IEnumerable<BoardFound> Findable()
    {
        var other = Speaking == Language.Russian ? Language.English : Language.Russian;
        foreach (var view in _views.Values)
            if (view.State != BoardState.Unknown) yield return new BoardFound(view.Id, _strip.NameOf(view.Id), _strip.NameIn(other, view.Id), view.State == BoardState.Known);
    }

    /// <summary>The search's list opened, changed or closed: the board lights what it lists, dimming the rest.</summary>
    private void Listed(IReadOnlyCollection<string>? ids)
    {
        _listed = ids;
        ShowListed();
    }

    private void ShowListed()
    {
        foreach (var view in _views.Values) view.Root.Modulate = _listed == null || _listed.Contains(view.Id) ? Colors.White : Dimmed;
    }

    /// <summary>What the search picked: picked as by a click, and the board glides to it.</summary>
    private void Find(string id) => Pick(id, true);

    /// <summary>Whether the board's search has the keys.</summary>
    public bool Typing => _find.Typing;

    /// <summary>The Search key (/): puts the keys in the board's search, while it shows.</summary>
    public void TypeSearch()
    {
        if (_find.Visible) _find.Type();
    }

    /// <inheritdoc cref="BoardFind.Drop"/>
    public bool DropSearch() => _find.Drop();

    /// <summary>What the pointer says over a frame: nothing of a «?» but that it is not open yet; what is known of the rest. Nothing past the board.</summary>
    private Tip? TipOver(string id)
    {
        var mouse = _board.GetGlobalMousePosition();
        if (!_board.GetGlobalRect().HasPoint(mouse) || (_goal.Visible && _goal.GetGlobalRect().HasPoint(mouse)) || (_legend.Visible && _legend.GetGlobalRect().HasPoint(mouse)) || (_side.Visible && _side.GetGlobalRect().HasPoint(mouse))) return null;
        if (!_views.TryGetValue(id, out var view)) return null;
        return view.State switch
        {
            BoardState.Unknown => new Tip(T("research.board.unknown")),
            BoardState.Almost => new Tip(_strip.NameOf(id), view.NeedOf is { Length: > 0 } need ? [new TipLine(T("research.board.need", ("what", string.Join(", ", need.Split(',').Select(_strip.NameOf)))), Tone.Dim)] : [new TipLine(T("research.board.ready"), Tone.Dim)]),
            _ => TipOf(id),
        };
    }

    /// <summary>The strip below, built again when anything read has moved: what is known of what is picked, and its pin.</summary>
    public void Fill()
    {
        _strip.Clear();
        var facts = _strip.Facts;
        if (_read is not { } read || !_keep.Seen.Any) return;
        if (_picked is not { } id)
        {
            var hint = Text(T("research.board.hint"), Style.Slate500);
            hint.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            facts.AddChild(hint);
            return;
        }
        var state = BoardView.StateOf(id, read);
        double? pencil = state == BoardState.Known ? null : BoardView.PencilOf(id, read);
        facts.AddChild(_strip.Title(_strip.NameOf(id), KindOf(id), id, TipOf, pencil));
        FactsOf(id, facts);
        if (state != BoardState.Almost && _keep.Pinned != id) return;
        var pin = ScreenButton(T(_keep.Pinned == id ? "research.goal.unpin" : "research.goal.pin"), () => Pin(_keep.Pinned == id ? null : id));
        pin.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        facts.AddChild(pin);
    }

    private static string KindOf(string id) => Table.ContainsKey($"item.{id}.kind") ? T($"item.{id}.kind") : "";

    /// <summary>Where the player finds the blueprint <paramref name="id"/> to build: the build menu and its tab.</summary>
    private static string BuiltFrom(string id) =>
        TechLinks.MenuTabOf(id) is { } tab ? T("research.builtInPlace", ("menu", T("build.key")), ("tab", T($"build.tab.{tab}"))) : T("build.key");

    /// <summary>Whether <paramref name="id"/> is known: what its strip lists as in a recipe.</summary>
    private bool Had(string id) => _read?.Knows(id) == true;

    /// <summary>What is known of <paramref name="id"/>: what it takes, where it is made, what it goes into.</summary>
    private void FactsOf(string id, Control facts)
    {
        bool note = Had(id);
        var thing = BoardThings.Of(id);
        var link = TechLinks.Of(id);
        if (link.Takes.Count > 0)
        {
            var takes = Flow(10);
            foreach (var (item, count) in link.Takes) takes.AddChild(PartOf(item, count, note));
            string heading = note || thing == null ? "research.takes" : thing.Parts.Any(p => !_read!.Has(p)) ? "research.board.needEach" : "research.board.haveEnough";
            facts.AddChild(Fact(T(heading), From, takes, 3));
        }
        else if (link.From.Count > 0)
            facts.AddChild(Fact(T("research.takes"), From, _strip.Pictures(link.From, TipOf, source => !Had(source)), 3));
        if (thing != null || link.MadeAt.Count > 0)
        {
            var at = Flow(10);
            foreach (string station in link.MadeAt) at.AddChild(StationOf(station));
            bool built = link.MadeAt.Count == 0 && thing!.Stations == null;
            if (link.MadeAt.Count == 0) at.AddChild(Text(built ? BuiltFrom(id) : T("research.atLifepod"), Style.Slate500));
            facts.AddChild(Fact(T(built ? "research.builtAt" : "research.madeAt"), MadeAt, at, 2));
        }
        if (link.UsedIn.Count > 0) facts.AddChild(Fact(T("research.usedIn"), UsedIn, _strip.Pictures(link.UsedIn, TipOf, used => !Had(used)), 5));
    }

    /// <summary>A part: of what is known, as in a recipe; of a silhouette, ticked once ever had, dim till then.</summary>
    private Control PartOf(string item, int count, bool known)
    {
        string name = _strip.NameOf(item);
        if (known) return _strip.Named(item, T("research.taken", ("n", count), ("what", name)), Style.Slate100);
        return _read!.Has(item)
            ? _strip.Named(item, T("research.board.partDone", ("what", name)), Bought)
            : _strip.Named(item, name, Style.Slate500);
    }

    /// <summary>A station: named while one stands; a pale picture while none does, named by the pointer.</summary>
    private Control StationOf(string station)
    {
        if (_read!.Stands(station)) return _strip.Named(station, _strip.NameOf(station), Style.Slate100);
        var pale = _strip.Unseen(station);
        _strip.Tips.Add(pale, () => TipOf(station));
        return pale;
    }

    /// <summary>What the pointer says of <paramref name="id"/>: all of what is known, the name alone of the rest.</summary>
    private Tip TipOf(string id)
    {
        if (!Had(id)) return new Tip(_strip.NameOf(id));
        if (_strip.PlacementOf(id) is not { } placement) return new Tip(_strip.NameOf(id)) { Card = ItemCards.Of(id) };
        var lines = new List<TipLine>();
        if (Table.ContainsKey($"tool.{id}.about")) lines.Add(new TipLine(T($"tool.{id}.about"), Tone.Dim));
        return new Tip(_strip.NameOf(id), lines) { Picture = placement };
    }
}
