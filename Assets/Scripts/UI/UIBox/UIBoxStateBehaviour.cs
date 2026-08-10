using System;
using LowDefMustard.Control;

namespace Frankie.Utils.UI
{
    public class UIBoxStateBehaviour
    {
        public Action setupChoiceOptions;
        public Action reconcileChoiceOptions;
        public Func<ControllerInputType, bool> prepareChooseAction;
        public Func<string, bool> choose;
        public Func<ControllerInputType, bool> handleGlobalInput;
        public Func<ControllerInputType, CursorMovementStyle, bool> moveCursor;
        public Func<ControllerInputType, bool> isBackInput;
        public Func<ControllerInputType, bool> tryHandleBackNavigation;

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
