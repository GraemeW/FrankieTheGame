using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    public abstract class SplashStageElement : VisualElement
    {
        // Note:
        //  - Shown while the model's stage matches its own - the fade in / out is a USS opacity transition on .splash-stage that is timed by the model
        //  - One typed element per stage (e.g. SplashStudioStage)

        // Const
        private const string _ussClassName = "splash-stage";
        private const string _shownUssClassName = "splash-stage--shown";

        // State
        private SplashStage internalShownStage = SplashStage.None;
        private float internalFadeSeconds;

        // Cached References
        private readonly SplashStage stage;

        [CreateProperty] public SplashStage shownStage
        {
            get => internalShownStage;
            set
            {
                internalShownStage = value;
                schedule.Execute(ApplyShown); // Deferred a tick ~ a class set before the element's first style pass would skip the transition
            }
        }

        [CreateProperty] public float fadeSeconds
        {
            get => internalFadeSeconds;
            set
            {
                internalFadeSeconds = value;
                style.transitionDuration = new List<TimeValue> { new(value, TimeUnit.Second) };
            }
        }

        protected SplashStageElement(SplashStage stage)
        {
            this.stage = stage;
            AddToClassList(_ussClassName);
            pickingMode = PickingMode.Ignore;
            SetBinding(nameof(shownStage), UIToolkitBindings.ToTarget(nameof(SplashModel.shownStage)));
            SetBinding(nameof(fadeSeconds), UIToolkitBindings.ToTarget(nameof(SplashModel.stageFadeSeconds)));
        }

        private void ApplyShown() => EnableInClassList(_shownUssClassName, stage == internalShownStage);
    }
}
