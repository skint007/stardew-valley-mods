using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using TrainUp.Systems;

namespace TrainUp.Patches;

/// <summary>
/// Prefix on the <see cref="Farmer.Stamina"/> setter that reduces energy <em>spent</em> by the
/// local player for the Efficient (10% cheaper) and Conservationist (20% chance free) perks.
/// Only expenditures are touched (when the new value is lower than the current); gains from
/// eating, regen, and sleep pass through unchanged.
/// </summary>
public static class StaminaPatches
{
    private static IMonitor _monitor = null!;

    public static void Init(IMonitor monitor) => _monitor = monitor;

    public static void Apply(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.PropertySetter(typeof(Farmer), nameof(Farmer.Stamina)),
            prefix: new HarmonyMethod(typeof(StaminaPatches), nameof(SetStamina_Prefix)));
    }

    public static void SetStamina_Prefix(Farmer __instance, ref float value)
    {
        try
        {
            if (!Context.IsWorldReady || !__instance.IsLocalPlayer) return;

            float spend = __instance.stamina - value;
            if (spend <= 0f) return; // a gain, not an expenditure

            if (Perks.ConservationistFreeRoll())
            {
                value = __instance.stamina; // free action
                return;
            }

            float mult = Perks.EfficientCostMultiplier();
            if (mult < 1f)
                value = __instance.stamina - spend * mult;
        }
        catch (Exception ex)
        {
            _monitor.Log($"StaminaPatches.SetStamina_Prefix failed: {ex}", LogLevel.Error);
        }
    }
}
