using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Content;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Research;
using static Stillwake.Game.Features.Research.StripParts;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The tablet's first tab: the scavenger's board (docs/plans/board.md), blank till the first find. Notes in ink where <see cref="BoardSlots"/>
/// stands them, threads to shapes in pencil, dragged and zoomed (<see cref="TechCamera"/>). A click picks and says what is known of it in
/// the strip below; it buys nothing, for it takes no way to send a command. A thing dragged stays where it is let go, on this machine only (<see cref="BoardKeep"/>). What landed has a loop drawn round it until the tablet is put away ([tablet-key](../../../docs/decisions/2026-10-31-tablet-key.md)).
/// Unread entries wait beside the board, one shown large; a click lands it at its place ([the pile](../../../docs/decisions/2026-11-02-board-pile.md), <see cref="BoardPile"/>).
/// </summary>
public sealed class BoardPage : ITabletPage
{
    private static readonly Color Paper = new("18202b"), Ink = new("8a93a6"), Blank = new("0f141b"), Pencil = new("39404c"), Thread = new("7a8090"), New = new("f2b84b");

    /// <summary>Under this zoom words would be too small to read, and only pictures show.</summary>
    private const double WordsFrom = 0.5;
    /// <summary>A note's sizes on the board at zoom 1, and its word's.</summary>
    private const int NoteSide = 64, Icon = 40, NameSize = 12;
    /// <summary>The loop round a new note and its word, from the note's centre at zoom 1, drawn by hand: wobbling, its ends overlapping.</summary>
    private static readonly Vector2[] Loop = LoopPoints();
    private const float LoopWidth = 2;
    /// <summary>What of the view the unread take while there are any, the picked candidate's; and their parts' sizes.</summary>
    private const float SideShare = 0.45f, SideGap = 12, StackWide = 118, StackTall = 140, TakeTall = 44, SheetWide = StackWide - SheetBy * (Sheets - 1);
    /// <summary>The entry's picture and name, and how far the picture stands from the card's top.</summary>
    private const int CardPicture = 96, CardName = 22, CardPad = 16;
    /// <summary>The outlines shown of the unread, and how far each stands from the one before.</summary>
    private const int Sheets = 4, SheetBy = 7;
    /// <summary>Seconds of an entry's flight and the board's glide with it, and of a thread drawing itself.</summary>
    private const double Slide = 0.42, ThreadDraws = 0.4;

    /// <summary>What a thing on the board is made of, kept so a read only restyles it.</summary>
    private sealed record NoteView(Control Root, Panel Box, TextureRect Picture, Label Name, Line2D Mark);

    /// <summary>An entry on its way to its place.</summary>
    private sealed record Flight(string Id, Panel Note);

