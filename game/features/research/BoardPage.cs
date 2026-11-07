using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Content;
using Stillwake.Game.I18n;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Research;
using static Stillwake.Game.Features.Research.StripParts;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The tablet's first page: the scavenger's board (docs/decisions/2026-11-05-the-board.md), blank till the first find. Notes written with a stylus where <see cref="BoardSlots"/>
/// stands them, threads to shapes sketched by outline, dragged and zoomed (<see cref="TechCamera"/>). A click picks and says what is known of it in
/// the strip below; it buys nothing, for it takes no way to send a command. A thing dragged stays where it is let go, on this machine only (<see cref="BoardKeep"/>). What landed has a loop drawn round it until the player touches it ([board-thoughts](../../../docs/decisions/2026-11-02-board-thoughts.md)).
/// Unread entries wait beside the board, one shown large; a click lands it at its place ([the pile](../../../docs/decisions/2026-11-02-board-pile.md), <see cref="BoardPile"/>).
/// </summary>
public sealed class BoardPage : ITabletPage
{
    private static readonly Color Light = new("d8e0ec"), Drawn = new("b4bfd0"), NoteFill = new("172033"), CardFill = new("131b2c"), Pressed = new("2a3550"),
        Behind = new("55617a"), Dim = new("94a3b8"), Fresh = new("dbe3ef"), Plain = new("7f8ba0"), New = new("f2b84b"), Dimmed = new(1, 1, 1, 0.3f);

    /// <summary>Under this zoom words would be too small to read, and only pictures show.</summary>
    private const double WordsFrom = 0.5;
    /// <summary>What a note is drawn in, and what shrinks that to the board at zoom 1: the strokes' wander is the candidate's own.</summary>
    private const float InkLeft = -74, InkTop = -90, InkWide = 148, InkTall = 180, InkScale = 0.45f;
    /// <summary>A note on the board at zoom 1: half of it, its picture and where that stands, its words' size and room.</summary>
    private const float HalfWide = InkWide * InkScale / 2, HalfTall = InkTall * InkScale / 2, PictureUp = 10, NameTop = 10, NameWide = 62, NameTall = 29;
    private const int Icon = 40, NameSize = 11;
    /// <summary>The line of a shape's lack at zoom 1: where it stands, how tall it is, and the picture of what it lacks.</summary>
    private const float LackTop = 16, LackTall = 16, LackPicture = 15;
    /// <summary>What a thread takes as a box, and the room kept round what a view holds.</summary>
    private const float ThreadHalfWide = 36, ThreadHalfTall = 42, FrameWide = 46, FrameTall = 52;
    /// <summary>The loop round a new note, from the note's centre at zoom 1.</summary>
    private const float LoopWide = 45, LoopTall = 52, LoopWidth = 2;
    /// <summary>What of the view the unread take while there are any, the picked candidate's; and their parts' sizes.</summary>
    private const float SideShare = 0.45f, SideGap = 12, StackWide = 118, StackTall = 140, TakeTall = 48, TakeOver = 4, SheetWide = StackWide - SheetBy * (Sheets - 1), CardMost = 460;
    /// <summary>The entry's picture, name and words, and how far they stand from its edge.</summary>
    private const int CardPicture = 128, CardName = 30, CardRow = 17, CardPad = 22;
    /// <summary>The outlines shown of the unread, and how far each stands from the one before.</summary>
    private const int Sheets = 4, SheetBy = 7;
    /// <summary>Seconds of an entry's flight, of a thread drawing itself, of a new entry coming up and of the board settling once the pile is gone.</summary>
    private const double Slide = 0.42, ThreadDraws = 0.4, CardComes = 0.16, Settling = 0.6;
    /// <summary>Of a flight: how far along the entry has become a note, how small it is by then, and how far aside a new entry starts.</summary>
    private const float NoteFrom = 0.55f, CardLeast = 0.3f, CardFrom = 90;
    /// <summary>A note's width in flight as it lands, against the card it was.</summary>
    private const float NoteLands = 72;
    /// <summary>Seconds of the glide to what the search picked.</summary>
    private const double Found = 0.3;

    /// <summary>What a thing on the board is made of, kept so a read only restyles it.</summary>
    private sealed record NoteView(Control Root, Control Box, TextureRect Picture, Label Name, Line2D Mark, HBoxContainer Lack, Label LackWords)
    {
        public TextureRect? LackPicture { get; set; }
        public string? LackOf { get; set; }
        public BoardShow Show { get; set; } = BoardShow.Unseen;
        public BoardSketch Sketch { get; set; }
        public InkPoint[][] Ink { get; set; } = [];
    }

    /// <summary>An entry on its way to its place: first the card, shrinking, then the note it becomes.</summary>
    private sealed record Flight(string Id, Control Card, NoteView Note, Vector2 From);

