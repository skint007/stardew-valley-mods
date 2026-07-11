using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using TrainUp.Config;
using TrainUp.Systems;

namespace TrainUp.Patches;

/// <summary>
/// Postfix on <see cref="GameLocation.damageMonster"/> — the single choke point every melee
/// swing routes through, called with the swing's area of effect, damage range, and crit info even
/// when no monsters are present. If the swing overlaps a placed Training Dummy, we show the hit's
/// damage as a floating number (rolled exactly like a real monster hit, crits and all, so the
/// dummy doubles as a damage tester) and award vanilla Combat XP. Debounced per dummy tile so one
/// swing can't count many times.
/// </summary>
public static class DummyHitPatches
{
    private static ModConfig _config = null!;
    private static XpAwarder _xp = null!;
    private static IMonitor _monitor = null!;

    /// <summary>Last game-tick a given dummy tile reacted, to debounce multi-frame swings.</summary>
    private static readonly Dictionary<Vector2, int> _lastHitTick = new();
    private const int CooldownTicks = 12; // ~200ms at 60fps

    public static void Init(ModConfig config, XpAwarder xp, IMonitor monitor)
    {
        _config = config;
        _xp = xp;
        _monitor = monitor;
    }

    public static void Apply(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.damageMonster), new[]
            {
                typeof(Rectangle), typeof(int), typeof(int), typeof(bool), typeof(float),
                typeof(int), typeof(float), typeof(float), typeof(bool), typeof(Farmer), typeof(bool)
            }),
            postfix: new HarmonyMethod(typeof(DummyHitPatches), nameof(DamageMonster_Postfix)));
    }

    // Parameter names must match GameLocation.damageMonster so Harmony binds them.
    public static void DamageMonster_Postfix(
        GameLocation __instance, Rectangle areaOfEffect,
        int minDamage, int maxDamage, float critChance, float critMultiplier, Farmer who)
    {
        try
        {
            if (!_config.Enabled || !_config.DummyEnabled) return;
            if (who == null || who != Game1.player) return;

            int tileLeft = areaOfEffect.Left / 64;
            int tileRight = areaOfEffect.Right / 64;
            int tileTop = areaOfEffect.Top / 64;
            int tileBottom = areaOfEffect.Bottom / 64;

            for (int tx = tileLeft; tx <= tileRight; tx++)
            {
                for (int ty = tileTop; ty <= tileBottom; ty++)
                {
                    var tile = new Vector2(tx, ty);
                    if (!__instance.Objects.TryGetValue(tile, out var obj)) continue;
                    if (obj?.QualifiedItemId != DummyContent.QualifiedId) continue;

                    if (_lastHitTick.TryGetValue(tile, out int last) && Game1.ticks - last < CooldownTicks)
                        continue;
                    _lastHitTick[tile] = Game1.ticks;

                    obj.shakeTimer = 150; // jitter the sprite on hit, like a clicked scarecrow
                    ShowDamage(__instance, tile, minDamage, maxDamage, critChance, critMultiplier, who);
                    _xp.AwardDummyCombatXp(who);
                }
            }
        }
        catch (Exception ex)
        {
            _monitor.Log($"DummyHitPatches.DamageMonster_Postfix failed: {ex}", LogLevel.Error);
        }
    }

    /// <summary>
    /// Roll and display the hit's damage the same way <see cref="GameLocation.damageMonster"/>
    /// does for a real monster, so the number shown is what your weapon would actually deal.
    /// </summary>
    private static void ShowDamage(GameLocation location, Vector2 tile,
        int minDamage, int maxDamage, float critChance, float critMultiplier, Farmer who)
    {
        int dmg = Game1.random.Next(minDamage, Math.Max(minDamage, maxDamage) + 1);

        bool crit = Game1.random.NextDouble() < critChance + who.LuckLevel * (critChance / 40f);
        if (crit)
        {
            dmg = (int)(dmg * critMultiplier);
            location.playSound("crit");
        }
        location.playSound("hitEnemy");

        // Float the number over the dummy's upper body (mirrors the +16 offset vanilla uses).
        var origin = new Vector2(tile.X * 64f + 40f, tile.Y * 64f - 16f);
        location.debris.Add(new Debris(
            dmg, origin,
            crit ? Color.Yellow : new Color(255, 130, 0),
            crit ? 1f + dmg / 300f : 1f,
            null));
    }
}
