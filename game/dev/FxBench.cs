using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Dev;

/// <summary>
/// A busy scene of luminous effects for measuring them (<c>-- --fx-bench</c>, with <c>--measure</c>): round the
/// camera's middle, <see cref="Fires"/> fires' embers, ash and cracking coals, each with its shimmer,
/// <see cref="Sites"/> plans' spark bursts and a wreck's break every second, as the features throw them (0075);
/// and a plan welded on above the middle, hot, smoking and shimmering, with the aim's outline drawn meanwhile, as
/// a hero working a plan has it: what once made the shimmer read the outline's buffer and draw a black disc.
/// Slice 2's too, through the features' own pieces: two jetpacks thrusting, a room venting through three openings,
/// three drones flying to and fro, an electric smelter at work, an arc drill biting, its coil arcing, and a lamp at
/// half its power catching and dying (its sparks and arcs only: no lamp is drawn). Slice 3's: a catch of five items every
/// second and a half flying home one after another, those past the cap (<see cref="Fx.MostRibbons"/>) not at all.
/// </summary>
public partial class FxBench : Node2D
{
    private const int Fires = 8, Sites = 4;
    private readonly Random _random = new(1);
    private readonly Shimmer[] _shimmers = new Shimmer[Fires];
    private double _embers, _ash, _bursts, _breakMs, _crackMs;
    private readonly Features.Building.BuildGlow _weld = new();
    private readonly Outline _aim = new(Layers.Aim) { ZAsRelative = false };
    private double _weldMs;
    private readonly Features.Crew.JetPlume[] _plumes = [new(), new()];
    private readonly Features.Atmosphere.VentLight _vents = new();
    private readonly Features.Logistics.DroneLight _drones = new();
    private readonly Features.Logistics.Hover[] _hovers = [new(1), new(2), new(3)];
    private readonly Features.Crafting.SmelterHeat _smelter = new();
    private readonly DrillLight _drill = new();
    private readonly Features.Light.LampSparks _lamp = new();
    private readonly Features.Items.PickUpFlights _flights = new();
    private double _clock, _catchMs, _drillMs;
    private static readonly double DrillPaceMs = Content.Weapons.Drill.Pace * 1000.0 / Sim.World.TicksPerSecond;

    public FxBench()
    {
        ZIndex = Layers.Fire;
        for (int i = 0; i < Fires; i++) AddChild(_shimmers[i] = new Shimmer());
        AddChild(_weld);
        AddChild(_aim);
        foreach (var n in (Node2D[])[.. _plumes, _vents, _drones, _smelter, _flights]) AddChild(n);
    }

    /// <summary>Slice 2's pieces round <paramref name="middle"/>, <paramref name="ms"/> on.</summary>
    private void Slice2(Vector2 middle, double ms)
    {
        _clock += ms / 1000;
        for (int i = 0; i < _plumes.Length; i++)
        {
            double a = _clock * 0.7 + i * Math.PI;
            _plumes[i].Step(middle + new Vector2(-150 + i * 300, -70), (Math.Cos(a), Math.Sin(a)), ms);
        }
        var vent = (middle + new Vector2(170, 10)) / ShipCamera.Cell;
        _vents.Step([new(vent.X, vent.Y, 0, 24), new(vent.X, vent.Y + 1, 0, 24), new(vent.X, vent.Y + 2, 0, 24)], ms);
        _drones.Begin();
        for (int i = 0; i < _hovers.Length; i++)
        {
            var h = _hovers[i];
            double tx = middle.X - 100 + 100 * i + 40 * Math.Sin(_clock * 0.9 + i), ty = middle.Y + 60;
            if (h.Spin == 0) h.Place(tx, ty);
            h.Step(ms / 1000, tx, ty, 36 * Math.Cos(_clock * 0.9 + i), 0, 1, 40, DroneLook.Lively);
            _drones.Drone(i, h, h.Bob(40, DroneLook.Lively), ty + 20, ms);
        }
        _drones.End();
        _smelter.Step(middle + new Vector2(-60, 90), true, ms);
        _lamp.Begin();
        _lamp.Lamp(1, middle + new Vector2(0, -100), Features.Light.Flicker.BrightnessOf(0.5f, _clock * 1000, 7), middle.Y - 40, ms);
        _lamp.End();
        var grip = middle + new Vector2(60, 90);
        _drill.Step(HeldLook.ArcDrill, 1, p => grip + new Vector2((float)(p.X - HeldLook.ArcDrill.Grip.X), (float)(p.Y - HeldLook.ArcDrill.Grip.Y)), ms);
        // Its hits on a wreck at its bit, at its pace (0094).
        var bit = grip + new Vector2(29, -5);
        for (_drillMs += ms; _drillMs >= DrillPaceMs; _drillMs -= DrillPaceMs)
            HitBurst.Shared.Throw(HitLooks.Of[Impact.Wreck], bit.X, bit.Y, -1, 0, Features.Effects.Impacts.ShareOf(Content.Weapons.Drill.Item));
        var chest = middle + new Vector2(150, -40);
        for (_catchMs += ms; _catchMs >= 1500; _catchMs -= 1500)
        {
            string[] loot = ["scrap", "seed", "canister", "planks", "iron"];
            for (int i = 0; i < loot.Length; i++) _flights.Fly(loot[i], chest + new Vector2(40, -12), i);
        }
        _flights.Update(chest, ms);
    }

