namespace Frankie.Stats
{
    // Note:  If updating, also update SkillStat enum
    // Non-modifying stats defined under _nonModifyingStats in BaseStats
    // See CalculatedStats for full usage
    public enum Stat
    {
        HP,
        AP,
        ExperienceReward,
        ExperienceToLevelUp,
        Brawn, // Physical offence - Up Skill Primary
        Beauty, // Magic offence + healing - Down Skill Primary
        Smarts, // AP on level, TBD - Right Skill Primary
        Nimble, // Cooldown, chance to run, in-world move speed
        Luck, // Chance to hit/dodge - Left Skill Primary
        Pluck, // Chance to crit, chance to auto-win, chance to survive
        Stoic, // Defence, HP on level
        InitialLevel
    }
}
