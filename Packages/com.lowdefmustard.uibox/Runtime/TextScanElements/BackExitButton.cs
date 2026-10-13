using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    [UxmlElement]
    public sealed partial class BackExitButton : Button
    {

        public BackExitButton()
        {
            focusable = false; // Keyboard/gamepad input handled via IInputReceiver - avoid UITK navigation double-firing
            AddToClassList(USSClassNames.BackExit.block); // Note:  Content (e.g. 'x' label, frame) composed as children in UXML
        }

        public void SetShown(bool enable) => EnableInClassList(USSClassNames.BackExit.hidden, !enable);
    }
}
