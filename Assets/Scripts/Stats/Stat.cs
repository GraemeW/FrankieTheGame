namespace Frankie.Stats
{
    // Note:  If updating, also update SkillStat enum
    // See CalculatedStats for full usage
    public enum Stat
    {
        HP,
        AP,
        ExperienceReward,
        ExperienceToLevelUp,
        Brawn, // Physical offense - Up Skill Primary
        Beauty, // Magic offense + healing - Down Skill Primary
        Smarts, // AP on level, TBD - Right Skill Primary
        Nimble, // Cooldown, chance to run, in-world move speed
        Luck, // Chance to hit/dodge - Left Skill Primary
        Pluck, // Chance to crit, chance to auto-win, chance to survive
        Stoic, // Defense, HP on level
        InitialLevel
    }
}
