using System;
using SpaceCore;
using StardewValley;

namespace TrainUp.Skills;

/// <summary>
/// A SpaceCore profession whose name/description come from translation lookups.
/// Mirrors spacechase0's own GenericProfession pattern so our professions render on the
/// vanilla level-up chooser exactly like vanilla ones.
/// </summary>
public class GenericProfession : SpaceCore.Skills.Skill.Profession
{
    private readonly Func<string> _getName;
    private readonly Func<string> _getDescription;

    public GenericProfession(SpaceCore.Skills.Skill skill, string id, Func<string> name, Func<string> description)
        : base(skill, id)
    {
        _getName = name;
        _getDescription = description;
    }

    public override string GetName() => _getName();

    public override string GetDescription() => _getDescription();

    /// <summary>True if the local player has chosen this profession.</summary>
    public bool IsActiveFor(Farmer farmer) => farmer.professions.Contains(this.GetVanillaId());
}
