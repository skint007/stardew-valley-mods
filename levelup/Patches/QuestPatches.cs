using System;
using HarmonyLib;
using LevelUp.Config;
using LevelUp.State;
using LevelUp.Systems;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Quests;

namespace LevelUp.Patches;

/// <summary>
/// Harmony postfix on <see cref="Quest.questComplete()"/>. Awards XP based on whether the quest
/// was a daily "Help Wanted" billboard quest or a story quest.
/// </summary>
public static class QuestPatches
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
            original: AccessTools.Method(typeof(Quest), nameof(Quest.questComplete)),
            prefix: new HarmonyMethod(typeof(QuestPatches), nameof(QuestComplete_Prefix)),
            postfix: new HarmonyMethod(typeof(QuestPatches), nameof(QuestComplete_Postfix)));
    }

    /// <summary>Capture completion state before the call so we only award on the transition.</summary>
    public static void QuestComplete_Prefix(Quest __instance, out bool __state)
    {
        __state = __instance?.completed.Value ?? true;
    }

    public static void QuestComplete_Postfix(Quest __instance, bool __state)
    {
        try
        {
            if (!_config.Enabled || !_config.XpSources.QuestEnabled) return;
            if (__instance == null) return;
            // Quests are per-player; this runs on the machine completing the quest, so the
            // local player is correctly credited (no IsMainPlayer gate — farmhands too).
            // __state == true means it was already completed (questComplete no-ops on repeat);
            // only award on the false → true transition.
            if (__state || !__instance.completed.Value) return;

            // Billboard "Help Wanted" quests have dailyQuest=true; story quests do not.
            bool isHelpWanted = __instance.dailyQuest.Value;
            int xp = isHelpWanted ? _config.XpSources.HelpWantedQuestXp : _config.XpSources.StoryQuestXp;
            if (xp <= 0) return;

            int oldLevel = _saveData.Current.Level;
            bool leveled = _xpTracker.AwardXp(xp, isHelpWanted ? "quest:helpwanted" : "quest:story");
            if (leveled)
            {
                _notifier.NotifyLevelUp(oldLevel, _saveData.Current.Level);
                _bonusApplier.ApplyAll();
            }
        }
        catch (Exception ex)
        {
            _monitor.Log($"QuestPatches.QuestComplete_Postfix failed: {ex}", LogLevel.Error);
        }
    }
}
