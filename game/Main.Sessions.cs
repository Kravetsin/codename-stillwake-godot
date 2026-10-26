// Main's sessions: hosting, joining and playing together, the worlds kept on disk, the title and the way back to it.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Debris;
using Stillwake.Game.Menu;
using Stillwake.Game.Net;
using Stillwake.Game.Platform;
using Stillwake.Game.Render;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game;

public partial class Main
{
    /// <summary>
    /// The relay to meet through rather than ENet, with <c>--relay</c> (the playtest server) or
    /// <c>--relay=wss://...</c>, and its password, <c>--password=...</c> or <c>STILLWAKE_PASSWORD</c>; none without.
    /// </summary>
    private (string Url, string Password)? Relay()
    {
        var relay = _dev.Given("--relay");
        if (relay == null) return null;
        string url = relay.Length > 0 ? relay : RelayFrames.PlaytestUrl;
        string password = _dev.Value("--password")
            ?? System.Environment.GetEnvironmentVariable("STILLWAKE_PASSWORD") ?? "";
        return (url, password);
    }

    /// <summary>Hosts a session from the start: over ENet on <see cref="Port"/>, or on the relay as <paramref name="name"/>.</summary>
    private void Host(string? name)
    {
        if (Has("--perf")) _layout = PerfLayout();
        var world = World.Create(1, _layout, Modules());
        // The debris and wreck scenarios are played hosting too, so a guest sees a catch, or the host chop.
        foreach (var scenario in _scenarios.Where(s => s.Hosted)) world = scenario.Seed(world, new Dev.DevSeed(Modules, _m));
        _roster = Roster.Empty();
        var (host, client) = LocalSession.Create(world, SessionBuild, _roster.SeatOf(_playerKey));
        _session = new Session(host, client);
        ShowWorld();
        AimCamera(_layout);
        Serve(name, Relay());
    }

    /// <summary>
    /// Opens this machine's own world to guests: over ENet on <see cref="Port"/>, or on
    /// <paramref name="relay"/> as <paramref name="name"/>. Its session's host welcomes them with its world.
    /// </summary>
    private void Serve(string? name, (string Url, string Password)? relay)
    {
        if (_session is not { Host: { } host } session) return;
        var client = session.Client;
        _role = "host";
        _guests.Clear();
        ILink<ToHost, ToClient> link;
        string where = $":{Port}";
        if (relay is var (url, password))
        {
            var relayed = RelayLink<ToHost, ToClient>.Host(url, password, name);
            where = "the relay";
            _together = new TogetherState(TogetherMode.Opening, name?.ToUpperInvariant(), [], null, false);
            relayed.Hosted += hosted =>
            {
                where = $"\"{hosted}\" on the relay";
                _net = $"hosting {where} · peers 0";
                _together = _together with { Mode = TogetherMode.Hosting, Code = hosted.ToUpperInvariant() };
                Log($"hosting session {hosted} on {url}");
            };
            relayed.Failed += why =>
            {
                Log(why);
                _net = why;
                // Nobody can join a session the relay never opened: the world plays on alone.
                bool opened = _together.Mode == TogetherMode.Hosting;
                _together = TogetherState.Alone with { Problem = why, PasswordRefused = !opened };
                _role = "alone";
                _poll = () => { };
            };
            link = relayed;
        }
        else
        {
            var (enet, error) = EnetLink<ToHost, ToClient>.Host(Port);
            if (error != Error.Ok) GD.PrintErr($"[host] cannot listen on {Port}: {error}");
            link = enet;
        }
        var ids = _guests;
        _net = $"hosting {where} · peers 0";
        var keys = new Dictionary<long, string>();
        link.Connected += (peer, channel) =>
        {
            // Seated only once it says who it is (0032): the same seat in this world every time.
            channel.OnMessage(message =>
            {
                if (message is not HelloMessage hello || ids.ContainsKey(peer)) return;
                if (hello.Player == _playerKey || keys.ContainsValue(hello.Player))
                {
                    Log($"peer refused: {hello.Player} is aboard already");
                    channel.Send(new RefusedMessage(T("menu.aboard")));
                    return;
                }
                int seat = _roster.SeatOf(hello.Player);
                SaveRoster();
                keys[peer] = hello.Player;
                ids[peer] = host.Admit(channel, seat);
                _net = $"hosting {where} · peers {ids.Count}";
                Log($"peer {seat} joined at tick {host.Tick}");
            });
        };
        link.Disconnected += peer =>
        {
            keys.Remove(peer);
            // Its hero leaves with it, its suit colour freed; its inventory stays in its seat.
            if (!ids.Remove(peer, out int id)) return;
            host.Disconnect(id, client.World.Encode(new Sim.Crew.Leave()));
            Log($"peer {id} left");
            _net = $"hosting {where} · peers {ids.Count}";
        };
        _poll = link.Poll;
        _stopLink = () =>
        {
            link.Close();
            // Its guests' heroes leave the world as they would leaving themselves.
            foreach (int id in ids.Values) host.Disconnect(id, client.World.Encode(new Sim.Crew.Leave()));
            ids.Clear();
        };
    }

