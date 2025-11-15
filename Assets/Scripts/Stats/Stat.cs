namespace Frankie.Stats
{
    // Note:  If updating, also update SkillStat enum
    public enum Stat
    {
        HP,
        AP,
        ExperienceReward,
        ExperienceToLevelUp,
        Brawn, // Physical offense - Up Skill Primary
        Beauty, // Magic offense + healing - Down Skill Primary
        Smarts, // Mana on level - Right Skill Primary
        Nimble, // Physical defense
        Luck, // Chance to hit + dodge - Left Skill Primary
        Pluck, // Cooldown && Chance to crit + avoid crits
        Stoic, // HP on level
        InitialLevel
    }
}
