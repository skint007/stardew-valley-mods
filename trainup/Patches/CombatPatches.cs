using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using TrainUp.Systems;

namespace TrainUp.Patches;

/// <summary>
/// Prefix on <see cref="GameLocation.damageMonster"/> that scales the local player's outgoing
/// weapon damage by the Bloodied perk (more damage the lower their health).
/// </summary>
public static class CombatPatches
{
    private static IMonitor _monitor = null!;

    public static void Init(IMonitor monitor) => _monitor = monitor;

    public static void Apply(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.damageMonster), new[]
            {
                typeof(Rectangle), typeof(int), typeof(int), typeof(bool), typeof(float),
                typeof(int), typeof(float), typeof(float), typeof(bool), typeof(Farmer), typeof(bool)
            }),
            prefix: new HarmonyMethod(typeof(CombatPatches), nameof(DamageMonster_Prefix)));
    }

    public static void DamageMonster_Prefix(ref int minDamage, ref int maxDamage, Farmer who)
    {
        try
        {
            if (who == null || who != Game1.player) return;

            float mult = Perks.BloodiedMultiplier();
            if (mult > 1f)
            {
                minDamage = (int)(minDamage * mult);
                maxDamage = (int)(maxDamage * mult);
            }
        }
        catch (Exception ex)
        {
            _monitor.Log($"CombatPatches.DamageMonster_Prefix failed: {ex}", LogLevel.Error);
        }
    }
}
