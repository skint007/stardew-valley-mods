using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;
using TrainUp.Systems;

namespace TrainUp.Patches;

/// <summary>
/// Patches on <see cref="Farmer.takeDamage"/>:
///   • <b>Prefix</b> rolls the Evasive/Acrobat dodge; on a dodge it cancels the hit entirely
///     (and Counter heals a little). It also records pre-hit HP for the postfix.
///   • <b>Postfix</b> awards Defense XP for HP actually lost and applies the Retaliate reflect.
/// </summary>
public static class FarmerPatches
{
    private static XpAwarder _xp = null!;
    private static IMonitor _monitor = null!;

    public static void Init(XpAwarder xp, IMonitor monitor)
    {
        _xp = xp;
        _monitor = monitor;
    }

    public static void Apply(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(Farmer), nameof(Farmer.takeDamage),
                new[] { typeof(int), typeof(bool), typeof(Monster) }),
            prefix: new HarmonyMethod(typeof(FarmerPatches), nameof(TakeDamage_Prefix)),
            postfix: new HarmonyMethod(typeof(FarmerPatches), nameof(TakeDamage_Postfix)));
    }

    /// <summary>Returns false to skip the hit (dodge). __state carries pre-hit HP to the postfix.</summary>
    public static bool TakeDamage_Prefix(Farmer __instance, out int __state)
    {
        __state = __instance.health;

        try
        {
            if (__instance != Game1.player) return true;

            float dodge = Perks.DodgeChance();
            if (dodge > 0f && Game1.random.NextDouble() < dodge)
            {
                // Visual/audio feedback for the dodge.
                __instance.currentLocation?.playSound("dwop");
                __instance.currentLocation?.debris.Add(new Debris(
                    ModEntry.Instance.Helper.Translation.Get("perk.dodge"),
                    1, __instance.Position, Color.White, 1f, 0f));

                if (Perks.HasCounter)
                    __instance.health = Math.Min(__instance.maxHealth, __instance.health + 2);

                return false; // skip the original: no damage, no invincibility frames
            }
        }
        catch (Exception ex)
        {
            _monitor.Log($"FarmerPatches.TakeDamage_Prefix failed: {ex}", LogLevel.Error);
        }

        return true;
    }

    public static void TakeDamage_Postfix(Farmer __instance, int __state, Monster? damager)
    {
        try
        {
            if (__instance != Game1.player) return;

            int lost = __state - __instance.health;
            if (lost <= 0) return;

            _xp.AwardDefenseFromDamage(lost);

            // Retaliate: reflect a fraction of the damage back to the attacker.
            float reflect = Perks.RetaliateFraction();
            if (reflect > 0f && damager != null && damager.Health > 0)
            {
                int dmg = Math.Max(1, (int)(lost * reflect));
                damager.takeDamage(dmg, 0, 0, false, 0.0, __instance);
            }
        }
        catch (Exception ex)
        {
            _monitor.Log($"FarmerPatches.TakeDamage_Postfix failed: {ex}", LogLevel.Error);
        }
    }
}