    private readonly Control _view;
    private readonly StripParts _strip;
    private readonly Action _readSoon;
    private readonly Func<uint> _worldId;
    private readonly BoardKeep _keep;
    private readonly TechCamera _camera = new();
    // What scales is under _map, what is set at its seen size under _front.
    private readonly Control _map = new() { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
    private readonly Control _front = new() { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
    private readonly Control _back = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly Dictionary<string, NoteView> _notes = new(StringComparer.Ordinal);
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
    // Over the board: the unread beside it, then what flies from them to it.
    private readonly Panel _side = new() { MouseFilter = Control.MouseFilterEnum.Stop, Visible = false };
    private readonly Panel _card = new() { MouseFilter = Control.MouseFilterEnum.Stop, ClipContents = true };
    private readonly VBoxContainer _cardWords = Style.Gap(new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 8);
    private readonly Control _stack = new() { MouseFilter = Control.MouseFilterEnum.Stop };
    private readonly Label _count = Text("", Style.Slate100, 40);
    private readonly Button _takeAll = new() { FocusMode = Control.FocusModeEnum.None, Text = T("research.pile.takeAll") };
    private readonly Control _flyers = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly List<Flight> _flights = [];
    private readonly List<Tween> _tweens = [];
    /// <summary>What has landed and goes at the next read, once its note stands there.</summary>
    private readonly List<Panel> _down = [];
    /// <summary>How far the threads of a note just landed are drawn.</summary>
    private readonly Dictionary<string, float> _growing = new(StringComparer.Ordinal);
    private readonly Label _tag = Text("", New, NameSize + 1);
    private string? _tagged;
    private Tween? _glide;
    // The press that flipped may still be held as the side goes, and its motion must not take the board.
    private bool _landedHere, _flipHeld;

    private bool Words => _camera.Zoom >= WordsFrom;

    /// <summary>What of the view the board has: all of it, unless unread stand beside it.</summary>
    private float Wide => _view.Size.X * (Reading ? 1 - SideShare : 1);

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
        view.AddChild(_map);
        view.AddChild(_front);
        _tag.Visible = false;
        var tagLook = new StyleBoxFlat { BgColor = Blank, BorderColor = New, ContentMarginLeft = 6, ContentMarginRight = 6, ContentMarginTop = 2, ContentMarginBottom = 2 };
        tagLook.SetBorderWidthAll(1);
        tagLook.SetCornerRadiusAll(3);
        _tag.AddThemeStyleboxOverride("normal", tagLook);
        _front.AddChild(_tag);
        BuildSide();
        view.AddChild(_side);
        view.AddChild(_flyers);
    }

    /// <summary>The unread beside the board, built once: what is shown changes under the pointer, what takes its press never does.</summary>
    private void BuildSide()
    {
        Paint(_side, Blank, Line, 1);
        Paint(_card, Paper, Ink, 2);
        _cardWords.Position = new Vector2(CardPad, CardPad);
        _card.AddChild(_cardWords);
        _card.GuiInput += e => FlipOn(e, _card);
        _stack.GuiInput += e => FlipOn(e, _stack);
        _stack.Draw += DrawStack;
        _count.HorizontalAlignment = HorizontalAlignment.Center;
        _count.Position = new Vector2(0, 30);
        _count.Size = new Vector2(SheetWide, 50);
        _stack.AddChild(_count);
        var unread = Text(T("research.pile.unread"), Style.Slate500);
        unread.HorizontalAlignment = HorizontalAlignment.Center;
        unread.Position = new Vector2(0, 84);
        unread.Size = new Vector2(SheetWide, 20);
        _stack.AddChild(unread);
        foreach (var (state, fill) in (ReadOnlySpan<(string, Color)>)[("normal", Paper), ("hover", Thread with { A = 0.4f }), ("pressed", Blank)])
        {
            var look = new StyleBoxFlat { BgColor = fill, BorderColor = Ink };
            look.SetBorderWidthAll(2);
            look.SetCornerRadiusAll(3);
            _takeAll.AddThemeStyleboxOverride(state, look);
        }
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
        _card.Position = new Vector2(SideGap, SideGap);
        _card.Size = new Vector2(size.X - StackWide - 3 * SideGap, size.Y - 2 * SideGap);
        _cardWords.Size = new Vector2(_card.Size.X - 2 * CardPad, 0);
        float top = (size.Y - StackTall - SideGap - TakeTall) / 2;
        _stack.Position = new Vector2(size.X - StackWide - SideGap, top);
        _stack.Size = new Vector2(StackWide, StackTall);
        _takeAll.Position = new Vector2(_stack.Position.X, top + StackTall + SideGap);
        _takeAll.Size = new Vector2(StackWide, TakeTall);
    }

    /// <summary>The outlines of the unread, one behind another up and to the right.</summary>
    private void DrawStack()
    {
        var sheet = new Vector2(SheetWide, StackTall - SheetBy * (Sheets - 1));
        for (int i = Math.Min(Sheets, _keep.Seen.Unread.Count) - 1; i >= 0; i--)
        {
            var box = new Rect2(new Vector2(i * SheetBy, (Sheets - 1 - i) * SheetBy), sheet);
            _stack.DrawRect(box, Paper);
            _stack.DrawRect(box, i == 0 ? Ink : Pencil, false, 1.5f);
        }
    }

    /// <summary>A left press on the entry or the stack: lands the entry.</summary>
    private void FlipOn(InputEvent e, Control over)
    {
        if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
        over.AcceptEvent();
        if (_pile.Flip(_keep.Seen, _order, BoardThings.All) is not { } id) return;
        _landedHere = _flipHeld = true;
        var rect = _card.GetGlobalRect();
        Launch(id, rect.Position - _view.GlobalPosition + new Vector2(rect.Size.X / 2, CardPad + CardPicture / 2f));
        GlideTo(SpotOf(id));
        ReadSoon();
    }

    /// <summary>The button: lands all that wait, at once, with no flight.</summary>
    private void TakeAll()
    {
        if (!_pile.TakeAll(_keep.Seen)) return;
        _landedHere = true;
        Stop();
        ReadSoon();
    }

    private void ReadSoon()
    {
        _key = null;
        _readSoon();
    }

    /// <summary>Starts <paramref name="id"/> from <paramref name="from"/> in the view, its box round a picture as large as the entry's, down to a note's on the board.</summary>
    private void Launch(string id, Vector2 from)
    {
        var note = new Panel { MouseFilter = Control.MouseFilterEnum.Ignore };
        Paint(note, Paper, Ink, 2);
        var picture = _strip.Picture(id, Icon);
        picture.CustomMinimumSize = Vector2.Zero;
        note.AddChild(picture);
        _flyers.AddChild(note);
        _flights.Add(new Flight(id, note));
        Fly(id, note, from, 0);
        var tween = NewTween();
        tween.TweenMethod(Callable.From<float>(t => Fly(id, note, from, t)), 0f, 1f, Slide).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenCallback(Callable.From(() => Landed(id, note)));
    }

    /// <summary>A flight <paramref name="t"/> of the way, to where the camera has its spot now.</summary>
    private void Fly(string id, Panel note, Vector2 from, float t)
    {
        const float PictureOfBox = (float)Icon / NoteSide;
        float zoom = (float)_camera.Zoom, side = Mathf.Lerp(CardPicture / PictureOfBox, NoteSide * zoom, t);
        var at = from.Lerp(new Vector2((float)_camera.X, (float)_camera.Y) + At(id) * zoom, t);
        note.Position = at - new Vector2(side, side) / 2;
        note.Size = new Vector2(side, side);
        var picture = note.GetChild<TextureRect>(0);
        picture.Position = new Vector2(side, side) * (1 - PictureOfBox) / 2;
        picture.Size = new Vector2(side, side) * PictureOfBox;
    }

    /// <summary>A flight's end: the note inks at its place, and its threads draw themselves.</summary>
    private void Landed(string id, Panel note)
    {
        _down.Add(note);
        _flights.RemoveAll(f => f.Id == id);
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

    /// <summary>Takes the middle of the board's part of the view to <paramref name="to"/>, in the board's own terms, for the box may grow under it.</summary>
    private void GlideTo(BoardSpot to)
    {
        _glide?.Kill();
        var (x, y) = _camera.ToMap(Wide / 2, _view.Size.Y / 2);
        var (fromX, fromY) = (x + _box.Left, y + _box.Top);
        _glide = _view.CreateTween();
        _glide.TweenMethod(Callable.From<float>(t => _camera.Centre(fromX + (to.X - fromX) * t - _box.Left, fromY + (to.Y - fromY) * t - _box.Top)), 0f, 1f, Slide).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
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
        foreach (var flight in _flights) flight.Note.QueueFree();
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
    }

    /// <summary>The tablet put away: the words by the closest shape go.</summary>
    public void Away() => Tag(null);

    /// <summary>Writes what <paramref name="id"/> lacks beside it; null for nothing.</summary>
    private void Tag(string? id, string words = "")
    {
        _tagged = id;
        _tag.Visible = id != null;
        _tag.Text = words;
        _tag.MoveToFront();
        _placedAt = null;
    }

    /// <summary>The pile just emptied and the last flight down: the board goes to the closest shape and says what it lacks. Nothing with no shape or no lack.</summary>
    private void Settle()
    {
        _landedHere = false;
        var shapes = _order.Select(s => s.Id).Where(id => _picture.Shows.GetValueOrDefault(id) == BoardShow.Silhouette);
        if (BoardPile.Closest(shapes, _read!, BoardThings.All) is not { } closest) return;
        string lack = LackOf(BoardThings.All.First(t => t.Id == closest));
        if (lack.Length == 0) return;
        Tag(closest, T("research.pile.lacks", ("what", _strip.NameOf(closest)), ("lack", lack)));
        Restyle();
        GlideTo(SpotOf(closest));
    }

    /// <summary>What <paramref name="thing"/> lacks in words: what was never had is not named.</summary>
    private string LackOf(BoardThing thing)
    {
        var lack = BoardPile.Lack(thing, _read!);
        var words = lack.Parts.Select(p => _had.Contains(p.Item) ? T("research.taken", ("n", p.More), ("what", _strip.NameOf(p.Item))) : T("research.pile.unknown")).Distinct().ToList();
        if (lack.Station is { } station) words.Add(_had.Contains(station) ? _strip.NameOf(station) : T("research.pile.noStation"));
        return string.Join(", ", words);
    }

    /// <summary><paramref name="read"/> without what is unread or in the air, for it is not on the board yet.</summary>
    private BoardRead Drawn(BoardRead read)
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
        else _picked = null;
        Reset();
        _key = null;
    }

    /// <summary>A showing over: what flew ends, and the pile is worked out afresh.</summary>
    private void Reset()
    {
        Stop();
        _pile.Reset();
        _side.Visible = _landedHere = _flipHeld = false;
    }

    public void Step(double delta)
    {
        if (_notes.Count == 0 && !Reading) return;
        if (_view.Size != _viewWas) Grow();
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
            view.Name.Visible = words && _picture.Shows.GetValueOrDefault(id) == BoardShow.Note;
        }
        if (_tagged is { } tagged) _tag.Position = (origin + At(tagged) * zoom + new Vector2(NoteSide * zoom / 2 + 8, -NameSize)).Round();
    }

