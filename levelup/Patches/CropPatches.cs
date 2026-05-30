using System;
using System.Linq;
using HarmonyLib;
using LevelUp.Config;
using LevelUp.Systems;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.TerrainFeatures;

namespace LevelUp.Patches;

/// <summary>
/// Harmony postfix on <see cref="Crop.harvest"/>. When the local player successfully harvests
/// a crop, rolls the accumulated <c>ExtraCropChance</c> milestone bonus and, if it hits,
/// hands them an extra copy of the harvested crop. Values above 1.0 grant guaranteed extras
/// plus a roll for one more (1.20 = always +1, plus 20% chance of +2). Junimo harvests are
/// skipped (the bonus is a player reward, and the Junimo bag has its own logic).
/// </summary>
public static class CropPatches
{
    private static ModConfig _config = null!;
    private static BonusApplier _bonusApplier = null!;
    private static IMonitor _monitor = null!;

    // Set by the prefix, read by the postfix: whether the crop was ripe when harvest was
    // called. Needed to detect a successful harvest on regrowable crops (tomatoes, blueberries,
    // summer squash, etc.), which call into Crop.harvest, drop their items, then return FALSE
    // because the plant survives — not because the harvest failed.
    [ThreadStatic]
    private static bool _wasRipeAtHarvest;

    public static void Init(ModConfig config, BonusApplier bonusApplier, IMonitor monitor)
    {
        _config = config;
        _bonusApplier = bonusApplier;
        _monitor = monitor;
    }

    public static void Apply(Harmony harmony)
    {
        // Match by name only (not signature) so a 1.6.x parameter tweak doesn't silently leave
        // the postfix unpatched. There's only one Crop.harvest in vanilla.
        var target = AccessTools.Method(typeof(Crop), nameof(Crop.harvest));
        if (target == null)
        {
            _monitor.Log("CropPatches: couldn't resolve Crop.harvest; bonus-crop milestone bonus will not fire.", LogLevel.Warn);
            return;
        }
        string sig = string.Join(", ", target.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
        _monitor.Log($"CropPatches: patched {target.DeclaringType?.Name}.{target.Name}({sig})", LogLevel.Info);
        harmony.Patch(target,
            prefix: new HarmonyMethod(typeof(CropPatches), nameof(Harvest_Prefix)),
            postfix: new HarmonyMethod(typeof(CropPatches), nameof(Harvest_Postfix)));
    }

    public static void Harvest_Prefix(Crop __instance)
    {
        try
        {
            _wasRipeAtHarvest = __instance != null
                && !__instance.dead.Value
                && __instance.phaseDays.Count > 0
                && __instance.currentPhase.Value >= __instance.phaseDays.Count - 1;
        }
        catch
        {
            _wasRipeAtHarvest = false;
        }
    }

    public static void Harvest_Postfix(
        Crop __instance,
        int xTile,
        int yTile,
        HoeDirt soil,
        JunimoHarvester junimoHarvester,
        bool __result)
    {
        try
        {
            if (junimoHarvester != null) return; // Junimo huts: don't pile bonus into the Junimo bag.
            // Regrowable crops (tomatoes, blueberries, summer squash, etc.) call into harvest,
            // drop their items, then return false because the plant survives. So the "did a
            // harvest actually happen" gate is the ripeness captured in the prefix, OR a true
            // return from the single-pick / forage paths.
            if (!__result && !_wasRipeAtHarvest) return;

            float chance = _bonusApplier.CurrentExtraCropChance;
            if (chance <= 0f) return;

            int extras = RollExtras(chance);
            if (extras <= 0) return;

            string itemId = __instance.indexOfHarvest.Value;
            if (string.IsNullOrEmpty(itemId)) return;

            var location = soil?.Location ?? Game1.currentLocation;
            if (location == null) return;

            // Drop each extra as its own visible debris that pops out of the harvest tile and
            // is auto-collected by proximity, matching vanilla harvest overflow. Adding silently
            // to inventory looked broken to playtesters even when it was working.
            var pos = new Vector2(xTile * 64f + 32f, yTile * 64f + 32f);
            for (int i = 0; i < extras; i++)
            {
                var single = ItemRegistry.Create(itemId, 1);
                if (single != null)
                    Game1.createItemDebris(single, pos, -1, location);
            }

            if (_config.DebugLogging)
                _monitor.Log($"+{extras} bonus crop ({itemId}) from harvest at ({xTile},{yTile})", LogLevel.Debug);
        }
        catch (Exception ex)
        {
            _monitor.Log($"CropPatches.Harvest_Postfix failed: {ex}", LogLevel.Error);
        }
    }

    /// <summary>
    /// Resolve a fractional bonus chance into a concrete extra count: whole part is guaranteed,
    /// fractional part is rolled. So 0.10 = 10% chance of +1; 1.20 = always +1, plus 20% chance
    /// of +2.
    /// </summary>
    private static int RollExtras(float chance)
    {
        int guaranteed = (int)Math.Floor(chance);
        float roll = chance - guaranteed;
        if (Game1.random.NextDouble() < roll) guaranteed++;
        return guaranteed;
    }
}
