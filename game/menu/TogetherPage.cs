using System;
using Godot;
using Stillwake.Game.Render;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Menu;

/// <summary>
/// The title's Play together panel (the approved mockup, coop-flow, 0031): Host and Join side by side. Host picks the
/// world to open as Worlds does, an existing one or a new one, and plays it, its code shown as it opens
/// (<see cref="KeyCard"/>); Join is one field for a friend's code. The playtest server's password is asked once, on its
/// own, the first time it is needed or after the server did not take it, and kept on this machine.
/// </summary>
public partial class TogetherPage : VBoxContainer
{
    /// <summary>Where the password is kept, on this machine only.</summary>
    private const string File = "user://coop.cfg";

    private enum View { Choose, Pick, Join, Password }

    private readonly ITogether _together;
    private readonly Action _back;
    private readonly ConfigFile _kept = new();
    private readonly VBoxContainer _choose, _joining, _asking;
    private readonly WorldsPanel _worlds;
    private readonly Label _joinStatus, _askStatus;
    private readonly LineEdit _codeField, _passwordField;
    private View _view = View.Choose;
    private Action<string>? _pending;
    private string _password = "";
    private TogetherState? _handled;
    private readonly Label _chooseStatus;

    /// <param name="worlds">A Worlds panel playing what it plays (an id) and makes (a name) through the two actions given.</param>
    public TogetherPage(ITogether together, Func<Action<string>, Action<string>, WorldsPanel> worlds, Action back)
    {
        (_together, _back) = (together, back);
        _kept.Load(File);
        Style.Gap(this, 8);

        _choose = Style.Gap(new VBoxContainer(), 8);
        var pair = Style.Gap(new HBoxContainer { Alignment = AlignmentMode.Center }, 8);
        pair.AddChild(Big("menu.hostTitle", "menu.hostWhat", Host));
        pair.AddChild(Big("menu.joinTitle", "menu.joinWhat", () => Show(View.Join)));
        _choose.AddChild(pair);
        _choose.AddChild(_chooseStatus = Line("", Style.Red300, 12));
        AddChild(_choose);

        _worlds = worlds(id => _together.HostTogether(_password, id, ""), name => _together.HostTogether(_password, null, name));
        AddChild(_worlds);

        _joining = Style.Gap(new VBoxContainer(), 6);
        _joining.AddChild(Line(T("menu.friendsCode"), Style.Slate400, 11));
        _joining.AddChild(_codeField = Field("", secret: false, size: 26));
        _codeField.Alignment = HorizontalAlignment.Center;
        _codeField.MaxLength = Codes.Length;
        _codeField.PlaceholderText = "····";
        _codeField.TextChanged += typed =>
        {
            string code = Codes.Typed(typed);
            if (code == typed) return;
            _codeField.Text = code;
            _codeField.CaretColumn = code.Length;
        };
        _codeField.TextSubmitted += _ => JoinTyped();
        _joining.AddChild(Style.Button(T("menu.joinTitle"), JoinTyped, size: 14, padX: 16, padY: 6));
        _joining.AddChild(_joinStatus = Line("", Style.Sky200, 12));
        AddChild(_joining);

        _asking = Style.Gap(new VBoxContainer(), 6);
        _asking.AddChild(Line(T("menu.passTitle"), Style.Slate100, 14));
        var why = Line(T("menu.passWhy"), Style.Slate400, 11);
        why.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        why.CustomMinimumSize = new Vector2(280, 0);
        _asking.AddChild(why);
        _asking.AddChild(_passwordField = Field((string)_kept.GetValue("coop", "password", ""), secret: true, size: 13));
        _passwordField.TextSubmitted += _ => Connect();
        _asking.AddChild(_askStatus = Line("", Style.Red300, 12));
        var answer = Style.Gap(new HBoxContainer { Alignment = AlignmentMode.Center }, 8);
        answer.AddChild(Style.Button(T("menu.connect"), Connect, size: 14, padX: 16, padY: 6));
        answer.AddChild(Style.Button(T("menu.cancel"), () => Show(View.Choose), size: 14, padX: 16, padY: 6));
        _asking.AddChild(answer);
        AddChild(_asking);

        AddChild(Style.Button(T("menu.back"), Back, size: 14, padX: 16, padY: 6));
        Show(View.Choose);
    }

    /// <summary>Forgets the password kept: the server did not take it as a world opened, so the next Host or Join asks again.</summary>
    public static void ForgetPassword()
    {
        var kept = new ConfigFile();
        kept.Load(File);
        kept.SetValue("coop", "password", "");
        kept.Save(File);
    }

