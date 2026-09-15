using LowDefMustard.Control;
using NUnit.Framework;

namespace LowDefMustard.UIBox.Tests.Editor
{
    public class UIBoxStateBehaviourTests
    {
        [Test]
        public void Constructor_NoArgs_AllDelegatesNull()
        {
            var behaviour = new UIBoxStateBehaviour();

            Assert.IsNull(behaviour.setupChoiceOptions);
            Assert.IsNull(behaviour.reconcileChoiceOptions);
            Assert.IsNull(behaviour.prepareChooseAction);
            Assert.IsNull(behaviour.choose);
            Assert.IsNull(behaviour.handleGlobalInput);
            Assert.IsNull(behaviour.moveCursor);
            Assert.IsNull(behaviour.isBackInput);
            Assert.IsNull(behaviour.tryHandleBackNavigation);
        }

        [Test]
        public void Constructor_AllArgsSupplied_EachLandsOnItsOwnField()
        {
            var setupCalled = false;
            var reconcileCalled = false;

            var behaviour = new UIBoxStateBehaviour(
                setupChoiceOptions: () => setupCalled = true,
                reconcileChoiceOptions: () => reconcileCalled = true,
                prepareChooseAction: _ => true,
                choose: _ => true,
                handleGlobalInput: _ => true,
                moveCursor: (_, _) => true,
                isBackInput: _ => true,
                tryHandleBackNavigation: _ => true);

            behaviour.setupChoiceOptions();
            behaviour.reconcileChoiceOptions();

            Assert.IsTrue(setupCalled);
            Assert.IsTrue(reconcileCalled);
            Assert.IsTrue(behaviour.prepareChooseAction(ControllerInputType.Execute));
            Assert.IsTrue(behaviour.choose(null));
            Assert.IsTrue(behaviour.handleGlobalInput(ControllerInputType.Execute));
            Assert.IsTrue(behaviour.moveCursor(ControllerInputType.NavigateRight, CursorMovementStyle.Combined));
            Assert.IsTrue(behaviour.isBackInput(ControllerInputType.Cancel));
            Assert.IsTrue(behaviour.tryHandleBackNavigation(ControllerInputType.Cancel));
        }
    }
}
