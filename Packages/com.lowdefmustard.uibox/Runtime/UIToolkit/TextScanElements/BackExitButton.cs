using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    [UxmlElement]
    public sealed partial class BackExitButton : Button
    {
        // Const Tunables
        private const string _ussClassName = "uibox-back-exit";
        private const string _hiddenUssClassName = _ussClassName + "--hidden";

        public BackExitButton()
        {
            focusable = false; // Keyboard/gamepad input handled via IInputReceiver - avoid UITK navigation double-firing
            AddToClassList(_ussClassName); // Note:  Content (e.g. 'x' label, frame) composed as children in UXML
        }

        public void SetShown(bool enable) => EnableInClassList(_hiddenUssClassName, !enable);
    }
}
