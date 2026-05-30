using System;
using HarmonyLib;
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
            original: AccessTools.Method(typeof(Crop), nameof(Crop.harvest),
                new[] { typeof(int), typeof(int), typeof(HoeDirt), typeof(JunimoHarvester), typeof(bool) }),
            postfix: new HarmonyMethod(typeof(CropPatches), nameof(Harvest_Postfix)));
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
            if (!__result) return;
            if (junimoHarvester != null) return; // Junimo huts: don't pile bonus into the Junimo bag.

            float chance = _bonusApplier.CurrentExtraCropChance;
            if (chance <= 0f) return;

            int extras = RollExtras(chance);
            if (extras <= 0) return;

            string itemId = __instance.indexOfHarvest.Value;
            if (string.IsNullOrEmpty(itemId)) return;

            var item = ItemRegistry.Create(itemId, extras);
            if (item == null) return;

            var player = Game1.player;
            if (player == null) return;

            // Try inventory first; if it doesn't all fit, drop the remainder at the tile.
            Item? leftover = player.addItemToInventory(item);
            if (leftover != null && leftover.Stack > 0)
            {
                var location = soil?.Location ?? Game1.currentLocation;
                if (location != null)
                {
                    var pos = new Vector2(xTile * 64f + 32f, yTile * 64f + 32f);
                    Game1.createItemDebris(leftover, pos, -1, location);
                }
            }
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