    /// <summary>
    /// Joins a session: a host's address over ENet, or a session's name on <paramref name="relay"/>. A world
    /// of this machine's own plays on until the host's arrives, and on still if joining fails.
    /// </summary>
    private void Join(string address, BadNetwork? network, (string Url, string Password)? relay)
    {
        _net = $"joining {address}";
        ILink<ToClient, ToHost> link;
        if (relay is var (url, password))
        {
            var relayed = RelayLink<ToClient, ToHost>.Join(url, password, address);
            _together = new TogetherState(TogetherMode.Joining, address.ToUpperInvariant(), [], null, false);
            relayed.Failed += why =>
            {
                Log(why);
                _net = why;
                bool refused = why.StartsWith("no session");
                _together = TogetherState.Alone with
                {
                    Problem = refused ? T("menu.noGame", ("code", address.ToUpperInvariant())) : why,
                    PasswordRefused = !refused && _together.Mode == TogetherMode.Joining,
                };
                _poll = () => { };
            };
            link = relayed;
        }
        else
        {
            var (enet, error) = EnetLink<ToClient, ToHost>.Join(address, Port);
            if (error != Error.Ok) GD.PrintErr($"[guest] cannot reach {address}: {error}");
            link = enet;
        }
        link.Connected += (_, channel) =>
        {
            Log($"connected to {address}{(network != null ? $" over {network}" : "")}");
            _net = $"joined {address}";
            _stopLink = link.Close;
            Client.Join(Load, network?.Over(channel, new GodotTiming(GetTree())) ?? channel, SessionBuild,
                client =>
                {
                    CloseTitle();
                    _role = "guest";
                    _together = _together with { Mode = TogetherMode.Joined };
                    _session = new Session(null, client);
                    _start = client.World.Tick;
                    ShowWorld();
                    AimCamera(Registry.Wreck);
                    Log($"joined as peer {client.Id} at tick {client.World.Tick}");
                },
                why =>
                {
                    Log(why.Message);
                    _net = why.Message.Contains("runs build") ? T("menu.otherVersion") : why.Message;
                    _together = TogetherState.Alone with { Problem = _net };
                    _stopLink?.Invoke();
                    _stopLink = null;
                    _poll = () => { };
                },
                player: _playerKey);
        };
        link.Disconnected += _ =>
        {
            Log("the host left");
            if (_session != null) ExitToTitle(T("menu.hostLeft"));
        };
        _poll = link.Poll;
    }

    public TogetherState Together =>
        _together.Mode is TogetherMode.Hosting or TogetherMode.Joined ? _together with { Suits = SuitsAboard() } : _together;

    /// <summary>The suit colours aboard, this player's first, as the Play together page shows them.</summary>
    private IReadOnlyList<int> SuitsAboard()
    {
        if (_session is not Session s) return [];
        return _m.Players.Worn.OrderBy(w => w.Peer == s.Client.Id ? 0 : 1).ThenBy(w => w.Peer)
            .Select(w => w.Colour >= 0 && w.Colour < Suits.All.Length ? Suits.All[w.Colour] : Suits.All[0]).ToList();
    }

    public void HostTogether(string password)
    {
        // From the title: the world picked in Worlds, else Continue's, else a new one.
        if (_session == null)
        {
            if ((_title?.Worlds.Playable ?? _index.Continue()?.Id) is string id) Play(id);
            else NewWorld("");
            if (_session == null) return;
        }
        if (_role != "alone" || _together.Mode != TogetherMode.Alone) return;
        Serve(Codes.New(Random.Shared).ToLowerInvariant(), (Relay()?.Url ?? RelayFrames.PlaytestUrl, password));
    }

