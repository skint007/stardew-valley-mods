using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using TrainUp.Systems;
using SObject = StardewValley.Object;

namespace TrainUp.Patches;

/// <summary>
/// Postfixes on <see cref="StardewValley.Object.healthRecoveredOnConsumption"/> and
/// <see cref="StardewValley.Object.staminaRecoveredOnConsumption"/> that boost how much HP/energy
/// food and drink restore for the local player (Medic / Caffeinated perks). These are the values
/// applied in <c>Farmer.doneEating</c>, so the perks affect any consumed item.
/// </summary>
public static class ConsumptionPatches
{
    private static IMonitor _monitor = null!;

    public static void Init(IMonitor monitor) => _monitor = monitor;

    public static void Apply(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(SObject), nameof(SObject.healthRecoveredOnConsumption)),
            postfix: new HarmonyMethod(typeof(ConsumptionPatches), nameof(Health_Postfix)));
        harmony.Patch(
            original: AccessTools.Method(typeof(SObject), nameof(SObject.staminaRecoveredOnConsumption)),
            postfix: new HarmonyMethod(typeof(ConsumptionPatches), nameof(Stamina_Postfix)));
    }

    public static void Health_Postfix(ref int __result)
    {
        try
        {
            if (__result <= 0) return;
            float mult = Perks.FoodHealthMultiplier();
            if (mult > 1f) __result = (int)(__result * mult);
        }
        catch (Exception ex) { _monitor.Log($"ConsumptionPatches.Health_Postfix failed: {ex}", LogLevel.Error); }
    }

    public static void Stamina_Postfix(ref int __result)
    {
        try
        {
            if (__result <= 0) return;
            float mult = Perks.FoodStaminaMultiplier();
            if (mult > 1f) __result = (int)(__result * mult);
        }
        catch (Exception ex) { _monitor.Log($"ConsumptionPatches.Stamina_Postfix failed: {ex}", LogLevel.Error); }
    }
}
