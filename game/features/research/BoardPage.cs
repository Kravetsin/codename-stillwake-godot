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
/// the strip below; it buys nothing, for it takes no way to send a command. A thing dragged stays where it is let go, on this machine only (<see cref="BoardKeep"/>). What is new is ringed until the tablet is put away.
/// </summary>
public sealed class BoardPage : ITabletPage
{
    private static readonly Color Paper = new("18202b"), Ink = new("8a93a6"), Blank = new("0f141b"), Pencil = new("39404c"), Thread = new("7a8090"), New = new("f2b84b");

    /// <summary>Under this zoom words would be too small to read, and only pictures show.</summary>
    private const double WordsFrom = 0.5;
    /// <summary>A note's sizes on the board at zoom 1, and its word's.</summary>
    private const int NoteSide = 64, Icon = 40, NameSize = 12;

    /// <summary>What a thing on the board is made of, kept so a read only restyles it.</summary>
    private sealed record NoteView(Control Root, Panel Box, TextureRect Picture, Label Name);

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

    private bool Words => _camera.Zoom >= WordsFrom;

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
    }

    /// <summary>What the board reads of <paramref name="m"/>.</summary>
    public static BoardRead ReadOf(Registry m) =>
        new(m.Knowledge.Obtained.Select(t => t.Item).ToList(), m.Knowledge.GrantedIds, m.Knowledge.ObtainedOf, kind => m.Grid.MachinesOf(kind).Count > 0);

    public Vector2 ScreenOf(string id) => _view.GlobalPosition + _notes[id].Root.Position;

    public void Show(bool shown)
    {
        _out = _map.Visible = _front.Visible = shown;
        if (shown) _strip.Action.Visible = false;
        else _picked = null;
        _key = null;
    }

    public void Step(double delta)
    {
        if (_notes.Count == 0) return;
        if (_view.Size != _viewWas) Grow();
        _camera.Fit(_view.Size.X, _view.Size.Y, _box.Wide, _box.Tall);
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
        var slots = BoardSlots.Of(read.Found, BoardThings.All, read.Granted);
        _picture = BoardView.Of(slots, BoardThings.All, read, _picked);
        _slots = slots.ToDictionary(s => s.Id, StringComparer.Ordinal);
        _had = [.. read.Found, .. read.Granted];
        _keep.Seen.Read(BoardView.NotesOf(slots, read));
        Restyle();
        Grow();
        _placedAt = null;
        _back.QueueRedraw();
        Fill();
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
            bool marked = _keep.Seen.Marked(id);
            Paint(view.Box, note ? Paper : Blank, tie ?? (marked ? New : note ? Ink : Pencil), tie != null || marked ? 3 : note ? 2 : 1);
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
        double left = _box.Left - box.Left, up = _box.Top - box.Top;
        _box = box;
        _camera.Fit(_view.Size.X, _view.Size.Y, box.Wide, box.Tall);
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
        _front.AddChild(root);
        _strip.Tips.Add(box, () => TipOf(id));
        _sizedAt = null;
        return new NoteView(root, box, picture, name);
    }

    /// <summary>The board's pointer: any button drags it, the left one on a thing drags that instead, and picks it if it was not dragged.</summary>
    public void Pointer(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                if (button.Pressed) Press(button.Position);
                else if (_pressed && _moved < ClickSlack) Click(button.Position);
                if (!button.Pressed) LetGo();
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown }:
                Zoom(e, _view);
                break;
            case InputEventMouseMotion { ButtonMask: not 0 } drag:
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
        _camera.Wheel(wheel.ButtonIndex == MouseButton.WheelUp ? 1 : -1, at.X, at.Y);
        over.AcceptEvent();
    }

    /// <summary>A click at a point of the view: picks what is shown there; off everything, picks nothing.</summary>
    private void Click(Vector2 at)
    {
        _picked = ThingAt(at);
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
        var facts = _strip.Facts;
        if (_read is not { } read || read.Found.Count == 0) return;
        if (_picked is not { } id)
        {
            var hint = Text(T("research.board.hint"), Style.Slate500);
            hint.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            facts.AddChild(hint);
            return;
        }
        bool note = _had.Contains(id);
        var thing = BoardThings.All.FirstOrDefault(t => t.Id == id);
        double? pencil = note || thing == null ? null : BoardPencil.Of(thing, read.Count, read.Stands);
        facts.AddChild(_strip.Title(_strip.NameOf(id), Table.ContainsKey($"item.{id}.kind") ? T($"item.{id}.kind") : "", id, TipOf, pencil));
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
        foreach (var (note, shape) in _picture.Threads) _back.DrawLine(At(note), At(shape), Thread with { A = _picked == null ? 0.5f : 0.15f }, 2, true);
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