    /// <summary>Sets every note at the size <paramref name="zoom"/> shows it; words are never scaled, so they stay sharp.</summary>
    private void SetSizes(float zoom)
    {
        _sizedAt = _camera.Zoom;
        float side = NoteSide * zoom, wide = (float)BoardSlots.NoteSize * zoom, icon = Words ? Icon * zoom : side * 0.75f;
        int size = Math.Max(8, Mathf.RoundToInt(NameSize * zoom));
        foreach (var view in _notes.Values)
        {
            view.Box.Position = new Vector2(-side / 2, -side / 2);
            view.Box.Size = new Vector2(side, side);
            view.Picture.Position = new Vector2(-icon / 2, -icon / 2);
            view.Picture.Size = new Vector2(icon, icon);
            view.Picture.TextureFilter = zoom < 1 ? CanvasItem.TextureFilterEnum.Linear : CanvasItem.TextureFilterEnum.Nearest;
            view.Name.LabelSettings.FontSize = size;
            view.Name.Position = new Vector2(-wide / 2, side / 2 + zoom);
            view.Name.Size = new Vector2(wide, size + 2);
            view.Mark.Scale = new Vector2(zoom, zoom);
            view.Mark.Width = LoopWidth / zoom;
        }
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
        if (starting) _picked = null;
        var drawn = Drawn(read);
        _picture = BoardView.Of(slots, BoardThings.All, drawn, _picked);
        _had = [.. drawn.Found, .. drawn.Granted];
        if (_pile.Top is { } top) _had.Add(top);
        if (_tagged != null && _picture.Shows.GetValueOrDefault(_tagged) != BoardShow.Silhouette) Tag(null);
        Restyle();
        Grow();
        if (starting)
        {
            _centred = true;
            _camera.Centre(SpotOf(_pile.Top!).X - _box.Left, SpotOf(_pile.Top!).Y - _box.Top);
        }
        Place();
        ClearDown();
        _back.QueueRedraw();
        Fill();
        if (_landedHere && !Reading && _flights.Count == 0) Settle();
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
        Tag(null);
    }

