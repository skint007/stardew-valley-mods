using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;

namespace TrainUp.Skills;

/// <summary>
/// Shared base for Train Up's custom skills. Handles icon loading, the (vanilla-matching)
/// experience curve, the bar color, and building the standard 2-at-L5 / 4-at-L10 profession
/// tree so each concrete skill only has to declare its professions and translation keys.
/// </summary>
public abstract class TrainUpSkill : SpaceCore.Skills.Skill
{
    /// <summary>The vanilla skill XP curve, reused so our bars/levels feel native.</summary>
    public static readonly int[] VanillaCurve = { 100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000 };

    /// <summary>Translation-key prefix for this skill (e.g. "defense"), used by i18n lookups.</summary>
    protected string Key { get; }

    protected TrainUpSkill(string id, string i18nKey, string icon16, string icon10, Color barColor)
        : base(id)
    {
        this.Key = i18nKey;
        this.Icon = ModEntry.Instance.Helper.ModContent.Load<Texture2D>(icon16);
        this.SkillsPageIcon = ModEntry.Instance.Helper.ModContent.Load<Texture2D>(icon10);
        this.ExperienceCurve = VanillaCurve;
        this.ExperienceBarColor = barColor;
    }

    /// <summary>Translation helper scoped to this skill: looks up "skill.&lt;key&gt;.&lt;suffix&gt;".</summary>
    protected string T(string suffix) => ModEntry.Instance.Helper.Translation.Get($"skill.{Key}.{suffix}");

    public override string GetName() => T("name");

    public override string GetSkillPageHoverText(int level) => T("description");

    /// <summary>
    /// Build a GenericProfession whose name/description read from
    /// "skill.&lt;key&gt;.profession.&lt;id&gt;.name" / ".desc".
    /// </summary>
    protected GenericProfession Prof(string id) => new(
        skill: this,
        id: id,
        name: () => T($"profession.{id.ToLowerInvariant()}.name"),
        description: () => T($"profession.{id.ToLowerInvariant()}.desc"));

    /// <summary>
    /// Register the standard tree: one L5 choice (a/b) and a distinct L10 pair gated behind
    /// each L5 pick. Adds every profession to <see cref="Skills.Skill.Professions"/> and the
    /// pairs to <see cref="Skills.Skill.ProfessionsForLevels"/>.
    /// </summary>
    protected void BuildTree(
        GenericProfession l5a, GenericProfession l5b,
        GenericProfession a1, GenericProfession a2,
        GenericProfession b1, GenericProfession b2)
    {
        this.Professions.Add(l5a);
        this.Professions.Add(l5b);
        this.ProfessionsForLevels.Add(new ProfessionPair(5, l5a, l5b));

        this.Professions.Add(a1);
        this.Professions.Add(a2);
        this.ProfessionsForLevels.Add(new ProfessionPair(10, a1, a2, l5a));

        this.Professions.Add(b1);
        this.Professions.Add(b2);
        this.ProfessionsForLevels.Add(new ProfessionPair(10, b1, b2, l5b));
    }

    /// <summary>Shown as the per-level perk line on the skills page / level-up menu.</summary>
    public override List<string> GetExtraLevelUpInfo(int level) => new();
}