    /// <summary>Back a step: from picking, joining or the password to the choice, else out of the page.</summary>
    private void Back()
    {
        if (_view != View.Choose && _together.Together.Mode == TogetherMode.Alone) Show(View.Choose);
        else _back();
    }

    /// <summary>Shown afresh each time the page opens.</summary>
    public void Opened() => Show(View.Choose);

    /// <summary>Host: the worlds to open, once the password is known.</summary>
    public void Host() => WithPassword(Pick);

    /// <summary>The worlds to host, under the password given.</summary>
    private void Pick(string password)
    {
        _password = password;
        _worlds.Refresh();
        Show(View.Pick);
    }

    private void Show(View view)
    {
        _view = view;
        if (view == View.Password)
        {
            _passwordField.GrabFocus();
            _passwordField.SelectAll();
        }
        if (view == View.Join) _codeField.GrabFocus();
    }

    /// <summary>Runs <paramref name="act"/> with the password kept, asking for it first when there is none.</summary>
    private void WithPassword(Action<string> act)
    {
        _kept.Load(File);
        string kept = (string)_kept.GetValue("coop", "password", "");
        if (kept != "")
        {
            act(kept);
            return;
        }
        _pending = act;
        _askStatus.Text = T("menu.passNeeded");
        Show(View.Password);
    }

    private void Connect()
    {
        if (_passwordField.Text == "") return;
        _kept.SetValue("coop", "password", _passwordField.Text);
        _kept.Save(File);
        var act = _pending;
        _pending = null;
        Show(View.Choose);
        act?.Invoke(_passwordField.Text);
    }

    private void JoinTyped()
    {
        string code = Codes.Typed(_codeField.Text);
        if (code.Length < Codes.Length)
        {
            _joinStatus.Text = T("menu.codeShort");
            return;
        }
        WithPassword(password => _together.JoinTogether(code, password));
        if (_view == View.Choose) Show(View.Join);
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree()) return;
        var state = _together.Together;
        // The server did not take the password: ask again, to join; a host has left the title by then.
        if (state.PasswordRefused && !ReferenceEquals(state, _handled))
        {
            _handled = state;
            _pending = password => _together.JoinTogether(Codes.Typed(_codeField.Text), password);
            _askStatus.Text = T("menu.passRefused");
            _kept.SetValue("coop", "password", "");
            Show(View.Password);
        }
        _asking.Visible = _view == View.Password;
        _worlds.Visible = _view == View.Pick;
        _joining.Visible = !_asking.Visible && (_view == View.Join || state.Mode is TogetherMode.Joining or TogetherMode.Joined);
        _choose.Visible = !_asking.Visible && !_joining.Visible && !_worlds.Visible;
        _chooseStatus.Text = state.Mode == TogetherMode.Alone && !state.PasswordRefused ? state.Problem ?? "" : "";
        if (_joining.Visible)
        {
            _joinStatus.Text = state.Mode switch
            {
                TogetherMode.Joining => T("menu.joiningCode", ("code", state.Code ?? "")),
                TogetherMode.Joined => T("menu.joinedCode", ("code", state.Code ?? "")),
                _ => state.Problem ?? (_joinStatus.Text == T("menu.codeShort") ? _joinStatus.Text : ""),
            };
        }
    }

    private static Button Big(string title, string what, Action pressed)
    {
        var button = Style.Button($"{T(title)}\n{T(what)}", pressed, size: 13, padX: 14, padY: 12);
        button.CustomMinimumSize = new Vector2(150, 0);
        return button;
    }

    private static Label Line(string text, Color colour, int size) => new()
    {
        Text = text,
        LabelSettings = Style.Text(Style.Mono, size, colour),
        HorizontalAlignment = HorizontalAlignment.Center,
        MouseFilter = MouseFilterEnum.Ignore,
    };

    private static LineEdit Field(string text, bool secret, int size)
    {
        var field = new LineEdit { Text = text, Secret = secret, CustomMinimumSize = new Vector2(240, 0) };
        field.AddThemeStyleboxOverride("normal", Style.Box(Style.Slate800, Style.Slate600, 6));
        field.AddThemeStyleboxOverride("focus", Style.Box(Style.Slate800, Style.Sky400, 6));
        field.AddThemeFontOverride("font", Style.Mono);
        field.AddThemeFontSizeOverride("font_size", size);
        field.AddThemeColorOverride("font_color", Style.Slate100);
        return field;
    }
}