    private readonly Control _view;
    private readonly StripParts _strip;
    private readonly Action _readSoon;
    private readonly Func<uint> _worldId;
    private readonly BoardKeep _keep;
    private readonly TechCamera _camera = new() { Floor = BoardBox.Smallest };
    // The board's part of the view: nothing on the board shows past it, under the entry and the stack.
    private readonly Control _board = new() { MouseFilter = Control.MouseFilterEnum.Ignore, ClipContents = true };
    // What scales is under _map, what is set at its seen size under _front.
    private readonly Control _map = new() { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
    private readonly Control _front = new() { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
    private readonly Control _back = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly Dictionary<string, NoteView> _notes = new(StringComparer.Ordinal);
    private readonly Dictionary<(string, string), (BoardSpot A, BoardSpot B, InkPoint[] Ink)> _threads = [];
    private Dictionary<string, BoardSlot> _slots = new(StringComparer.Ordinal);
    private HashSet<string> _had = new(StringComparer.Ordinal);
    private BoardPicture _picture = new(new Dictionary<string, BoardShow>(), []);
    private BoardRead? _read;
    private object? _knowledge;
    private (object Knowledge, int Version, object Grid, int Built, string? Picked)? _key;
    private (double X, double Y, double Zoom)? _placedAt;
    private BoardBox _box;
    private Vector2 _viewWas;
    private string? _picked;
    private double? _sizedAt;
    private bool _centred, _out, _pressed;
    private float _moved;
    private string? _held;
    private Vector2 _heldBy;
    private readonly BoardPile _pile = new();
    private IReadOnlyList<BoardSlot> _order = [];
    // Over the board: the unread beside it, then what flies from them to it. _card takes the presses and stays, what it shows changes.
    private readonly Control _side = new() { MouseFilter = Control.MouseFilterEnum.Stop, Visible = false };
    private readonly Control _card = new() { MouseFilter = Control.MouseFilterEnum.Stop };
    private Control? _cardNow;
    private string? _cardFor;
    private readonly Control _stack = new() { MouseFilter = Control.MouseFilterEnum.Stop };
    private readonly Label _count = Text("", Light, 44);
    private readonly Button _takeAll = new() { FocusMode = Control.FocusModeEnum.None, Text = T("research.pile.takeAll") };
    private readonly Control _flyers = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly List<Flight> _flights = [];
    private readonly List<Tween> _tweens = [];
    /// <summary>What has landed and goes at the next read, once its note stands there.</summary>
    private readonly List<Control> _down = [];
    /// <summary>How far the threads of a note just landed are drawn.</summary>
    private readonly Dictionary<string, float> _growing = new(StringComparer.Ordinal);
    private Tween? _glide;
    /// <summary>What will be on the board once the pile is down: what it is framed by while reading.</summary>
    private List<string> _ahead = [];
    private (double X, double Y, double Zoom)? _framed;
    /// <summary>A pile is showing or still landing, so the board keeps to its part of the view; and how much it gives up, which eases back as it settles.</summary>
    private bool _narrow;
    private float _share;
    // The press that flipped may still be held as the side goes, and its motion must not take the board.
    private bool _flipHeld;
    private readonly BoardFind _find;

    /// <summary>What the search's open list shows: lit on the board, the rest dimmed; none while it is closed.</summary>
    private IReadOnlyCollection<string>? _listed;

    private bool Words => _camera.Zoom >= WordsFrom;

    /// <summary>What of the view the board has: all of it, unless unread stand beside it.</summary>
    private float Wide => _view.Size.X * (1 - _share);

    /// <summary>Whether an entry is shown to read.</summary>
    public bool Reading => _pile.Top != null;

    /// <summary>Where the entry is on the screen: what a recording points at.</summary>
    public Vector2 EntryAt => _card.GetGlobalRect().GetCenter();

    /// <summary>Where the button is on the screen: what a recording points at.</summary>
    public Vector2 TakeAllAt => _takeAll.GetGlobalRect().GetCenter();

    /// <param name="view">The tablet's view, which it draws into.</param>
    /// <param name="strip">The strip below and its words.</param>
    /// <param name="readSoon">Has the tablet read on its next frame.</param>
    /// <param name="worldId">Which world is played: what the board is kept under.</param>
    /// <param name="keep">What is kept of the board, shared with the tablet's key.</param>
    public BoardPage(Control view, StripParts strip, Action readSoon, Func<uint> worldId, BoardKeep keep)
    {
        (_view, _strip, _readSoon, _worldId, _keep) = (view, strip, readSoon, worldId, keep);
        _back.Draw += DrawBack;
        _map.AddChild(_back);
        _board.AddChild(_map);
        _board.AddChild(_front);
        view.AddChild(_board);
        BuildSide();
        view.AddChild(_side);
        view.AddChild(_flyers);
        _find = new BoardFind(Findable, _strip.Picture, Find, Listed) { Visible = false };
        view.AddChild(_find);
    }

    /// <summary>The unread beside the board, built once: what is shown changes under the pointer, what takes its press never does.</summary>
    private void BuildSide()
    {
        _card.GuiInput += e => FlipOn(e, _card);
        _stack.GuiInput += e => FlipOn(e, _stack);
        _stack.Draw += DrawStack;
        _count.HorizontalAlignment = HorizontalAlignment.Center;
        _count.Position = new Vector2(0, 40);
        _count.Size = new Vector2(SheetWide, 50);
        _stack.AddChild(_count);
        var unread = Text(T("research.pile.unread"), Dim, Tablet.HintSize + 1);
        unread.HorizontalAlignment = HorizontalAlignment.Center;
        unread.Position = new Vector2(0, 100);
        unread.Size = new Vector2(SheetWide, 20);
        _stack.AddChild(unread);
        foreach (var (state, fill) in (ReadOnlySpan<(string, Color)>)[("normal", CardFill), ("hover", NoteFill), ("pressed", Pressed)])
            _takeAll.AddThemeStyleboxOverride(state, new StyleBoxFlat { BgColor = fill });
        _takeAll.AddThemeFontOverride("font", Style.Clear);
        _takeAll.AddThemeFontSizeOverride("font_size", 18);
        _takeAll.AddThemeColorOverride("font_color", Light);
        _takeAll.AddThemeColorOverride("font_hover_color", Light);
        _takeAll.AddThemeColorOverride("font_pressed_color", Light);
        _takeAll.Draw += () => InkDraw.Strokes(_takeAll, BoardInk.Box(0, 0, _takeAll.Size.X, _takeAll.Size.Y, 5.3f, 3, 12), Drawn, 1.5f);
        _takeAll.Pressed += TakeAll;
        _side.AddChild(_card);
        _side.AddChild(_stack);
        _side.AddChild(_takeAll);
    }

    /// <summary>The side where the view has room for it.</summary>
    private void LaySide()
    {
        var size = new Vector2(_view.Size.X * SideShare, _view.Size.Y);
        _side.Position = new Vector2(_view.Size.X - size.X, 0);
        _side.Size = size;
        float tall = MathF.Min(size.Y - 2 * SideGap, CardMost);
        _card.Position = new Vector2(0, (size.Y - tall) / 2);
        _card.Size = new Vector2(size.X - StackWide - 3 * SideGap, tall);
        float top = (size.Y - StackTall - SideGap - TakeTall) / 2;
        _stack.Position = new Vector2(size.X - StackWide - 2 * SideGap, top);
        _stack.Size = new Vector2(StackWide, StackTall);
        _takeAll.Position = new Vector2(_stack.Position.X - TakeOver, top + StackTall + SideGap);
        _takeAll.Size = new Vector2(StackWide + 2 * TakeOver, TakeTall);
        _board.Size = new Vector2(Wide, _view.Size.Y);
        _find.Lay(_view.Size.X);
    }

    /// <summary>The outlines of the unread, one behind another up and to the right.</summary>
    private void DrawStack()
    {
        var sheet = new Vector2(SheetWide, StackTall - SheetBy * (Sheets - 1));
        for (int i = Math.Min(Sheets, _keep.Seen.Unread.Count) - 1; i >= 0; i--)
        {
            var at = new Vector2(i * SheetBy, (Sheets - 1 - i) * SheetBy);
            _stack.DrawRect(new Rect2(at, sheet), CardFill);
            InkDraw.Strokes(_stack, BoardInk.Box(at.X, at.Y, sheet.X, sheet.Y, 2 + i * 1.7f, 2, 14), i == 0 ? Drawn : Behind, 1.4f);
        }
    }

    /// <summary>A left press on the entry or the stack: lands the entry.</summary>
    private void FlipOn(InputEvent e, Control over)
    {
        if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
        over.AcceptEvent();
        if (_pile.Flip(_keep.Seen, _order, BoardThings.All) is not { } id) return;
        _flipHeld = true;
        Launch(id);
        ReadSoon();
    }

    /// <summary>The button: lands all that wait, at once, with no flight.</summary>
    private void TakeAll()
    {
        if (!_pile.TakeAll(_keep.Seen)) return;
        Stop();
        ReadSoon();
    }

    private void ReadSoon()
    {
        _key = null;
        _readSoon();
    }

    /// <summary>Takes the entry shown off the side and starts it towards its place on the board.</summary>
    private void Launch(string id)
    {
        var card = _cardNow ?? CardOf(id);
        _cardNow?.GetParent().RemoveChild(_cardNow);
        (_cardNow, _cardFor) = (null, null);
        _flyers.AddChild(card);
        var note = NoteOf(id, _flyers);
        Inked(note, id, BoardShow.Note, default);
        SizeOf(note, (float)_camera.Zoom);
        note.Name.Visible = Words;
        var flight = new Flight(id, card, note, _card.GlobalPosition - _view.GlobalPosition + _card.Size / 2);
        _flights.Add(flight);
        Fly(flight, 0);
        var tween = NewTween();
        tween.TweenMethod(Callable.From<float>(t => Fly(flight, t)), 0f, 1f, Slide);
        tween.TweenCallback(Callable.From(() => Landed(flight)));
    }

    /// <summary>A flight <paramref name="t"/> of the way, to where the camera has its spot now: the card shrinks whole, then is the note it comes down as.</summary>
    private void Fly(Flight flight, float t)
    {
        float u = BoardInk.Ease(t), zoom = (float)_camera.Zoom;
        var at = flight.From.Lerp(new Vector2((float)_camera.X, (float)_camera.Y) + At(flight.Id) * zoom, u);
        bool card = u <= NoteFrom;
        flight.Card.Visible = card;
        flight.Note.Root.Visible = !card;
        if (card)
        {
            float scale = 1 - u / NoteFrom * (1 - CardLeast);
            flight.Card.Scale = new Vector2(scale, scale);
            flight.Card.Position = at - flight.Card.Size / 2;
            return;
        }
        float small = NoteLands * zoom, wide = small + (flight.Card.Size.X * CardLeast - small) * (1 - u) / (1 - NoteFrom);
        flight.Note.Root.Position = at;
        flight.Note.Root.Scale = new Vector2(wide / small, wide / small);
    }

    /// <summary>A flight's end: the note stands at its place, and its threads draw themselves.</summary>
    private void Landed(Flight flight)
    {
        string id = flight.Id;
        flight.Card.QueueFree();
        _down.Add(flight.Note.Root);
        _flights.Remove(flight);
        _growing[id] = 0;
        var tween = NewTween();
        tween.TweenMethod(Callable.From<float>(t =>
        {
            _growing[id] = t;
            _back.QueueRedraw();
        }), 0f, 1f, ThreadDraws);
        tween.TweenCallback(Callable.From(() =>
        {
            _growing.Remove(id);
            _back.QueueRedraw();
        }));
        ReadSoon();
    }

    /// <summary>Takes the middle of the board's part of the view to a point of the board at <paramref name="zoom"/>, in the board's own terms, for the box may grow under it.</summary>
    private void GlideTo(double x, double y, double zoom, double seconds)
    {
        _glide?.Kill();
        var (mapX, mapY) = _camera.ToMap(Wide / 2, _view.Size.Y / 2);
        var (fromX, fromY) = (mapX + _box.Left, mapY + _box.Top);
        double fromZoom = _camera.Zoom;
        float fromShare = _share, toShare = _narrow ? SideShare : 0;
        _glide = _view.CreateTween();
        _glide.TweenMethod(Callable.From<float>(t =>
        {
            _share = Mathf.Lerp(fromShare, toShare, t);
            _camera.Fit(Wide, _view.Size.Y, _box.Wide, _box.Tall);
            _camera.Look(fromX + (x - fromX) * t - _box.Left, fromY + (y - fromY) * t - _box.Top, fromZoom * Math.Pow(zoom / fromZoom, t));
        }), 0f, 1f, seconds).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
    }

    /// <summary>Holds what will stand on the board in its part of the view, still while the entries land; if too much for that at a size with words, the entry at hand.</summary>
    private void Frame(bool now)
    {
        if (BoardInk.Around(_ahead.Select(id => (SpotOf(id).X, SpotOf(id).Y)), FrameWide, FrameTall) is not var (left, top, right, bottom)) return;
        double zoom = TechCamera.Fitting(Wide, _view.Size.Y, right - left, bottom - top);
        (double X, double Y, double Zoom) to = zoom >= WordsFrom ? ((left + right) / 2, (top + bottom) / 2, zoom) : (SpotOf(_pile.Top!).X, SpotOf(_pile.Top!).Y, WordsFrom);
        if (to == _framed) return;
        _framed = to;
        if (now) _camera.Look(to.X - _box.Left, to.Y - _box.Top, to.Zoom);
        else GlideTo(to.X, to.Y, to.Zoom, Slide);
    }

    private Tween NewTween()
    {
        var tween = _view.CreateTween();
        _tweens.Add(tween);
        tween.Finished += () => _tweens.Remove(tween);
        return tween;
    }

    /// <summary>Every flight and glide over now, with nothing more to happen.</summary>
    private void Stop()
    {
        foreach (var tween in _tweens) tween.Kill();
        _tweens.Clear();
        foreach (var flight in _flights)
        {
            flight.Card.QueueFree();
            flight.Note.Root.QueueFree();
        }
        _flights.Clear();
        ClearDown();
        _growing.Clear();
        Hold();
    }

    private void ClearDown()
    {
        foreach (var note in _down) note.QueueFree();
        _down.Clear();
    }

    /// <summary>The player has taken the board: the glide lets go of it.</summary>
    private void Hold()
    {
        _glide?.Kill();
        _glide = null;
        _share = _narrow ? SideShare : 0;
    }

    /// <summary>The pile gone and the last flight down: the board opens to the whole view, framing all that stands. Too much to frame, it goes to the shape closest to done.</summary>
    private void Settle()
    {
        _narrow = false;
        var shapes = _order.Select(s => s.Id).Where(id => _picture.Shows.GetValueOrDefault(id) == BoardShow.Silhouette);
        string? closest = BoardPile.Closest(shapes, _read!, BoardThings.All);
        var shown = _picture.Shows.Where(s => s.Value != BoardShow.Unseen).Select(s => (SpotOf(s.Key).X, SpotOf(s.Key).Y));
        var around = BoardInk.Around(shown, FrameWide, FrameTall);
        double zoom = around is var (left, top, right, bottom) ? TechCamera.Fitting(_view.Size.X, _view.Size.Y, right - left, bottom - top) : 0;
        if (zoom >= WordsFrom && around is var (l, t, r, b)) GlideTo((l + r) / 2, (t + b) / 2, zoom, Settling);
        else if (closest != null) GlideTo(SpotOf(closest).X, SpotOf(closest).Y, WordsFrom, Settling);
        else _share = 0;
    }

    /// <summary>The first thing <paramref name="thing"/> lacks, in words: what was never had is not named.</summary>
    private string LackOf(BoardThing thing)
    {
        var lack = BoardPile.Lack(thing, _read!);
        if (lack.Parts is [var (item, more), ..]) return _had.Contains(item) ? T("research.pile.lackPart", ("n", more), ("what", _strip.NameOf(item))) : T("research.pile.unknown");
        if (lack.Station is { } station) return _had.Contains(station) ? T("research.pile.lackStation", ("what", _strip.NameOf(station))) : T("research.pile.lackNoStation");
        return "";
    }

    /// <summary><paramref name="read"/> without what is unread or in the air, for it is not on the board yet.</summary>
    private BoardRead OnTheBoard(BoardRead read)
    {
        var away = _keep.Seen.Unread.Concat(_flights.Select(f => f.Id)).ToHashSet(StringComparer.Ordinal);
        return away.Count == 0 ? read : read with { Found = read.Found.Where(id => !away.Contains(id)).ToList(), Granted = read.Granted.Where(id => !away.Contains(id)).ToList() };
    }

    /// <summary>What the board reads of <paramref name="m"/>.</summary>
    public static BoardRead ReadOf(Registry m) =>
        new(m.Knowledge.Obtained.Select(t => t.Item).ToList(), m.Knowledge.GrantedIds, m.Knowledge.ObtainedOf, kind => m.Grid.MachinesOf(kind).Count > 0);

    public Vector2 ScreenOf(string id) => _view.GlobalPosition + _notes[id].Root.Position;

    public void Show(bool shown)
    {
        _out = _map.Visible = _front.Visible = _flyers.Visible = shown;
        if (shown) _strip.Action.Visible = false;
        else
        {
            _picked = null;
            _find.Drop();
            _find.Visible = false;
        }
        Reset();
        _key = null;
    }

    /// <summary>A showing over: what flew ends, and the pile is worked out afresh.</summary>
    private void Reset()
    {
        _narrow = false;
        Stop();
        _pile.Reset();
        _side.Visible = _flipHeld = false;
        _framed = null;
        _ahead = [];
    }

    public void Step(double delta)
    {
        if (_notes.Count == 0 && !Reading) return;
        if (_view.Size != _viewWas) Grow();
        _board.Size = new Vector2(Wide, _view.Size.Y);
        _camera.Fit(Wide, _view.Size.Y, _box.Wide, _box.Tall);
        if (!_centred)
        {
            _centred = true;
            _camera.Centre(-_box.Left, -_box.Top);
        }
        var at = (_camera.X, _camera.Y, _camera.Zoom);
        if (at == _placedAt) return;
        _placedAt = at;
        Place();
    }

    /// <summary>Everything where the camera has it now: only when it has moved, or what is shown has changed.</summary>
    private void Place()
    {
        float zoom = (float)_camera.Zoom;
        var origin = new Vector2((float)_camera.X, (float)_camera.Y).Round();
        _map.Scale = new Vector2(zoom, zoom);
        _map.Position = origin;
        if (_sizedAt != _camera.Zoom) SetSizes(zoom);
        bool words = Words;
        foreach (var (id, view) in _notes)
        {
            if (!view.Root.Visible) continue;
            view.Root.Position = (origin + At(id) * zoom).Round();
            bool note = view.Show == BoardShow.Note;
            view.Name.Visible = words && note;
            view.Lack.Visible = words && !note && view.LackWords.Text.Length > 0;
        }
    }

    /// <summary>Sets every note at the size <paramref name="zoom"/> shows it; words are never scaled, so they stay sharp.</summary>
    private void SetSizes(float zoom)
    {
        _sizedAt = _camera.Zoom;
        foreach (var view in _notes.Values) SizeOf(view, zoom);
        _back.QueueRedraw();
    }

    private void SizeOf(NoteView view, float zoom)
    {
        bool words = Words;
        var half = new Vector2(HalfWide, HalfTall) * zoom;
        view.Box.Position = -half;
        view.Box.Size = half * 2;
        view.Box.QueueRedraw();
        float icon = words ? Icon * zoom : HalfWide * 2 * zoom * 0.75f;
        view.Picture.Position = new Vector2(-icon / 2, -icon / 2 - (words ? PictureUp * zoom : 0));
        view.Picture.Size = new Vector2(icon, icon);
        view.Picture.TextureFilter = view.Show != BoardShow.Note || zoom < 1 ? CanvasItem.TextureFilterEnum.Linear : CanvasItem.TextureFilterEnum.Nearest;
        view.Name.LabelSettings.FontSize = Math.Max(8, Mathf.RoundToInt(NameSize * zoom));
        view.Name.Position = new Vector2(-NameWide / 2, NameTop) * zoom;
        view.Name.Size = new Vector2(NameWide, NameTall) * zoom;
        view.LackWords.LabelSettings.FontSize = view.Name.LabelSettings.FontSize;
        view.Lack.Position = new Vector2(-NameWide / 2, LackTop) * zoom;
        view.Lack.Size = new Vector2(NameWide, LackTall) * zoom;
        if (view.LackPicture != null) view.LackPicture.CustomMinimumSize = new Vector2(LackPicture, LackPicture) * zoom;
        view.Mark.Scale = new Vector2(zoom, zoom);
        view.Mark.Width = LoopWidth / zoom;
    }

    /// <summary>A read of the world: nothing done unless a count, a grant, a machine or what is picked has moved.</summary>
    public void Read(Registry m)
    {
        if (!_out) return;
        if (!ReferenceEquals(m.Knowledge, _knowledge)) Forget(m);
        var key = ((object)m.Knowledge, m.Knowledge.Version, (object)m.Grid, m.Grid.Version, _picked);
        if (_key == key) return;
        _key = key;
        var read = _read = ReadOf(m);
        var slots = _order = BoardSlots.Of(read.Found, BoardThings.All, read.Granted);
        _slots = slots.ToDictionary(s => s.Id, StringComparer.Ordinal);
        _keep.Seen.Read(BoardView.NotesOf(slots, read));
        _pile.Read(_keep.Seen, slots, BoardThings.All);
        bool starting = Reading && !_side.Visible;
        _side.Visible = Reading;
        if (starting)
        {
            _picked = null;
            _narrow = true;
            _share = SideShare;
            Hold();
        }
        var drawn = OnTheBoard(read);
        _picture = BoardView.Of(slots, BoardThings.All, drawn, _picked);
        _ahead = Reading ? BoardView.Of(slots, BoardThings.All, read, null).Shows.Where(s => s.Value != BoardShow.Unseen).Select(s => s.Key).ToList() : [];
        _had = [.. drawn.Found, .. drawn.Granted];
        if (_pile.Top is { } top) _had.Add(top);
        Restyle();
        ShowFind();
        Grow();
        if (Reading)
        {
            _centred = true;
            Frame(starting);
        }
        Place();
        ClearDown();
        _back.QueueRedraw();
        Fill();
        if (_narrow && !Reading && _flights.Count == 0) Settle();
    }

    /// <summary>Another world, loaded or resynced: its board starts afresh, what was moved kept through a resync, which is the same world.</summary>
    private void Forget(Registry m)
    {
        _knowledge = m.Knowledge;
        _keep.Of(_worldId());
        _strip.Learn(m.Research);
        foreach (var view in _notes.Values) view.Root.QueueFree();
        _notes.Clear();
        (_box, _viewWas, _picked, _key, _sizedAt, _centred) = (default, default, null, null, null, false);
        Reset();
    }

    private void Restyle()
    {
        foreach (var (id, view) in _notes) view.Root.Visible = _picture.Shows.GetValueOrDefault(id) != BoardShow.Unseen;
        foreach (var (id, show) in _picture.Shows)
        {
            if (show == BoardShow.Unseen) continue;
            if (!_notes.TryGetValue(id, out var view))
            {
                view = _notes[id] = NoteOf(id, _front);
                // Tips go by a control's rectangle alone, and a note may stand behind the side.
                _strip.Tips.Add(view.Box, () => _side.Visible && _side.GetGlobalRect().HasPoint(_side.GetGlobalMousePosition()) ? null : TipOf(id));
            }
            bool note = show == BoardShow.Note;
            double pencil = note ? 1 : BoardPencil.Of(BoardThings.All.First(t => t.Id == id), _read!.Count, _read.Stands);
            Inked(view, id, show, note ? default : BoardInk.SketchOf(pencil));
            view.Box.QueueRedraw();
            view.Mark.Visible = note && _keep.Seen.Marked(id);
            if (!note) SayLack(view, BoardThings.All.First(t => t.Id == id));
            if (note) view.Picture.Material = null;
            else Sketched(view.Picture, pencil);
            view.Root.Modulate = Lit(id) ? Colors.White : Dimmed;
        }
    }

    /// <summary>Works out the strokes of <paramref name="view"/> when what it shows has changed: a note's whole box, of a shape as much of its corners as its pencil has.</summary>
    private void Inked(NoteView view, string id, BoardShow show, BoardSketch sketch)
    {
        if (view.Show == show && view.Sketch == sketch) return;
        (view.Show, view.Sketch) = (show, sketch);
        float seed = BoardInk.Seed(id);
        view.Ink = show == BoardShow.Note ? BoardInk.Box(InkLeft, InkTop, InkWide, InkTall, seed, 4, 12) : BoardInk.Corners(InkLeft, InkTop, InkWide, InkTall, sketch.Side, seed);
        _sizedAt = null;
    }

    /// <summary>Writes in a shape's box the first thing it lacks: how many and its picture, which stays readable when the board is small. What was never had shows no picture.</summary>
    private void SayLack(NoteView view, BoardThing thing)
    {
        var lack = BoardPile.Lack(thing, _read!);
        string? what = lack.Parts.Count > 0 ? lack.Parts[0].Item : lack.Station;
        string words = lack.Parts.Count > 0 ? T(_had.Contains(what!) ? "research.board.more" : "research.board.moreUnknown", ("n", lack.Parts[0].More)) : what == null ? "" : T(_had.Contains(what) ? "research.board.needs" : "research.board.needsUnknown");
        view.LackWords.Text = words;
        string? shown = what != null && _had.Contains(what) ? what : null;
        if (shown == view.LackOf) return;
        view.LackOf = shown;
        view.LackPicture?.QueueFree();
        view.LackPicture = null;
        if (shown == null) return;
        view.LackPicture = _strip.Picture(shown, Mathf.RoundToInt(LackPicture));
        view.LackPicture.TextureFilter = CanvasItem.TextureFilterEnum.Linear;
        view.LackPicture.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        view.Lack.AddChild(view.LackPicture);
        _sizedAt = null;
    }

    /// <summary>The box grown to what is shown and the view, the camera dragged by what it grew left and up, so nothing jumps.</summary>
    private void Grow()
    {
        _viewWas = _view.Size;
        var box = (_box == default ? BoardBox.Round(_view.Size.X, _view.Size.Y) : _box).Holding(BoardBox.Round(_view.Size.X, _view.Size.Y));
        foreach (var (id, show) in _picture.Shows)
            if (show != BoardShow.Unseen) box = box.Holding(SpotOf(id).X, SpotOf(id).Y);
        // What is on its way to a spot must be able to get there in view.
        foreach (string id in _flights.Select(f => f.Id).Concat(_ahead)) box = box.Holding(SpotOf(id).X, SpotOf(id).Y);
        double left = _box.Left - box.Left, up = _box.Top - box.Top;
        _box = box;
        // A control is culled by its size: with none, all its threads go as its corner leaves the view.
        _back.Size = new Vector2((float)box.Wide, (float)box.Tall);
        LaySide();
        _camera.Fit(Wide, _view.Size.Y, box.Wide, box.Tall);
        if (_centred) _camera.Drag(-left * _camera.Zoom, -up * _camera.Zoom);
        _placedAt = null;
    }

    private NoteView NoteOf(string id, Control parent)
    {
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        var box = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(box);
        var picture = _strip.Picture(id, Icon);
        picture.CustomMinimumSize = Vector2.Zero;
        root.AddChild(picture);
        var name = Text(_strip.NameOf(id), Light, NameSize);
        name.LabelSettings.LineSpacing = -2;
        name.HorizontalAlignment = HorizontalAlignment.Center;
        name.VerticalAlignment = VerticalAlignment.Center;
        name.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        name.ClipText = true;
        name.MaxLinesVisible = 2;
        root.AddChild(name);
        var lack = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center, Visible = false }, 2);
        var lackWords = Text("", Dim, NameSize);
        lack.AddChild(lackWords);
        root.AddChild(lack);
        var loop = BoardInk.Loop(BoardInk.Seed(id), LoopWide, LoopTall);
        var mark = new Line2D
        {
            Points = loop.Select(p => new Vector2(p.X, p.Y)).ToArray(),
            Width = LoopWidth,
            DefaultColor = New,
            JointMode = Line2D.LineJointMode.Round,
            BeginCapMode = Line2D.LineCapMode.Round,
            EndCapMode = Line2D.LineCapMode.Round,
            Visible = false,
        };
        root.AddChild(mark);
        parent.AddChild(root);
        _sizedAt = null;
        var view = new NoteView(root, box, picture, name, mark, lack, lackWords);
        box.Draw += () => DrawNote(id, view);
        return view;
    }

