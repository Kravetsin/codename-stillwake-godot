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
/// </summary>
public partial class FxBench : Node2D
{
    private const int Fires = 8, Sites = 4;
    private readonly Random _random = new(1);
    private readonly Shimmer[] _shimmers = new Shimmer[Fires];
    private double _embers, _ash, _bursts, _breakMs, _crackMs;
    private readonly Features.Building.BuildGlow _weld = new();
    private readonly Outline _aim = new(Layers.Aim);
    private double _weldMs;

    public FxBench()
    {
        ZIndex = Layers.Fire;
        for (int i = 0; i < Fires; i++) AddChild(_shimmers[i] = new Shimmer());
        AddChild(_weld);
        AddChild(_aim);
    }

    private double Between(double from, double to) => from + (to - from) * _random.NextDouble();

    public override void _Process(double delta)
    {
        double ms = delta * 1000;
        var middle = GetViewport().GetCamera2D()?.GetScreenCenterPosition() ?? Vector2.Zero;
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
