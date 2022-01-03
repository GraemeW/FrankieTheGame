namespace Frankie.Stats
{
    // Note:  If updating, also update SkillStat enum
    public enum Stat
    {
        HP,
        AP,
        ExperienceReward,
        ExperienceToLevelUp,
        Brawn, // Physical offense
        Beauty, // Magic offense + healing
        Smarts, // Mana on level
        Nimble, // Physical defense
        Luck, // Chance to hit + dodge
        Pluck, // Cooldown && Chance to crit + avoid crits
        Stoic // HP on level
    }
}