    /// <summary>A note: its fill and its box written by hand. A shape: what its pencil has of the box's corners, nothing behind.</summary>
    private void DrawNote(string id, NoteView view)
    {
        float scale = (float)_camera.Zoom * InkScale;
        view.Box.DrawSetTransform(view.Box.Size / 2, 0, new Vector2(scale, scale));
        Color? tie = Tie(id);
        if (view.Show == BoardShow.Note)
        {
            view.Box.DrawRect(new Rect2(InkLeft + 1, InkTop + 1, InkWide - 2, InkTall - 2), NoteFill);
            InkDraw.Strokes(view.Box, view.Ink, tie ?? Drawn, tie != null ? 4 : 3, InkDraw.Least / scale);
        }
        else InkDraw.Strokes(view.Box, view.Ink, tie ?? Drawn with { A = view.Sketch.Edge * 0.8f }, tie != null ? 3 : 2.4f, InkDraw.Least / scale);
    }

    /// <summary>The board's pointer: any button drags it, the left one on a thing drags that instead, and picks it if it was not dragged.</summary>
    public void Pointer(InputEvent e)
    {
        if (_flipHeld)
        {
            if (e is InputEventMouseMotion { ButtonMask: not 0 }) return;
            _flipHeld = false;
        }
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                if (button.Pressed)
                {
                    Hold();
                    Press(button.Position);
                }
                else if (_pressed && _moved < ClickSlack) Click(button.Position);
                if (!button.Pressed) LetGo();
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown }:
                Zoom(e, _view);
                break;
            case InputEventMouseMotion { ButtonMask: not 0 } drag:
                Hold();
                _moved += drag.Relative.Length();
                if (_held == null) _camera.Drag(drag.Relative.X, drag.Relative.Y);
                else if (_moved >= ClickSlack) Stand(_held, drag.Position);
                break;
        }
    }

    public void LetGo() => (_pressed, _held) = (false, null);

    /// <summary>Whether a thing is being carried: the pointer says nothing of it meanwhile.</summary>
    public bool Carrying => _held != null && _moved >= ClickSlack;

    /// <summary>A left press at a point of the view: takes hold of what is there, on top of the rest from now on.</summary>
    private void Press(Vector2 at)
    {
        (_pressed, _moved, _held) = (true, 0, ThingAt(at));
        if (_held is not { } id) return;
        _notes[id].Root.MoveToFront();
        _keep.Seen.Touch(id);
        _notes[id].Mark.Visible = false;
        var (x, y) = _camera.ToMap(at.X, at.Y);
        _heldBy = new Vector2((float)x, (float)y) - At(id);
    }

    /// <summary>Stands what is held under the pointer, by where it was taken; only it moves, and the threads with it.</summary>
    private void Stand(string id, Vector2 at)
    {
        var (x, y) = _camera.ToMap(at.X, at.Y);
        _keep.Move(id, new BoardSpot(x - _heldBy.X + _box.Left, y - _heldBy.Y + _box.Top));
        Grow();
        _back.QueueRedraw();
    }

    public void Zoom(InputEvent e, Control over)
    {
        if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown } wheel) return;
        var at = _view.GetLocalMousePosition();
        Hold();
        _camera.Wheel(wheel.ButtonIndex == MouseButton.WheelUp ? 1 : -1, at.X, at.Y);
        over.AcceptEvent();
    }

    /// <summary>A click at a point of the view: picks what is shown there; off everything, picks nothing. Reading, the entry already says it.</summary>
    private void Click(Vector2 at)
    {
        if (Reading) return;
        _picked = ThingAt(at);
        _readSoon();
    }

    /// <summary>The search over the board while anything stands on it and no entry is being read; it lets go of the keys as it goes.</summary>
    private void ShowFind()
    {
        bool shown = _out && !Reading && _notes.Values.Any(v => v.Root.Visible);
        if (!shown && _find.Typing) _find.Drop();
        _find.Visible = shown;
    }

    /// <summary>What the search can find: whatever the board shows now, by the name it goes by.</summary>
    private IEnumerable<BoardFound> Findable() =>
        _picture.Shows.Where(s => s.Value != BoardShow.Unseen && _notes.ContainsKey(s.Key)).Select(s => new BoardFound(s.Key, _strip.NameOf(s.Key), _strip.NameIn(Speaking == Language.Russian ? Language.English : Language.Russian, s.Key), s.Value == BoardShow.Note));

    /// <summary>The search's list opened, changed or closed: the board lights what it lists, or what the pick ties to again.</summary>
    private void Listed(IReadOnlyCollection<string>? ids)
    {
        _listed = ids;
        foreach (var (id, view) in _notes) view.Root.Modulate = Lit(id) ? Colors.White : Dimmed;
        _back.QueueRedraw();
    }

    /// <summary>Whether <paramref name="id"/> stands out: listed by the open search, else tied to the pick, else everything does.</summary>
    private bool Lit(string id) => _listed?.Contains(id) ?? (_picked == null || Tie(id) != null);

    /// <summary>What the search picked: picked as by a click, on top of the rest, and the board glides to it at a size with words.</summary>
    private void Find(string id)
    {
        if (Reading || !_notes.TryGetValue(id, out var view) || !view.Root.Visible) return;
        _picked = id;
        view.Root.MoveToFront();
        GlideTo(SpotOf(id).X, SpotOf(id).Y, Math.Max(_camera.Zoom, WordsFrom), Found);
        _readSoon();
    }

    /// <summary>Whether the board's search has the keys.</summary>
    public bool Typing => _find.Typing;

    /// <summary>The Search key (/): puts the keys in the board's search, while it shows.</summary>
    public void TypeSearch()
    {
        if (_find.Visible) _find.Type();
    }

    /// <inheritdoc cref="BoardFind.Drop"/>
    public bool DropSearch() => _find.Drop();

    /// <summary>What is shown at a point of the view, the topmost; none if nothing is.</summary>
    private string? ThingAt(Vector2 at)
    {
        var (x, y) = _camera.ToMap(at.X, at.Y);
        var point = new Vector2((float)x, (float)y);
        var half = new Vector2(HalfWide, HalfTall);
        return _notes.Where(n => n.Value.Root.Visible).Select(n => n.Key).Where(id => new Rect2(At(id) - half, half * 2).HasPoint(point)).MaxBy(id => _notes[id].Root.GetIndex());
    }

    /// <summary>The strip below, built again when anything read has moved: what is known of what is picked.</summary>
    public void Fill()
    {
        _strip.Clear();
        FillCard();
        var facts = _strip.Facts;
        if (_read is not { } read || read.Found.Count == 0) return;
        if (Reading || _picked is not { } id)
        {
            var hint = Text(T(Reading ? "research.pile.hint" : "research.board.hint"), Style.Slate500);
            hint.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            facts.AddChild(hint);
            return;
        }
        var thing = BoardThings.All.FirstOrDefault(t => t.Id == id);
        double? pencil = _had.Contains(id) || thing == null ? null : BoardPencil.Of(thing, read.Count, read.Stands);
        facts.AddChild(_strip.Title(_strip.NameOf(id), KindOf(id), id, TipOf, pencil));
        FactsOf(id, facts);
    }

    private static string KindOf(string id) => Table.ContainsKey($"item.{id}.kind") ? T($"item.{id}.kind") : "";

    /// <summary>The entry shown large and the count of the rest; a new top comes up from the side.</summary>
    private void FillCard()
    {
        _stack.QueueRedraw();
        _cardNow?.QueueFree();
        _cardNow = null;
        if (_pile.Top is not { } id)
        {
            _cardFor = null;
            return;
        }
        _count.Text = _keep.Seen.Unread.Count.ToString();
        var card = _cardNow = CardOf(id);
        _card.AddChild(card);
        if (_cardFor != id)
        {
            ComeUp(card, 0);
            NewTween().TweenMethod(Callable.From<float>(t => ComeUp(card, t)), 0f, 1f, CardComes);
        }
        _cardFor = id;
    }

    private static void ComeUp(Control card, float t)
    {
        if (!GodotObject.IsInstanceValid(card)) return;
        float u = BoardInk.Ease(t);
        card.Position = new Vector2((1 - u) * CardFrom, 0);
        card.Modulate = new Color(1, 1, 1, u < 0.5f ? 0.5f : 1);
    }

    /// <summary>The entry written out: its box by hand, picture, name and what it is, and what is known of it in lines.</summary>
    private Control CardOf(string id)
    {
        var card = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, Size = _card.Size, PivotOffset = _card.Size / 2 };
        float seed = BoardInk.Hash(id) % 97 / 9f;
        card.Draw += () =>
        {
            card.DrawRect(new Rect2(Vector2.Zero, card.Size), CardFill);
            InkDraw.Strokes(card, BoardInk.Box(0, 0, card.Size.X, card.Size.Y, seed, 5, 14), Drawn, 1.8f);
        };
        var words = Style.Gap(new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 8);
        words.Position = new Vector2(CardPad, CardPad);
        words.Size = new Vector2(_card.Size.X - 2 * CardPad, 0);
        var picture = _strip.Picture(id, CardPicture);
        picture.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        _strip.Tips.Add(picture, () => TipOf(id));
        words.AddChild(picture);
        var name = Text(_strip.NameOf(id), Light, CardName);
        name.HorizontalAlignment = HorizontalAlignment.Center;
        name.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        words.AddChild(name);
        var kind = Text(KindOf(id), Dim, Tablet.HintSize + 1);
        kind.HorizontalAlignment = HorizontalAlignment.Center;
        kind.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        words.AddChild(kind);
        foreach (string line in LinesOf(id))
        {
            var row = Text(line, Light, CardRow);
            row.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            words.AddChild(row);
        }
        card.AddChild(words);
        return card;
    }

    /// <summary>What the entry says of <paramref name="id"/>: what it takes, where it is made, what it goes into, and what it leads to. What was never had is not named.</summary>
    private IEnumerable<string> LinesOf(string id)
    {
        string Line(string label, string what) => T("research.pile.row", ("label", T(label)), ("what", what));
        var link = TechLinks.Of(id);
        switch (TechLinks.OriginOf(id))
        {
            case Origin.Made:
                yield return Line("research.takes", string.Join(", ", link.Takes.Select(t => T("research.taken", ("n", t.Count), ("what", _strip.NameOf(t.Item))))));
                break;
            case Origin.Worked:
                yield return Line("research.takes", string.Join(T("research.pile.or"), link.From.Select(f => _had.Contains(f) ? _strip.NameOf(f) : T("research.pile.noItem")).Distinct()));
                break;
            case Origin.Free:
                yield return T("research.pile.free");
                break;
            case Origin.Found:
                yield return T("research.pile.found");
                break;
        }
        if (link.MadeAt.Count > 0)
            yield return Line("research.madeAt", string.Join(", ", link.MadeAt.Select(s => _had.Contains(s) ? _strip.NameOf(s) : T("research.pile.noStation")).Distinct()));
        else if (TechLinks.MenuTabOf(id) != null)
            yield return Line("research.builtAt", BuiltFrom(id));
        var named = link.UsedIn.Where(u => _had.Contains(u) || _picture.Shows.GetValueOrDefault(u) != BoardShow.Unseen).Take(3).Select(_strip.NameOf).ToList();
        if (named.Count > 0)
        {
            string names = string.Join(", ", named);
            int more = link.UsedIn.Count - named.Count;
            yield return Line("research.usedIn", more == 0 ? names : T("research.pile.andMore", ("names", names), ("n", more)));
        }
        double Pencil(BoardThing thing) => BoardPencil.Of(thing, _read!.Count, _read.Stands);
        bool Shape(string other) => other != id && _picture.Shows.ContainsKey(other) && _picture.Shows[other] != BoardShow.Note;
        if (BoardThreads.Of(id, BoardThings.All, Shape, Pencil) is [var next, ..])
        {
            string lack = LackOf(BoardThings.All.First(t => t.Id == next));
            yield return Line("research.pile.next", lack.Length == 0 ? _strip.NameOf(next) : $"{_strip.NameOf(next)} — {lack}");
        }
    }

    /// <summary>Where the player finds the blueprint <paramref name="id"/> to build: the build menu and its tab.</summary>
    private static string BuiltFrom(string id) =>
        TechLinks.MenuTabOf(id) is { } tab ? T("research.builtInPlace", ("menu", T("build.key")), ("tab", T($"build.tab.{tab}"))) : T("build.key");

    /// <summary>What is known of <paramref name="id"/>: what it takes, where it is made, what it goes into.</summary>
    private void FactsOf(string id, Control facts)
    {
        bool note = _had.Contains(id);
        var thing = BoardThings.All.FirstOrDefault(t => t.Id == id);
        var link = TechLinks.Of(id);
        if (link.Takes.Count > 0)
        {
            var takes = Flow(10);
            foreach (var (item, count) in link.Takes) takes.AddChild(PartOf(item, count, note));
            string heading = note || thing == null ? "research.takes" : BoardPile.Lack(thing, _read!).Parts.Count > 0 ? "research.board.needMore" : "research.board.haveEnough";
            facts.AddChild(Fact(T(heading), From, takes, 3));
        }
        else if (link.From.Count > 0)
            facts.AddChild(Fact(T("research.takes"), From, _strip.Pictures(link.From, TipOf, source => !_had.Contains(source)), 3));
        if (thing != null || link.MadeAt.Count > 0)
        {
            var at = Flow(10);
            foreach (string station in link.MadeAt) at.AddChild(StationOf(station));
            bool built = link.MadeAt.Count == 0 && thing!.Stations == null;
            if (link.MadeAt.Count == 0) at.AddChild(Text(built ? BuiltFrom(id) : T("research.atLifepod"), Style.Slate500));
            facts.AddChild(Fact(T(built ? "research.builtAt" : "research.madeAt"), MadeAt, at, 2));
        }
        if (link.UsedIn.Count > 0) facts.AddChild(Fact(T("research.usedIn"), UsedIn, _strip.Pictures(link.UsedIn, TipOf, used => !_had.Contains(used)), 5));
    }

    /// <summary>A part: of what is known, as in a recipe; of a shape, what was had of it over enough; unseen yet, a pale picture with no name.</summary>
    private Control PartOf(string item, int count, bool known)
    {
        string name = _strip.NameOf(item);
        if (known) return _strip.Named(item, T("research.taken", ("n", count), ("what", name)), Style.Slate100);
        if (!_had.Contains(item)) return _strip.Unseen(item);
        int have = _read!.Count(item), enough = Understanding.Enough(item);
        // Enough had is a tick: the count would only climb on past it, telling nothing more.
        return have >= enough
            ? _strip.Named(item, T("research.board.partDone", ("what", name)), Bought)
            : _strip.Named(item, T("research.board.part", ("what", name), ("have", have), ("enough", enough)), Style.Slate100);
    }

    /// <summary>A station: named while one stands; a pale picture while none does, named by the pointer only if it is known at all.</summary>
    private Control StationOf(string station)
    {
        if (_read!.Stands(station)) return _strip.Named(station, _strip.NameOf(station), Style.Slate100);
        var pale = _strip.Unseen(station);
        if (_had.Contains(station)) _strip.Tips.Add(pale, () => TipOf(station));
        return pale;
    }

    /// <summary>What the pointer says of <paramref name="id"/>: all of what is known, the name alone of a shape.</summary>
    private Tip TipOf(string id)
    {
        if (!_had.Contains(id)) return new Tip(_strip.NameOf(id));
        if (_strip.PlacementOf(id) is not { } placement) return new Tip(_strip.NameOf(id)) { Card = ItemCards.Of(id) };
        var lines = new List<TipLine>();
        if (Table.ContainsKey($"tool.{id}.about")) lines.Add(new TipLine(T($"tool.{id}.about"), Tone.Dim));
        return new Tip(_strip.NameOf(id), lines) { Picture = placement };
    }

    /// <summary>The colour that ties <paramref name="id"/> to what is picked; none if nothing does.</summary>
    private Color? Tie(string id)
    {
        if (_picked is not { } picked) return null;
        return picked == id ? Picked : TechLinks.TieOf(picked, id) is { } tie ? ColourOf(tie) : null;
    }

    /// <summary>Where <paramref name="id"/> stands on the board: where it was moved to, else its slot.</summary>
    private BoardSpot SpotOf(string id) => _keep.Spots.TryGetValue(id, out var spot) ? spot : new BoardSpot(_slots[id].X, _slots[id].Y);

    /// <summary>Where <paramref name="id"/> stands on the map, whose top-left is the box's.</summary>
    private Vector2 At(string id) => new((float)(SpotOf(id).X - _box.Left), (float)(SpotOf(id).Y - _box.Top));

    /// <summary>The strokes of the thread between two things on the board, worked out again only once one has moved.</summary>
    private InkPoint[] ThreadOf(string from, string to)
    {
        BoardSpot a = SpotOf(from), b = SpotOf(to);
        if (_threads.TryGetValue((from, to), out var kept) && kept.A == a && kept.B == b) return kept.Ink;
        var ink = BoardInk.Thread($"{from}|{to}", (float)a.X, (float)a.Y, (float)b.X, (float)b.Y, ThreadHalfWide, ThreadHalfTall);
        _threads[(from, to)] = (a, b, ink);
        return ink;
    }

    /// <summary>One canvas under the notes: the threads, and the lines from what is picked to what it ties to. Their width stays as seen, whatever the zoom.</summary>
    private void DrawBack()
    {
        float seen = 1 / (float)_camera.Zoom;
        _back.DrawSetTransform(new Vector2((float)-_box.Left, (float)-_box.Top));
        foreach (var (note, shape) in _picture.Threads)
        {
            bool fresh = _keep.Seen.Fresh(note);
            float drawn = _growing.GetValueOrDefault(note, 1);
            var tip = InkDraw.Stroke(_back, ThreadOf(note, shape), (fresh ? Fresh : Plain) with { A = (_listed != null ? Lit(note) && Lit(shape) : _picked == null) ? 1 : 0.3f }, (fresh ? 1.5f : 1.1f) * seen, InkDraw.Least * seen, drawn);
            if (drawn < 1) InkDraw.Nib(_back, tip, Light, seen);
        }
        if (_listed != null || _picked == null || !_slots.ContainsKey(_picked)) return;
        foreach (var (id, show) in _picture.Shows)
            if (show != BoardShow.Unseen && id != _picked && Tie(id) is { } colour) InkDraw.Stroke(_back, ThreadOf(_picked, id), colour with { A = 0.7f }, 2 * seen, InkDraw.Least * seen);
    }
}