    public void JoinTogether(string code, string password)
    {
        if (_role != "alone" || _together.Mode != TogetherMode.Alone) return;
        Join(code.ToLowerInvariant(), null, (Relay()?.Url ?? RelayFrames.PlaytestUrl, password));
    }

    public void StopTogether()
    {
        if (_role != "host") return;
        _stopLink?.Invoke();
        _stopLink = null;
        _poll = () => { };
        _role = "alone";
        _net = null;
        _together = TogetherState.Alone;
    }

    /// <summary>A world from the host's save: a joiner's first, or a resync's.</summary>
    private World Load(string save)
    {
        if (_cheat)
        {
            // Once: a door this peer alone sees open, as a determinism bug would leave it.
            _cheat = false;
            var root = JsonNode.Parse(save)!;
            var grid = root["slices"]!["grid"]!;
            var structure = Runs.Cells(grid["structure"]!);
            int door = Array.FindIndex(structure, s => Doorways.IsDoor(s) && Doorways.IsClosed(s));
            structure[door] = Doorways.Toggled(structure[door], open: true);
            grid["structure"] = Runs.Node(structure);
            save = root.ToJsonString();
            Log($"cheated: door {door} open here only");
        }
        return World.Load(save, Modules());
    }

    /// <summary>The menu's save: the world as this machine has it, to its folder; how it went as an i18n key.</summary>
    private string SaveWorld()
    {
        if (_session == null || _worldId == null || _role == "guest") return "menu.nothingToSave";
        return SaveNow() ? "menu.saved" : "menu.saveFailed";
    }

    /// <summary>Writes the world played to its folder, the save before kept; false, and why kept for the menu's foot, when it could not.</summary>
    private bool SaveNow()
    {
        if (_session is not Session s || _worldId is not string id) return false;
        try
        {
            _index.Write(id, s.Client.World.Save());
            (_lastSaveTick, _savedAt, _saveProblem) = (s.Client.World.Tick, DateTime.Now, null);
            TakeShot();
            return true;
        }
        catch (Exception e) when (e is System.IO.IOException or UnauthorizedAccessException)
        {
            Log($"not saved: {e.Message}");
            (_lastSaveTick, _saveProblem) = (s.Client.World.Tick, e.Message);
            return false;
        }
    }

    /// <summary>Keeps who is who in the world played, as soon as a seat is given: a crash then forgets nobody.</summary>
    private void SaveRoster()
    {
        if (_worldId is not string id) return;
        try
        {
            _index.WritePlayers(id, _roster.ToJson());
        }
        catch (Exception e) when (e is System.IO.IOException or UnauthorizedAccessException)
        {
            Log($"the roster not kept: {e.Message}");
        }
    }

    /// <summary>Saves the world when this machine keeps it: before every way out.</summary>
    private void SaveIfOwned()
    {
        if (_role != "guest") SaveNow();
    }

    /// <summary>The menu's foot: the world, when it was saved, and the code while hosting.</summary>
    private string Foot()
    {
        var parts = new List<string>();
        if (_worldId != null) parts.Add(_index.NameOf(_worldId));
        if (_saveProblem != null) parts.Add(T("menu.notSaved", ("why", _saveProblem)));
        else if (_savedAt is DateTime at)
        {
            int minutes = (int)(DateTime.Now - at).TotalMinutes;
            parts.Add(minutes < 1 ? T("menu.savedNow") : T("menu.savedAgo", ("minutes", minutes)));
        }
        if (_together.Mode == TogetherMode.Hosting) parts.Add(T("menu.hostingCode", ("code", _together.Code ?? "")));
        else if (_together.Problem != null && _role != "guest") parts.Add(_together.Problem);
        return string.Join(" · ", parts);
    }

    /// <summary>The title, loading nothing: what it says first, if the scene came back to it with a reason.</summary>
    private void ShowTitle()
    {
        AdoptOldSave();
        var worlds = new WorldsPanel(_index.List, Play, id => _index.Delete(id), NewWorld, ShotOf,
            busy: () => _together.Mode is TogetherMode.Joining or TogetherMode.Opening);
        AddChild(_title = new TitleScreen(_index.Continue, Continue, worlds, this, new SettingsRows(_hands.Ui), Quit,
            Updates.Of(Build).ToString(), () => _updater.Available?.Tag, _updater.Update, () => _updater.Status,
            testbed: OS.IsDebugBuild() ? PlayTestbed : null));
        if (_next is { } said) _title.Say(said);
        _next = null;
    }