    private void Restyle()
    {
        foreach (var (id, view) in _notes) view.Root.Visible = _picture.Shows.GetValueOrDefault(id) != BoardShow.Unseen;
        foreach (var (id, show) in _picture.Shows)
        {
            if (show == BoardShow.Unseen) continue;
            if (!_notes.TryGetValue(id, out var view)) view = _notes[id] = NoteOf(id);
            Color? tie = Tie(id);
            bool note = show == BoardShow.Note;
            Paint(view.Box, note ? Paper : Blank, tie ?? (id == _tagged ? New : note ? Ink : Pencil), tie != null ? 3 : note || id == _tagged ? 2 : 1);
            view.Mark.Visible = note && _keep.Seen.Marked(id);
            if (note) view.Picture.Material = null;
            else Pale(view.Picture, BoardPencil.Of(BoardThings.All.First(t => t.Id == id), _read!.Count, _read.Stands));
            view.Root.Modulate = _picked == null || tie != null ? Colors.White : new Color(1, 1, 1, 0.3f);
        }
    }

    /// <summary>The box grown to what is shown and the view, the camera dragged by what it grew left and up, so nothing jumps.</summary>
    private void Grow()
    {
        _viewWas = _view.Size;
        var box = (_box == default ? BoardBox.Round(_view.Size.X, _view.Size.Y) : _box).Holding(BoardBox.Round(_view.Size.X, _view.Size.Y));
        foreach (var (id, show) in _picture.Shows)
            if (show != BoardShow.Unseen) box = box.Holding(SpotOf(id).X, SpotOf(id).Y);
        // What is on its way to a spot must be able to get there in view.
        foreach (string id in _flights.Select(f => f.Id).Append(_pile.Top).OfType<string>()) box = box.Holding(SpotOf(id).X, SpotOf(id).Y);
        double left = _box.Left - box.Left, up = _box.Top - box.Top;
        _box = box;
        // A control is culled by its size: with none, all its threads go as its corner leaves the view.
        _back.Size = new Vector2((float)box.Wide, (float)box.Tall);
        LaySide();
        _camera.Fit(Wide, _view.Size.Y, box.Wide, box.Tall);
        if (_centred) _camera.Drag(-left * _camera.Zoom, -up * _camera.Zoom);
        _placedAt = null;
    }

