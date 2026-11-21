using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The Appearance tab (hero-skins), shown over the equipment's doll in its gear's place (gear-tabs): a slot a kind where its gear's is
/// (<see cref="Appearance.Spots"/>), each showing the skin chosen, its amber leader line to what it dresses. A click opens the kind's
/// strip under the doll, "none" and every skin of it, those this player does not own locked; a pick closes it and goes to
/// <c>picked</c>, which keeps it and sends it. Built at the slot size and built again with it.
/// </summary>
public partial class AppearanceView : VBoxContainer
{
    private static readonly LabelSettings Caption = Style.Text(Style.Mono, 9, new Color("c99b5f"));

    private readonly Ui _ui;
    private readonly int _size;
    private readonly Func<IReadOnlyList<string>> _kept;
    private readonly Func<Skin, bool> _owned;
    private readonly Action<IReadOnlyList<string>> _picked;
    private readonly TipSpots _tips;
    private readonly Dictionary<string, SkinSlot> _slots = [];
    private readonly DollLeads _leads;
    private readonly PanelContainer _shelf;
    private readonly Label _caption;
    private readonly HFlowContainer _strip;
    private string? _open;

    /// <param name="kept">The skins this player keeps, one a kind, owned or not.</param>
    /// <param name="owned">Whether this player may pick a skin.</param>
    /// <param name="picked">The skins kept after a pick.</param>
    public AppearanceView(Ui ui, int size, Func<IReadOnlyList<string>> kept, Func<Skin, bool> owned, Action<IReadOnlyList<string>> picked)
    {
        (_ui, _size, _kept, _owned, _picked) = (ui, size, kept, owned, picked);
        _tips = new TipSpots(ui.Tip);
        MouseFilter = MouseFilterEnum.Ignore;
        Style.Gap(this, 0);
        var layout = DollLayout.Of(size);
        var doll = new Control { CustomMinimumSize = new Vector2(layout.Width, layout.Height), MouseFilter = MouseFilterEnum.Ignore };
        doll.AddChild(_leads = new DollLeads(layout, DollLayout.Looks, amber: true));
        foreach (var (kind, spot) in Appearance.Spots) doll.AddChild(Slot(kind, layout.SlotAt(spot)));
        AddChild(doll);

        // The strip under the doll, ruled off in amber and named by its kind (pick B).
        var rule = new StyleBoxFlat { BgColor = Colors.Transparent, BorderColor = SkinSlot.Amber with { A = 0.28f }, BorderWidthTop = 1 };
        (rule.ContentMarginTop, rule.ContentMarginLeft, rule.ContentMarginRight) = (7, 4, 4);
        _shelf = new PanelContainer { Visible = false, MouseFilter = MouseFilterEnum.Ignore };
        _shelf.AddThemeStyleboxOverride("panel", rule);
        var shelf = Style.Gap(new VBoxContainer(), 4);
        shelf.AddChild(_caption = new Label { LabelSettings = Caption, MouseFilter = MouseFilterEnum.Ignore });
        shelf.AddChild(_strip = new HFlowContainer());
        _strip.AddThemeConstantOverride("h_separation", Slots.Gap);
        _strip.AddThemeConstantOverride("v_separation", Slots.Gap);
        _shelf.AddChild(shelf);
        AddChild(_shelf);
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

    private SkinSlot Slot(string kind, (int X, int Y) at)
    {
        var slot = new SkinSlot(_size) { Position = new Vector2(at.X, at.Y) };
        slot.Pressed += () =>
        {
            _ui.Click();
            Open(_open == kind ? null : kind);
        };
        _tips.Add(slot, () => SlotTip(kind));
        _slots[kind] = slot;
        return slot;
    }

    /// <summary>Every slot as the skins kept now: a kept skin this player does not own shown locked, as its hero wears its gear's own.</summary>
    private void Refresh()
    {
        var kept = _kept();
        foreach (var (kind, spot) in Appearance.Spots)
        {
            var id = Skins.Chosen(kept, kind);
            _slots[kind].Show(id, id == null || Skins.Of(id) is not { } skin || _owned(skin), chosen: _open == kind);
            _leads.Fill(spot, id != null);
        }
    }

    // The strip of `kind` under the doll, or none.
    private void Open(string? kind)
    {
        _open = kind;
        foreach (var child in _strip.GetChildren()) child.QueueFree();
        _shelf.Visible = kind != null;
        Refresh();
        if (kind == null) return;
        _caption.Text = T($"skins.{kind}").ToUpperInvariant();
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