    private double Between(double from, double to) => from + (to - from) * _random.NextDouble();

    public override void _Process(double delta)
    {
        double ms = delta * 1000;
        var middle = GetViewport().GetCamera2D()?.GetScreenCenterPosition() ?? Vector2.Zero;
        Slice2(middle, ms);
        var glims = Glims.Shared;
        _embers += 9 * Fires * ms / 1000;
        _ash += 7.2 * Fires * ms / 1000;
        _bursts += 0.42 * 20 * Sites * ms / 1000;
        _crackMs += ms;
        _breakMs += ms;
        var weld = middle + new Vector2(30, -20);
        for (_weldMs += ms; _weldMs >= 50; _weldMs -= 50) _weld.Sparked(1, weld, Math.PI, 1, false, middle.Y + 40);
        _aim.Begin();
        _aim.AddBox(new Rect2(weld + new Vector2(0, -8), new Vector2(16, 16)), Colors.White);
        _aim.AddBox(new Rect2(middle + new Vector2(-2, -34), new Vector2(12, 12)), new Color("7fe08a"));
        _aim.End();
        for (int i = 0; i < Fires; i++)
        {
            var at = middle + new Vector2(-200 + i * 50, 40);
            _shimmers[i].Box = new Rect2(at + new Vector2(-8, -39), new Vector2(26, 42));
            _shimmers[i].Amplitude = 0.55f;
        }
        for (; _embers >= 1; _embers--)
        {
            var at = middle + new Vector2(-200 + _random.Next(Fires) * 50, 40);
            glims.Add(Luminous.Ember, at.X + Between(-3.5, 3.5), at.Y, Between(-6, 6), -Between(14, 32));
        }
        for (; _ash >= 1; _ash--)
        {
            var at = middle + new Vector2(-200 + _random.Next(Fires) * 50, 40);
            glims.Add(Luminous.Ash, at.X + Between(-4, 4), at.Y - 3, Between(-3, 3), -Between(4, 9));
        }
        if (_crackMs >= 110)
        {
            _crackMs -= 110;
            var at = middle + new Vector2(-200 + _random.Next(Fires) * 50, 40);
            for (int i = 0; i < 6; i++) glims.Add(Luminous.CrackSpark, at.X, at.Y - 2, Between(-28, 28), -Between(55, 95), at.Y + 13);
            Flares.Shared.Glint(at.X, at.Y, 6, (255, 190, 110));
        }
        for (; _bursts >= 1; _bursts--)
        {
            var tip = middle + new Vector2(-120 + _random.Next(Sites) * 80, -60);
            glims.Spray(Luminous.BuildSpark, tip.X, tip.Y, 3, Math.PI, (-1.15, 0.9), (55, 150), tip.Y + 30);
            Flares.Shared.Glint(tip.X, tip.Y, 4.5, (255, 200, 120));
            Flares.Shared.Flash(tip.X, tip.Y, (255, 170, 80), 0.45, 2, 0.3f, 0.7);
        }
        if (_breakMs >= 1000)
        {
            _breakMs -= 1000;
            var at = middle + new Vector2(0, -20);
            for (int i = 0; i < 26; i++)
            {
                double a = Between(0, Math.Tau), s = Between(40, 150);
                glims.Add(Luminous.BreakSpark, at.X, at.Y, Math.Cos(a) * s, Math.Sin(a) * s);
            }
            for (int i = 0; i < 44; i++)
            {
                double a = Between(0, Math.Tau), s = Between(4, 30);
                glims.Add(Luminous.BreakDust, at.X, at.Y, Math.Cos(a) * s, Math.Sin(a) * s);
            }
            for (int i = 0; i < 10; i++)
            {
                double a = Between(0, Math.Tau), s = Between(4, 14);
                glims.Add(Luminous.Vapour, at.X, at.Y, Math.Cos(a) * s, Math.Sin(a) * s);
            }
            Flares.Shared.Glint(at.X, at.Y, 18, (255, 214, 150));
            Flares.Shared.Shock(at.X, at.Y, 4, 44, 14 * 1000.0 / 60, (255, 236, 210), 0.4);
            Flares.Shared.Flash(at.X, at.Y, (255, 226, 180), 0.55, 4, 0.45f, 0.9);
        }
    }
}
