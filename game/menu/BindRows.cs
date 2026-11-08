using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.I18n;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Menu;

/// <summary>
/// Settings' Controls (settings-menu), the title's and the game menu's (2026-11-04-key-binds): a row per action, its key on a button. A click
/// waits for a key; Esc while waiting changes nothing; a key another action has is swapped, a reserved one refused, the line
/// under the rows saying so. Reset all puts the defaults back. Every change goes through <see cref="Ui.Binds"/>, applied at once.
/// </summary>
public partial class BindRows : VBoxContainer
{
    private readonly Ui _ui;
    private readonly Dictionary<Bind, Button> _keys = [];
    private readonly Label _status;
    private Bind? _waiting;
    private (Binds, Language)? _drawn;

    public BindRows(Ui ui)
    {
        _ui = ui;
        Style.Gap(this, 8);
        AddChild(Line(T("bind.hint"), Style.Slate500, 11));
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(520, 200), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var rows = Style.Gap(new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }, 4);
        foreach (var bind in Enum.GetValues<Bind>())
        {
            var row = Style.Gap(new HBoxContainer(), 8);
            var name = Line(T("bind." + Binds.IdOf(bind)), Style.Slate100, 13);
            (name.HorizontalAlignment, name.SizeFlagsHorizontal) = (HorizontalAlignment.Left, SizeFlags.ExpandFill);
            row.AddChild(name);
            var key = Style.Button("", () => Wait(bind), size: 13, padX: 10, padY: 4);
            key.CustomMinimumSize = new Vector2(110, 0);
            row.AddChild(_keys[bind] = key);
            rows.AddChild(row);
        }
        scroll.AddChild(rows);
        AddChild(scroll);
        AddChild(_status = Line("", Style.Sky200, 12));
        AddChild(Style.Button(T("bind.reset"), Reset, size: 14, padX: 16, padY: 6));
    }

    private void Wait(Bind bind)
    {
        _waiting = bind;
        _status.Text = "";
        _drawn = null;
    }

    private void Reset()
    {
        _waiting = null;
        _ui.Binds.Set(Binds.Default);
        _status.Text = T("bind.resetDone");
        _drawn = null;
    }

    /// <summary>While waiting, the next key is the new one, before the menu or the game hears it; Esc cancels, a click elsewhere too.</summary>
    public override void _Input(InputEvent e)
    {
        if (_waiting is not Bind bind) return;
        if (e is InputEventMouseButton { Pressed: true })
        {
            (_waiting, _drawn) = (null, null);
            return;
        }
        if (e is not InputEventKey { Pressed: true, Echo: false } key) return;
        GetViewport().SetInputAsHandled();
        (_waiting, _drawn) = (null, null);
        if (key.PhysicalKeycode == Key.Escape) return;
        string name = BindKeys.NameOf(key);
        if (_ui.Binds.Get().Rebind(bind, name, out var swapped, out var refused) is not { } next)
        {
            _status.Text = T(refused!, ("key", Binds.Shown(name)));
            return;
        }
        _ui.Binds.Set(next);
        _status.Text = swapped is Bind other ? T("bind.swapped", ("other", T("bind." + Binds.IdOf(other)))) : "";
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree())
        {
            (_waiting, _drawn) = (null, _waiting == null ? _drawn : null);
            return;
        }
        var now = (_ui.Binds.Get(), Speaking);
        if (_drawn == now) return;
        _drawn = now;
        foreach (var (bind, button) in _keys)
        {
            button.Text = bind == _waiting ? T("bind.press") : Binds.Shown(now.Item1.KeyOf(bind));
            Style.Paint(button, bind == _waiting ? Style.Sky700 : Style.Slate700, bind == _waiting ? Style.Sky700 : Style.Slate600, 10, 4);
        }
    }

    private static Label Line(string text, Color colour, int size) => new()
    {
        Text = text,
        LabelSettings = Style.Text(Style.Mono, size, colour),
        HorizontalAlignment = HorizontalAlignment.Center,
        MouseFilter = MouseFilterEnum.Ignore,
    };
}
