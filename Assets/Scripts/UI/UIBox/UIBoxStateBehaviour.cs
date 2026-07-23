using System;
using Frankie.Control;

namespace Frankie.Utils.UI
{
    public class UIBoxStateBehaviour
    {
        public readonly Action setupChoiceOptions;
        public readonly Action reconcileChoiceOptions;
        public readonly Func<ControllerInputType, bool> prepareChooseAction;
        public readonly Func<string, bool> choose;
        public readonly Func<ControllerInputType, bool> handleGlobalInput;
        public readonly Func<ControllerInputType, CursorMovementStyle, bool> moveCursor;
        public readonly Func<ControllerInputType, bool> isBackInput;
        public readonly Func<ControllerInputType, bool> tryHandleBackNavigation;

        public UIBoxStateBehaviour(
            Action setupChoiceOptions = null,
            Action reconcileChoiceOptions = null,
            Func<ControllerInputType, bool> prepareChooseAction = null,
            Func<string, bool> choose = null, 
            Func<ControllerInputType, bool> handleGlobalInput = null,
            Func<ControllerInputType, CursorMovementStyle, bool> moveCursor = null, 
            Func<ControllerInputType, bool> isBackInput = null,
            Func<ControllerInputType, bool> tryHandleBackNavigation = null)
        {
            this.setupChoiceOptions = setupChoiceOptions;
            this.reconcileChoiceOptions = reconcileChoiceOptions;
            this.prepareChooseAction = prepareChooseAction;
            this.choose = choose;
            this.handleGlobalInput = handleGlobalInput;
            this.moveCursor = moveCursor;
            this.isBackInput = isBackInput;
            this.tryHandleBackNavigation = tryHandleBackNavigation;
        }
    }
}