    /// <summary>The save slot of v0.0.3 and before, taken into the worlds once, so an update loses no ship.</summary>
    private void AdoptOldSave()
    {
        try
        {
            if (_index.Adopt(ProjectSettings.GlobalizePath("user://saves/world.json"), T("title.oldSave"), DateTime.Now) is string id)
                Log($"the old save slot is world {id} now");
        }
        catch (Exception e) when (e is System.IO.IOException or UnauthorizedAccessException)
        {
            Log($"the old save slot not taken in: {e.Message}");
        }
    }

    private void CloseTitle()
    {
        _title?.QueueFree();
        _title = null;
    }

    /// <summary>Continue: the last world played, or a new one on a first run.</summary>
    private void Continue()
    {
        if (_together.Mode is TogetherMode.Joining or TogetherMode.Opening) return;
        if (_index.Continue() is { } world) Play(world.Id);
        else NewWorld("");
    }

    /// <summary>Plays world <paramref name="id"/>, its save brought up to this build's format; a failure stays on the title, saying why.</summary>
    private void Play(string id)
    {
        try
        {
            string save = _index.Read(id) ?? throw new System.IO.FileNotFoundException(T("worlds.noSave"));
            // The registry is swapped only once the save has loaded: a broken one leaves nothing half made.
            var m = new Registry();
            var world = World.Load(Migrations.Upgrade(save), m.All);
            _m = _ship = m;
            Enter(world, id, Roster.Read(_index.ReadPlayers(id), _playerKey));
        }
        catch (Exception e) when (e is System.IO.IOException or System.Text.Json.JsonException or System.IO.InvalidDataException or KeyNotFoundException or InvalidOperationException)
        {
            Log($"world {id} not loaded: {e.Message}");
            _title?.Worlds.Say(T("worlds.failed", ("why", e.Message)));
            _title?.Say(T("worlds.failed", ("why", e.Message)));
        }
    }

    /// <summary>A new world in the wreck, named <paramref name="name"/>, saved at once and played.</summary>
    private void NewWorld(string name)
    {
        string id = _index.Create(name, DateTime.Now);
        var world = World.Create(1, Registry.Wreck, Modules());
        _index.Write(id, world.Save());
        Enter(world, id, Roster.Empty());
    }

    /// <summary>Plays <paramref name="world"/> as a session of this machine's own, kept as <paramref name="id"/>.</summary>
    private void Enter(World world, string id, Roster roster)
    {
        _roster = roster;
        int seat = roster.SeatOf(_playerKey);
        var (host, client) = LocalSession.Create(world, SessionBuild, seat);
        _session = new Session(host, client);
        // Saved with friends aboard, the world still holds their heroes, on whichever surface: they come back when their
        // players do, aboard (0076). This player comes back aboard too, at the pod.
        foreach (var surface in world.Surfaces)
            foreach (var hero in Registry.On(world, surface.Id).Crew.Heroes.Where(h => h.Id != seat).ToList())
                host.Release(hero.Id, client.World.Encode(new Sim.Crew.Leave()));
        // Sent as this player's own, so the client holds it in flight when it comes back; released by the host for it, the
        // client took it for a desync and stopped, the world frozen.
        if (world.SurfaceOf(seat) != 0) client.Send(new Transfer(0));
        (_worldId, _lastSaveTick, _savedAt, _saveProblem) = (id, world.Tick, DateTime.Now, null);
        SaveRoster();
        CloseTitle();
        ShowWorld();
        AimCamera(Registry.Wreck);
    }

    /// <summary>
    /// Leaves the world for the title: saved if this machine keeps it, the link closed and the session dropped,
    /// what is drawn of it let go, and the scene loaded afresh once the frame ends, every view with it;
    /// <paramref name="why"/> said on the title.
    /// </summary>
    private void ExitToTitle(string? why)
    {
        SaveIfOwned();
        _stopLink?.Invoke();
        _stopLink = null;
        _session = null;
        Bolts.Shared.Clear();
        Glims.Shared.Clear();
        Flares.Shared.Clear();
        _next = why;
        // A reload takes the scene out of the tree at once: deferred, the frame that asked (a click, the host gone) ends whole.
        GetTree().CallDeferred(SceneTree.MethodName.ReloadCurrentScene);
    }
}
