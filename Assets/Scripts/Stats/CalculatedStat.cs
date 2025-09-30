namespace Frankie.Stats
{
    // Note:  If updating, also update SkillStat enum
    public enum CalculatedStat
    {
        CooldownFraction,
        HitChance,
        CritChance,
        PhysicalAdder,
        MagicalAdder,
        Defense,
        RunSpeed, // in-battle speed
        RunChance, // contested chance based on speed
        Fearsome, // cause enemies to flee
        Imposing, // cause battles to auto-win
        MoveSpeed // in-world speed
    }
}
