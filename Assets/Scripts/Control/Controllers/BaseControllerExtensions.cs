using LowDefMustard.Control;
using Frankie.Core;

namespace Frankie.Control
{
    public static class BaseControllerExtensions
    {
        public static void StandardOnNoReceiversIdentified(this BaseController baseController)
        {
            // Attempt to find player and enter world to prevent lock-up
            PlayerStateMachine playerStateMachine = Player.FindPlayerStateMachine();
            if (playerStateMachine != null) { playerStateMachine.EnterWorld(); }
        }
    }
}
