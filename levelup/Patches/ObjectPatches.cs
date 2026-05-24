using System;
using HarmonyLib;
using LevelUp.Systems;
using StardewModdingAPI;
using SObject = StardewValley.Object;

namespace LevelUp.Patches;

/// <summary>
/// Harmony postfix on <see cref="StardewValley.Object.sellToStorePrice"/>. Multiplies the result
/// by the player's accumulated SellPriceBonus from milestones.
/// </summary>
public static class ObjectPatches
{
    private static BonusApplier _bonusApplier = null!;
    private static IMonitor _monitor = null!;

    public static void Init(BonusApplier bonusApplier, IMonitor monitor)
    {
        _bonusApplier = bonusApplier;
        _monitor = monitor;
    }

    public static void Apply(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(SObject), nameof(SObject.sellToStorePrice), new[] { typeof(long) }),
            postfix: new HarmonyMethod(typeof(ObjectPatches), nameof(SellToStorePrice_Postfix)));
    }

    public static void SellToStorePrice_Postfix(SObject __instance, ref int __result)
    {
        try
        {
            float bonus = _bonusApplier.CurrentSellPriceBonus;
            if (bonus <= 0f) return;
            __result = (int)Math.Round(__result * (1.0 + bonus));
        }
        catch (Exception ex)
        {
            _monitor.Log($"ObjectPatches.SellToStorePrice_Postfix failed: {ex}", LogLevel.Error);
        }
    }
}
