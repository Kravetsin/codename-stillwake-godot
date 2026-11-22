using System;
using System.Text.Json.Nodes;
using Stillwake.Game.Features.Crew;
using Stillwake.Net;
using Stillwake.Sim;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// The forms' visions (form-vision), for frames: a corridor of hull, the hero paired in its middle, creative so nothing harms it. The scanner's
/// vision plays from 3 to 16.8 s, the lamp's from 18 to 30.7 s and the alarms' from 32 to 45.5 s, each whole, one after another, timed by the
/// frames shown, so a recording at a fixed rate plays them whole. No lesson is learned: the visions are played on the hero's view, which tells
/// the sim as a lesson's would.
/// </summary>
[Dev("--form-vision", "a paired hero in a corridor, creative: the scanner's vision at 3-16.8 s, the lamp's at 18-30.7, the alarms' at 32-45.5 (FormVisionScenario)")]
public sealed class FormVisionScenario : DevScenario
{
    /// <summary>The corridor, a character a 16 px block, doubled (0104): the hero in its middle.</summary>
    private static readonly Layout Corridor = Layout.Parse(
        [
            new string('#', 30),
            new string('.', 30),
            new string('.', 30),
            new string('.', 15) + "S" + new string('.', 14),
            new string('.', 30),
            new string('#', 30),
        ]).Doubled() with
    { Known = Spec.Research.StartKnown };

    private const int Peer = 0;

    /// <summary>Each vision's form and when it begins, seconds from the hero's first frame.</summary>
    private static readonly (string Form, double At)[] Visions =
        [(Spec.LightDrone.ScannerMode, 3), (Spec.LightDrone.LampMode, 18), (Spec.LightDrone.AlarmsMode, 32)];

    private DevContext? _context;
    private bool _creative, _hooked;
    private double _ms;

    public override Layout? Map => Corridor;

    public override World Seed(World world, DevSeed seed)
    {
        var root = JsonNode.Parse(world.Save())!;
        root["slices"]!["players"]!["drones"] = new JsonArray(Peer);
        return World.Load(root.ToJsonString(), seed.Modules());
    }

    public override void Attach(DevContext c) => _context = c;

    public override void Frame(Client client, Registry m, Action<string> log)
    {
        if (m.Crew.HeroOf(client.Id) == null) return;
        if (!_creative)
        {
            _creative = true;
            client.Send(new Sim.Items.SetCreative(true));
        }
        if (_hooked || _context?.HeroView() is not { } hero) return;
        _hooked = true;
        hero.VisionScript = Playing;
        log("form-vision: the scanner's at 3-16.8 s, the lamp's at 18-30.7 s, the alarms' at 32-45.5 s");
    }

    /// <summary>The vision playing <paramref name="elapsedMs"/> after the last frame, and how far in; none between them.</summary>
    private (string Form, double Seconds)? Playing(double elapsedMs)
    {
        _ms += elapsedMs;
        double s = _ms / 1000;
        foreach (var (form, at) in Visions)
            if (FormVision.Of(form) is { } kind && FormVision.Playing(kind, s - at)) return (form, s - at);
        return null;
    }
}
