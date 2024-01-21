namespace Frankie.Combat
{
    public enum StateAlteredType
    {
        DecreaseHP,
        IncreaseHP,
        AdjustHPNonSpecific,
        IncreaseAP,
        DecreaseAP,
        AdjustAPNonSpecific,
        Dead,
        Resurrected,
        StatusEffectApplied,
        BaseStateEffectApplied,
        CooldownSet,
        CooldownExpired,
        HitMiss,
        HitCrit,
        FriendFound,
        FriendIgnored
    }
}