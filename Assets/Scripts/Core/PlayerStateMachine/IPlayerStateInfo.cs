using Frankie.Combat;

namespace Frankie.Core
{
    public interface IPlayerStateInfo
    {
        bool CanMoveInCutscene();
        bool InZoneTransition();
        bool InBattleExitTransition();
        bool IsAnyPartyMemberAlive();
        bool IsPlayerFearsome(CombatParticipant combatParticipant);
    }
}
