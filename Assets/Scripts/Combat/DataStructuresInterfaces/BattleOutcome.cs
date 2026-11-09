namespace Frankie.Combat
{
    public enum BattleOutcome
    {
        Undetermined,
        Won,
        Lost,
        Ran,
        Aborted // Battle cut short without an outro ~ complete gets raised directly
    }
}
