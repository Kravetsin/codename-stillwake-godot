using System;
using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Landing;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// The thermometer to try by hand (the-thermometer): the hero beamed down to the first planet's pod suited and helmeted, the
/// axe, the drill and the lamp as <c>--planet</c> gives them, a cape on the hotbar to wear. F6 beams it to the next band's cave,
/// garden, amber, ember, then back to the pod; F7 brings the night, again the day. Creative, so the cold reads at no cost; the
/// suit may be taken off into the bag to feel it bare. Back aboard and out of the ship through the beam, for the flight out.
/// </summary>
[Dev("--thermometer", "the first planet, the hero suited with a cape on the hotbar; F6 the next band's cave, F7 night or day, to try the thermometer (the-thermometer)")]
public sealed partial class ThermometerScenario : DevScenario
{
    /// <summary>The caves F6 beams to in turn, a band each, then the pod.</summary>
    private static readonly string[] Caves = ["bloomCache", "amberCache", "emberCache"];

    private readonly Keys _keys = new();
    private int _cave = -1;
    private bool _down;

    public override World Seed(World world, DevSeed seed)
    {
        var root = JsonNode.Parse(GroundDemo.Seed(world, seed.Modules).Save())!;
        var slots = root["slices"]!["items"]!["inventories"]![0]!["slots"]!.AsArray();
        int Worn(string slot) => Spec.Inventory.Slots + Spec.Inventory.Equipment.ToList().IndexOf(slot);
        slots[Worn(Spec.Suit.Slot)] = Stack(Spec.Suit.Basic);
        slots[Worn(Spec.Helmet.Slot)] = Stack(Spec.Helmet.Basic);
        slots[4] = Stack(Spec.Cape.Item);
        return World.Load(root.ToJsonString(), seed.Modules());
    }

    private static JsonObject Stack(string item) => new() { ["item"] = item, ["count"] = 1 };

    public override void Attach(DevContext c) => c.Add(_keys);

    public override void Started(Client client)
    {
        client.Send(new Sim.Items.SetCreative(true));
        client.Send(new OpenSurface(Spec.PlanetTypes.First.Id));
    }

    public override void Frame(Client client, Registry m, Action<string> log)
    {
        int planet = client.World.Surfaces.Count - 1;
        if (planet < 1) return;
        if (!_down)
        {
            _down = true;
            client.Send(new Transfer(planet));
        }
        if (_keys.Take(Key.F6))
        {
            _cave = _cave + 1 > Caves.Length - 1 ? -1 : _cave + 1;
            if (_cave < 0)
            {
                client.Send(new Transfer(planet));
                _keys.Say("back to the pod");
            }
            else
            {
                PlanetScenario.Cache(client, log, Caves[_cave]);
                _keys.Say($"down to the {Caves[_cave]}");
            }
        }
        if (_keys.Take(Key.F7) && m.Rules.Cycle is { } day)
        {
            bool night = m.Sky.Now.Chill > 0;
            client.Send(new Sim.Sky.SetTime(night ? 0 : day.Length - day.Night));
            _keys.Say(night ? "day" : "night");
        }
    }

    /// <summary>F6 and F7 pressed, kept until the scenario's next frame takes them; what was done shown a moment top-left.</summary>
    private sealed partial class Keys : CanvasLayer
    {
        private readonly Label _said = new() { Position = new Vector2(16, 16), Visible = false };
        private bool _f6, _f7;
        private double _shown;

        public bool Take(Key key)
        {
            ref bool pressed = ref key == Key.F6 ? ref _f6 : ref _f7;
            bool was = pressed;
            pressed = false;
            return was;
        }

        public void Say(string text) => (_said.Text, _said.Visible, _shown) = (text, true, 2.5);

        public override void _Ready()
        {
            Layer = 100;
            _said.AddThemeFontSizeOverride("font_size", 22);
            _said.AddThemeColorOverride("font_outline_color", Colors.Black);
            _said.AddThemeConstantOverride("outline_size", 6);
            AddChild(_said);
        }

        public override void _UnhandledInput(InputEvent e)
        {
            if (e is not InputEventKey { Pressed: true, Echo: false } key || key.PhysicalKeycode is not (Key.F6 or Key.F7)) return;
            if (key.PhysicalKeycode == Key.F6) _f6 = true;
            else _f7 = true;
            GetViewport().SetInputAsHandled();
        }

        public override void _Process(double delta)
        {
            if (_shown > 0 && (_shown -= delta) <= 0) _said.Visible = false;
        }
    }
}
