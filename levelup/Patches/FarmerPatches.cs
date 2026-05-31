using System;
using HarmonyLib;
using LevelUp.Config;
using LevelUp.State;
using LevelUp.Systems;
using StardewModdingAPI;
using StardewValley;

namespace LevelUp.Patches;

/// <summary>
/// Harmony prefix on <see cref="Farmer.gainExperience(int, int)"/>. Does two things for the
/// local player:
///   1. Inflates the incoming vanilla skill XP by the milestone XP-gain multiplier (mutating
///      the input rather than re-calling gainExperience, so there's no re-entry).
///   2. Awards meta XP as a fraction of the *original* vanilla skill XP (the "scale with
///      skill XP" task source). Basing it on the original amount keeps the XP-gain
///      milestone from compounding into the meta award.
/// </summary>
public static class FarmerPatches
{
    private static ModConfig _config = null!;
    private static SaveDataManager _saveData = null!;
    private static XpTracker _xpTracker = null!;
    private static LevelUpNotifier _notifier = null!;
    private static BonusApplier _bonusApplier = null!;
    private static IMonitor _monitor = null!;

    public static void Init(
        ModConfig config,
        SaveDataManager saveData,
        XpTracker xpTracker,
        LevelUpNotifier notifier,
        BonusApplier bonusApplier,
        IMonitor monitor)
    {
        _config = config;
        _saveData = saveData;
        _xpTracker = xpTracker;
        _notifier = notifier;
        _bonusApplier = bonusApplier;
        _monitor = monitor;
    }

    public static void Apply(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(Farmer), nameof(Farmer.gainExperience),
                new[] { typeof(int), typeof(int) }),
            prefix: new HarmonyMethod(typeof(FarmerPatches), nameof(GainExperience_Prefix)));
    }

    public static void GainExperience_Prefix(Farmer __instance, int which, ref int howMuch)
    {
        try
        {
            if (!_config.Enabled) return;
            if (__instance != Game1.player) return;
            if (howMuch <= 0) return;

            int original = howMuch;

            // Surface suspiciously large single grants regardless of DebugLogging. Vanilla
            // skill actions cap out around a few hundred XP; anything in the thousands is
            // almost certainly another mod bulk-granting skill XP, and at SkillXpRate >= 1.0
            // that maps straight into meta XP and can rocket the player several levels at
            // once. Skill index in the source label helps identify the culprit (0=farming,
            // 1=fishing, 2=foraging, 3=mining, 4=combat, 5+ = SpaceCore custom skills).
            if (original >= 1000)
                _monitor.Log(
                    $"Large skill-XP grant: skill={which}, amount={original}. Another mod is " +
                    $"bulk-granting skill XP; mapped to meta via SkillXpRate={_config.XpSources.SkillXpRate:F2}.",
                    LogLevel.Warn);

            // 1. Milestone XP-gain multiplier (inflate the vanilla skill XP).
            float mult = _bonusApplier.CurrentXpMultiplier;
            if (mult > 0f)
            {
                int bonus = (int)Math.Round(original * mult);
                if (bonus > 0) howMuch += bonus;
            }

            // 2. "Scale with skill XP" meta XP, based on the original vanilla amount.
            var src = _config.XpSources;
            if (src.SkillXpEnabled && src.SkillXpRate > 0f)
            {
                long meta = (long)Math.Floor(original * src.SkillXpRate);
                if (meta > 0)
                {
                    int oldLevel = _saveData.Current.Level;
                    if (_xpTracker.AwardXp(meta, $"skill-xp:{which}"))
                    {
                        _notifier.NotifyLevelUp(oldLevel, _saveData.Current.Level);
                        _bonusApplier.ApplyAll();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _monitor.Log($"FarmerPatches.GainExperience_Prefix failed: {ex}", LogLevel.Error);
        }
    }
}
