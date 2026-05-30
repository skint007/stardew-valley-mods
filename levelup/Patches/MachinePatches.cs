using System;
using HarmonyLib;
using LevelUp.Systems;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Machines;
using SObject = StardewValley.Object;

namespace LevelUp.Patches;

/// <summary>
/// Harmony postfix on <see cref="StardewValley.Object.PlaceInMachine"/>. When the local
/// player successfully places an input into a machine, scales the machine's
/// <c>MinutesUntilReady</c> down by the accumulated <c>MachineSpeedBonus</c>. By design this
/// only affects newly-started machines, not ones already running (the bonus is applied at
/// placement time), which keeps the semantics predictable.
/// </summary>
public static class MachinePatches
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
        var target = AccessTools.Method(typeof(SObject), nameof(SObject.PlaceInMachine));
        if (target == null)
        {
            _monitor.Log("MachinePatches: couldn't resolve Object.PlaceInMachine; skipping.", LogLevel.Warn);
            return;
        }
        _monitor.Log($"MachinePatches: patched {target.DeclaringType?.Name}.{target.Name}", LogLevel.Info);
        harmony.Patch(target,
            postfix: new HarmonyMethod(typeof(MachinePatches), nameof(PlaceInMachine_Postfix)));
    }

    public static void PlaceInMachine_Postfix(
        SObject __instance,
        MachineData machineData,
        Item inputItem,
        bool probe,
        Farmer who,
        bool __result)
    {
        try
        {
            if (probe || !__result) return;
            if (who == null || who != Game1.player) return; // only the local player's own placements

            float bonus = _bonusApplier.CurrentMachineSpeedBonus;
            if (bonus <= 0f) return;
            if (__instance == null) return;

            int original = __instance.MinutesUntilReady;
            if (original <= 0) return;

            // Faster = less time. minutes / (1 + bonus). Clamp to >=1 so the machine never
            // finishes the same tick it starts.
            float factor = 1f / (1f + bonus);
            int scaled = Math.Max(1, (int)Math.Round(original * factor));
            if (scaled < original)
                __instance.MinutesUntilReady = scaled;
        }
        catch (Exception ex)
        {
            _monitor.Log($"MachinePatches.PlaceInMachine_Postfix failed: {ex}", LogLevel.Error);
        }
    }
}
