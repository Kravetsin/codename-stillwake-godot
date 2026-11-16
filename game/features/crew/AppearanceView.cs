using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The Appearance tab (skins), shown on the equipment's screen in its place: a slot a kind in rows (<see cref="Appearance.Rows"/>),
/// each under its name, showing the skin chosen. A click opens the kind's strip under them, "none" and every skin of it, those
/// this player does not own locked; a pick closes it and goes to <c>picked</c>, which keeps it and sends it. Built at the
/// slot size and built again with it.
/// </summary>
public partial class AppearanceView : VBoxContainer
{
    private readonly Ui _ui;
    private readonly int _size;
    private readonly Func<IReadOnlyList<string>> _kept;
    private readonly Func<Skin, bool> _owned;
    private readonly Action<IReadOnlyList<string>> _picked;
    private readonly TipSpots _tips;
    private readonly Dictionary<string, SkinSlot> _slots = [];
    private readonly HFlowContainer _strip;
    private string? _open;

    /// <param name="kept">The skins this player keeps, one a kind, owned or not.</param>
    /// <param name="owned">Whether this player may pick a skin.</param>
    /// <param name="picked">The skins kept after a pick.</param>
    public AppearanceView(Ui ui, int size, Func<IReadOnlyList<string>> kept, Func<Skin, bool> owned, Action<IReadOnlyList<string>> picked)
    {
        (_ui, _size, _kept, _owned, _picked) = (ui, size, kept, owned, picked);
        _tips = new TipSpots(ui.Tip);
        Style.Gap(this, 2);
        foreach (var kinds in Appearance.Rows)
        {
            var row = Style.Gap(new HBoxContainer { Alignment = AlignmentMode.Center }, Slots.Gap);
            foreach (var kind in kinds) row.AddChild(Kind(kind));
            AddChild(row);
        }
        AddChild(_strip = new HFlowContainer { Visible = false, Alignment = FlowContainer.AlignmentMode.Center });
        _strip.AddThemeConstantOverride("h_separation", Slots.Gap);
        _strip.AddThemeConstantOverride("v_separation", Slots.Gap);
        VisibilityChanged += () => Open(null);
        Refresh();
    }

    public override void _Process(double delta) => _tips.Frame(_ui.MenuOpen.Get() || !IsVisibleInTree() ? null : _mouse);

    // The pointer as its moves came, as the items panel keeps it: a scripted move is one too.
    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouse mouse) _mouse = mouse.Position;
    }

    private Vector2 _mouse;

    private VBoxContainer Kind(string kind)
    {
        var column = Style.Gap(new VBoxContainer(), 2);
        column.AddChild(new Label
        {
            Text = T($"skins.{kind}"),
            LabelSettings = Style.Text(Style.Mono, 9, Style.Slate500),
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        });
        var slot = new SkinSlot(_size) { SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
        slot.Pressed += () =>
        {
            _ui.Click();
            Open(_open == kind ? null : kind);
        };
        _tips.Add(slot, () => SlotTip(kind));
        _slots[kind] = slot;
        column.AddChild(slot);
        return column;
    }

    /// <summary>Every slot as the skins kept now: a kept skin this player does not own shown locked, as its hero wears its gear's own.</summary>
    private void Refresh()
    {
        var kept = _kept();
        foreach (var (kind, slot) in _slots)
        {
            var id = Skins.Chosen(kept, kind);
            slot.Show(id, id == null || Skins.Of(id) is not { } skin || _owned(skin), chosen: _open == kind);
        }
    }

    // The strip of `kind` under the slots, or none.
    private void Open(string? kind)
    {
        _open = kind;
        foreach (var child in _strip.GetChildren()) child.QueueFree();
        _strip.Visible = kind != null;
        Refresh();
        if (kind == null) return;
        var chosen = Skins.Chosen(_kept(), kind);
        foreach (var choice in Appearance.Strip(kind, _owned))
        {
            var slot = new SkinSlot(_size);
            slot.Show(choice.Id, choice.Owned, choice.Id == chosen);
            slot.Pressed += () => Pick(kind, choice);
            _tips.Add(slot, () => ChoiceTip(kind, choice));
            _strip.AddChild(slot);
        }
    }

    private void Pick(string kind, Appearance.Choice choice)
    {
        if (!choice.Owned) return;
        _ui.Click();
        _picked(Appearance.Pick(_kept(), kind, choice.Id));
        Open(null);
    }

    private Tip SlotTip(string kind)
    {
        var id = Skins.Chosen(_kept(), kind);
        var lines = new List<TipLine> { new(T($"skins.{kind}"), Tone.Dim) };
        if (id != null && Skins.Of(id) is { } skin && !_owned(skin)) lines.Add(new(T("skins.notOwned"), Tone.Dim));
        if (kind == Skins.Effect && id != null) lines.AddRange(EffectLines());
        return new Tip(id == null ? T("skins.none") : T($"skin.{id}"), lines);
    }

    private static Tip ChoiceTip(string kind, Appearance.Choice choice)
    {
        var lines = new List<TipLine>();
        if (!choice.Owned) lines.Add(new(T("skins.locked"), Tone.Dim));
        if (kind == Skins.Effect && choice.Id != null) lines.AddRange(EffectLines());
        return new Tip(choice.Id == null ? T("skins.none") : T($"skin.{choice.Id}"), lines);
    }

    private static TipLine[] EffectLines() => [new(T("skins.effectWhere"), Tone.Dim), new(T("skins.arcWins"), Tone.Dim)];
}
