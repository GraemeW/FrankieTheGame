using System;
using Frankie.Control;

namespace Frankie.Utils.UI
{
    public class UIBoxStateBehaviour
    {
        public readonly Func<ControllerInputType, CursorMovementStyle, bool> moveCursor;
        public readonly Func<string, bool> choose;
        public readonly Func<ControllerInputType, bool> tryHandleBackNavigation;

        public UIBoxStateBehaviour(Func<ControllerInputType, CursorMovementStyle, bool> moveCursor = null, Func<string, bool> choose = null, Func<ControllerInputType, bool> tryHandleBackNavigation = null)
        {
            this.moveCursor = moveCursor;
            this.choose = choose;
            this.tryHandleBackNavigation = tryHandleBackNavigation;
        }
    }
}