    private NoteView NoteOf(string id)
    {
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        var box = new Panel { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(box);
        var picture = _strip.Picture(id, Icon);
        picture.CustomMinimumSize = Vector2.Zero;
        root.AddChild(picture);
        var name = Text(_strip.NameOf(id), Style.Slate100, NameSize);
        name.HorizontalAlignment = HorizontalAlignment.Center;
        name.ClipText = true;
        name.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        root.AddChild(name);
        var mark = new Line2D
        {
            Points = Loop,
            Width = LoopWidth,
            DefaultColor = New,
            JointMode = Line2D.LineJointMode.Round,
            BeginCapMode = Line2D.LineCapMode.Round,
            EndCapMode = Line2D.LineCapMode.Round,
            Visible = false,
        };
        root.AddChild(mark);
        _front.AddChild(root);
        // Tips go by a control's rectangle alone, and a note may stand behind the side.
        _strip.Tips.Add(box, () => _side.Visible && _side.GetGlobalRect().HasPoint(_side.GetGlobalMousePosition()) ? null : TipOf(id));
        _sizedAt = null;
        return new NoteView(root, box, picture, name, mark);
    }

    private static Vector2[] LoopPoints()
    {
        const int Steps = 40;
        var points = new Vector2[Steps + 1];
        for (int i = 0; i <= Steps; i++)
        {
            float t = (float)i / Steps, a = -2.2f + t * (Mathf.Tau + 0.55f);
            float k = 1 + 0.035f * MathF.Sin(3 * a + 1) + 0.02f * MathF.Sin(7 * a + 0.5f) + 0.05f * t;
            float Rounded(float v) => MathF.Sign(v) * MathF.Pow(MathF.Abs(v), 2f / 3);
            points[i] = new Vector2(45 * k * Rounded(MathF.Cos(a)), 8 + 52 * k * Rounded(MathF.Sin(a)));
        }
        return points;
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
        if (_picked != null) Tag(null);
        _readSoon();
    }

    /// <summary>What is shown at a point of the view, the topmost; none if nothing is.</summary>
    private string? ThingAt(Vector2 at)
    {
        var (x, y) = _camera.ToMap(at.X, at.Y);
        var point = new Vector2((float)x, (float)y);
        var half = new Vector2(NoteSide / 2f, NoteSide / 2f);
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

    /// <summary>The entry shown large: its picture, name and what it is, where it leads, and what is known of it.</summary>
    private void FillCard()
    {
        Clear(_cardWords);
        _stack.QueueRedraw();
        if (_pile.Top is not { } id) return;
        _count.Text = _keep.Seen.Unread.Count.ToString();
        var picture = _strip.Picture(id, CardPicture);
        picture.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        _strip.Tips.Add(picture, () => TipOf(id));
        _cardWords.AddChild(picture);
        var name = Text(_strip.NameOf(id), Style.Slate100, CardName);
        name.HorizontalAlignment = HorizontalAlignment.Center;
        name.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _cardWords.AddChild(name);
        var kind = Text(KindOf(id), Style.Slate500, Tablet.HintSize + 1);
        kind.HorizontalAlignment = HorizontalAlignment.Center;
        kind.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _cardWords.AddChild(kind);
        double Pencil(BoardThing thing) => BoardPencil.Of(thing, _read!.Count, _read.Stands);
        bool Shape(string other) => other != id && _picture.Shows.ContainsKey(other) && _picture.Shows[other] != BoardShow.Note;
        if (BoardThreads.Of(id, BoardThings.All, Shape, Pencil) is [var next, ..])
        {
            string lack = LackOf(BoardThings.All.First(t => t.Id == next));
            var words = Text(lack.Length == 0 ? _strip.NameOf(next) : T("research.pile.lacks", ("what", _strip.NameOf(next)), ("lack", lack)), Style.Slate100);
            words.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _cardWords.AddChild(Fact(T("research.pile.next"), New, words, 1));
        }
        FactsOf(id, _cardWords);
    }

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
            facts.AddChild(Fact(T("research.takes"), From, takes, 3));
        }
        if (thing != null)
        {
            var at = Flow(10);
            foreach (string station in link.MadeAt) at.AddChild(StationOf(station));
            if (link.MadeAt.Count == 0) at.AddChild(Text(T(thing.Stations == null ? "research.builtInPlace" : "research.atLifepod"), Style.Slate500));
            facts.AddChild(Fact(T("research.madeAt"), MadeAt, at, 2));
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
        return _strip.Named(item, T("research.board.part", ("what", name), ("have", have), ("enough", enough)), have >= enough ? Bought : Style.Slate100);
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
        if (picked == id) return Picked;
        var link = TechLinks.Of(picked);
        return link.MadeAt.Contains(id) ? MadeAt : link.From.Contains(id) || link.Takes.Any(t => t.Item == id) ? From : link.UsedIn.Contains(id) ? UsedIn : null;
    }

    /// <summary>Where <paramref name="id"/> stands on the board: where it was moved to, else its slot.</summary>
    private BoardSpot SpotOf(string id) => _keep.Spots.TryGetValue(id, out var spot) ? spot : new BoardSpot(_slots[id].X, _slots[id].Y);

    /// <summary>Where <paramref name="id"/> stands on the map, whose top-left is the box's.</summary>
    private Vector2 At(string id) => new((float)(SpotOf(id).X - _box.Left), (float)(SpotOf(id).Y - _box.Top));

    /// <summary>One canvas under the notes: the threads, and the lines from what is picked to what it ties to.</summary>
    private void DrawBack()
    {
        foreach (var (note, shape) in _picture.Threads)
        {
            var from = At(note);
            var to = _growing.TryGetValue(note, out float drawn) ? from.Lerp(At(shape), drawn) : At(shape);
            _back.DrawLine(from, to, Thread with { A = _picked == null ? 0.5f : 0.15f }, 2, true);
        }
        if (_picked == null || !_slots.ContainsKey(_picked)) return;
        foreach (var (id, show) in _picture.Shows)
            if (show != BoardShow.Unseen && id != _picked && Tie(id) is { } colour) _back.DrawLine(At(_picked), At(id), colour with { A = 0.7f }, 2, true);
    }

    private static void Paint(Panel box, Color fill, Color edge, int border)
    {
        var look = new StyleBoxFlat { BgColor = fill, BorderColor = edge, AntiAliasing = true };
        look.SetBorderWidthAll(border);
        look.SetCornerRadiusAll(3);
        box.AddThemeStyleboxOverride("panel", look);
    }
}
