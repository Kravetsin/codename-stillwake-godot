using System;
using System.Linq;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Landing;
using Stillwake.Sim.Places;

namespace Stillwake.Game.Dev;

/// <summary>
/// The testbed's hero by the crew shuttle in its hangar (shuttle-flight): as it starts, a freighter is found and the course set to
/// it, both sent as commands, so the radar shows the route and the shuttle its lamp. <c>--trip=auto</c> plays the trip by itself:
/// boards, sets off, gets out three seconds after arriving by the vessel, boards again and flies home, for a sheet of frames.
/// </summary>
[Dev("--trip", "the testbed's hero by the crew shuttle, a freighter found and the course set to it; =auto boards, sets off, gets out 3 s after arriving, boards again and flies home (ShuttleScenario)", Shape = DevShape.Either, Order = 1)]
public sealed class ShuttleScenario : TestbedScenario
{
    /// <summary>The tick the hero boards on with <c>=auto</c>; it sets off <see cref="Wait"/> ticks later.</summary>
    private const int BoardAt = 40;
    private const int Wait = 10;
    /// <summary>Ticks seated by the vessel before it gets out, and seated again before it flies home.</summary>
    private const int StayFor = 3 * World.TicksPerSecond;
    private const string Shuttle = "crewShuttle";

    private bool _found, _coursed;
    private int? _arrivedAt;
    private int _readAt = -1;
    /// <summary>How many of <c>=auto</c>'s steps are sent: board, set off, out, in, home.</summary>
    private int _step;

    /// <summary>The next step taken: true, so a condition sends it once.</summary>
    private bool Next()
    {
        _step++;
        return true;
    }

    private bool Auto => Args.Value("--trip") == "auto";

    public override Func<Registry, int>? Start => m => OpenDemo.StartOf(m, Shuttle);

    public override void Frame(Client client, Registry m, Action<string> log)
    {
        int tick = client.World.Tick;
        // Frames come faster than ticks: each tick is acted on once.
        if (tick == _readAt) return;
        _readAt = tick;
        Course(client, m, log);
        if (Auto) Play(client, m, tick, log);
    }

    /// <summary>A freighter found, then the course set to it, once each and only aboard, as a host's commands.</summary>
    private void Course(Client client, Registry m, Action<string> log)
    {
        if (client.World.SurfaceOf(client.Id) != 0) return;
        if (!_found && m.Places.Course == null && !m.Places.Contacts.Any(c => c.Class == "freighter"))
        {
            _found = true;
            client.Send(new Find("freighter"));
        }
        if (!_coursed && m.Places.Course == null && m.Places.Contacts.LastOrDefault(c => c.Class == "freighter") is { } contact)
        {
            _coursed = true;
            client.Send(new SetCourse(contact.Id));
            log($"course set to contact {contact.Id}: {PlacesModule.CostOf(contact)} l");
        }
    }

    /// <summary><c>=auto</c>: board and set off; by the vessel, out three seconds after arriving, back in, and home.</summary>
    private void Play(Client client, Registry m, int tick, Action<string> log)
    {
        int surface = client.World.SurfaceOf(client.Id);
        // Each step once, at or after its tick: a frame may run several ticks.
        if (surface == 0 && _arrivedAt == null)
        {
            if (_step == 0 && tick >= BoardAt && m.Grid.MachinesOf(Shuttle).FirstOrDefault() is { } shuttle && Next()) client.Send(new Board(shuttle.Id));
            if (_step == 1 && tick >= BoardAt + Wait && Next())
            {
                log($"setting off: {m.Places.SetOffProblem(client.Id) ?? "ready"}");
                client.Send(new SetOff());
            }
            return;
        }
        if (surface == 0) return;
        if (_arrivedAt == null)
        {
            _arrivedAt = tick;
            log($"arrived on surface {surface} at tick {tick}, seated: {m.Landing.SeatOf(client.Id) != null}");
        }
        int since = tick - _arrivedAt.Value;
        if (_step == 2 && since >= StayFor && Next()) client.Send(new Alight());
        if (_step == 3 && since >= 2 * StayFor && m.Grid.MachinesOf(Shuttle).FirstOrDefault() is { } parked && Next()) client.Send(new Board(parked.Id));
        if (_step == 4 && since >= 2 * StayFor + Wait && Next())
        {
            log($"flying home: {m.Places.SetOffProblem(client.Id) ?? "ready"}");
            client.Send(new SetOff());
        }
    }
}
