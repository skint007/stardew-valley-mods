using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;
using TrainUp.Skills;

namespace TrainUp.Systems;

/// <summary>SMAPI console commands for inspecting and testing Train Up.</summary>
public class ConsoleCommands
{
    private readonly SkillRegistry _skills;
    private readonly IMonitor _monitor;

    public ConsoleCommands(SkillRegistry skills, IMonitor monitor)
    {
        _skills = skills;
        _monitor = monitor;
    }

    public void Register(ICommandHelper commands)
    {
        commands.Add("trainup_skills",
            "Show your current Train Up skill levels and XP.",
            (_, _) => ShowSkills());

        commands.Add("trainup_addxp",
            "Usage: trainup_addxp <defense|vitality|stamina> <amount>\nAward XP to a Train Up skill.",
            (_, args) => AddXp(args));

        commands.Add("trainup_dummy",
            "Add a Training Dummy big craftable to your inventory.",
            (_, _) => GiveDummy());
    }

    private bool RequireWorld()
    {
        if (Context.IsWorldReady) return true;
        _monitor.Log("Load a save first.", LogLevel.Error);
        return false;
    }

    private static readonly Dictionary<string, string> Ids = new(StringComparer.OrdinalIgnoreCase)
    {
        ["defense"] = DefenseSkill.SkillId,
        ["vitality"] = VitalitySkill.SkillId,
        ["stamina"] = StaminaSkill.SkillId,
    };

    private void ShowSkills()
    {
        if (!RequireWorld()) return;
        foreach (var (label, id) in Ids)
        {
            int level = SpaceCore.Skills.GetSkillLevel(Game1.player, id);
            int xp = SpaceCore.Skills.GetExperienceFor(Game1.player, id);
            _monitor.Log($"{label,-9} level {level,2}  ({xp} XP)", LogLevel.Info);
        }
    }

    private void AddXp(string[] args)
    {
        if (!RequireWorld()) return;
        if (args.Length < 2 || !Ids.TryGetValue(args[0], out string? id) || !int.TryParse(args[1], out int amount))
        {
            _monitor.Log("Usage: trainup_addxp <defense|vitality|stamina> <amount>", LogLevel.Error);
            return;
        }
        _skills.AddXp(id, amount);
        _monitor.Log($"Added {amount} XP to {args[0]}.", LogLevel.Info);
        ShowSkills();
    }

    private void GiveDummy()
    {
        if (!RequireWorld()) return;
        var item = ItemRegistry.Create(DummyContent.QualifiedId);
        if (Game1.player.addItemToInventoryBool(item))
            _monitor.Log("Added a Training Dummy to your inventory.", LogLevel.Info);
        else
            _monitor.Log("Inventory full — couldn't add the Training Dummy.", LogLevel.Warn);
    }
}
