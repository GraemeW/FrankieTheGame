using System;
using Frankie.Core.PlayerStates;

namespace Frankie.Core.PlayerStateMemory
{
    public class OptionMemory
    {
        public OptionStateType optionStateType = OptionStateType.None;

        public bool InitiateOptions(Action instantiateWorldOptions, Action instantiateEscapeMenu)
        {
            switch (optionStateType)
            {
                case OptionStateType.WorldOptions:
                    if (instantiateWorldOptions == null) { return false; }
                    instantiateWorldOptions.Invoke();
                    break;
                case OptionStateType.EscapeMenu:
                    if (instantiateEscapeMenu == null) { return false; }
                    instantiateEscapeMenu.Invoke();
                    break;
                case OptionStateType.None:
                default:
                    return false;
            }
            return true;
        }
    }
}
