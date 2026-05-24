using System;
using HarmonyLib;
using LevelUp.Config;
using LevelUp.State;
using LevelUp.Systems;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace LevelUp.Patches;

/// <summary>
/// Awards player XP on monster kills.
///
/// We patch <c>GameLocation.onMonsterKilled</c> (the private method <c>damageMonster</c> calls
/// once a monster reaches 0 HP) rather than <c>Monster.takeDamage</c>: many monsters
/// (GreenSlime, Bat, Bug, RockCrab, …) <b>override</b> takeDamage and never call the base,
/// so a base-method patch silently misses them. onMonsterKilled is the single choke point
/// for every player-attributed kill — melee, slingshot, and bomb — regardless of subclass.
/// </summary>
public static class MonsterPatches
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
            original: AccessTools.Method(typeof(GameLocation), "onMonsterKilled",
                new[] { typeof(Farmer), typeof(Monster), typeof(Microsoft.Xna.Framework.Rectangle), typeof(bool) }),
            postfix: new HarmonyMethod(typeof(MonsterPatches), nameof(OnMonsterKilled_Postfix)));
    }

    public static void OnMonsterKilled_Postfix(Farmer who, Monster monster)
    {
        try
        {
            if (!_config.Enabled || !_config.XpSources.MonsterKillEnabled) return;
            if (monster == null) return;
            // Multiplayer: this postfix runs on each machine. Count a kill only if the local
            // player landed it — on the killer's machine `who == Game1.player`, on every other
            // machine it isn't, so each kill is credited exactly once to the right player.
            if (who == null || who != Game1.player) return;

            int maxHp = Math.Max(1, monster.MaxHealth);
            double xp = maxHp * _config.XpSources.MonsterXpPerMaxHp;
            if (IsBoss(monster))
                xp *= _config.XpSources.BossKillMultiplier;

            long amount = (long)Math.Round(xp);
            if (amount <= 0) return;

            int oldLevel = _saveData.Current.Level;
            bool leveled = _xpTracker.AwardXp(amount, $"kill:{monster.Name}");
            if (leveled)
            {
                _notifier.NotifyLevelUp(oldLevel, _saveData.Current.Level);
                _bonusApplier.ApplyAll();
            }
        }
        catch (Exception ex)
        {
            _monitor.Log($"MonsterPatches.OnMonsterKilled_Postfix failed: {ex}", LogLevel.Error);
        }
    }

    private static bool IsBoss(Monster monster)
    {
        // Vanilla has no general "isBoss" flag; dangerous-mine / hard-mode monsters set
        // isHardModeMonster, and true bosses (Dwarf King, etc.) have very high max HP.
        try
        {
            if (monster.isHardModeMonster.Value) return true;
        }
        catch { /* fall through */ }

        return monster.MaxHealth >= 500;
    }
}
