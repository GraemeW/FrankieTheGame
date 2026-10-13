using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.Utils.UI
{
    [UxmlElement]
    public sealed partial class FlashingElement : VisualElement
    {
        // Note:  Fades between shown and hidden, where flash rate is a USS custom property set on the element itself
        //   --flash-period:  seconds for one full flash (out and back in)

        // Const
        private const float _defaultFlashPeriod = 1f;
        private static readonly CustomStyleProperty<float> _flashPeriodProperty = new("--flash-period");

        // State
        private float flashPeriod = _defaultFlashPeriod;
        private bool isDimmed;
        private IVisualElementScheduledItem flashSchedule;

        public FlashingElement()
        {
            pickingMode = PickingMode.Ignore;
            style.transitionProperty = new List<StylePropertyName> { new("opacity") };
            style.transitionTimingFunction = new List<EasingFunction> { new(EasingMode.EaseInOutSine) };
            RegisterCallback<AttachToPanelEvent>(_ => RestartFlash());
            RegisterCallback<DetachFromPanelEvent>(_ => StopFlash());
            RegisterCallback<CustomStyleResolvedEvent>(HandleCustomStyleResolved);
        }

        #region PrivateMethods
        private void HandleCustomStyleResolved(CustomStyleResolvedEvent customStyleResolvedEvent)
        {
            float resolvedFlashPeriod = customStyleResolvedEvent.customStyle.TryGetValue(_flashPeriodProperty, out float period) && period > 0f ? period : _defaultFlashPeriod;
            if (Mathf.Approximately(resolvedFlashPeriod, flashPeriod) && flashSchedule != null) { return; }

            flashPeriod = resolvedFlashPeriod;
            RestartFlash();
        }

        private void RestartFlash()
        {
            StopFlash();
            if (panel == null || !Application.isPlaying) { return; } // Edit-time (UI Builder) previews stay shown

            float halfPeriod = flashPeriod / 2f;
            style.transitionDuration = new List<TimeValue> { new(halfPeriod, TimeUnit.Second) };
            flashSchedule = schedule.Execute(ToggleDimmed).Every(Mathf.RoundToInt(halfPeriod * 1000f));
        }

        private void StopFlash()
        {
            flashSchedule?.Pause();
            flashSchedule = null;
        }

        private void ToggleDimmed()
        {
            isDimmed = !isDimmed;
            style.opacity = isDimmed ? 0f : 1f;
        }
        #endregion
    }
}
