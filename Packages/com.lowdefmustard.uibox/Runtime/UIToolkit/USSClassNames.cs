namespace LowDefMustard.UIBox
{
    // Single source of truth for the package's USS class names (BEM:  block, block--modifier, block__element)
    // Note:  USS files repeat these names - rename in both places
    public static class USSClassNames
    {
        public static class BoxView
        {
            public const string block = "uibox-view";
            public const string pointerInputDisabled = block + "--pointer-input-disabled";
        }

        public static class BackExit
        {
            public const string block = "uibox-back-exit";
            public const string hidden = block + "--hidden";
        }

        public static class TextEntries
        {
            public const string block = "text-entries";
        }

        public static class TextEntry
        {
            public const string block = "text-entry";
            public const string speech = block + "--speech";
            public const string hidden = block + "--hidden";
            public const string bullet = block + "__bullet";
            public const string label = block + "__label";
        }

        public static class ChoiceEntries
        {
            public const string block = "choice-entries";
            public const string horizontal = block + "--horizontal";
            public const string vertical = block + "--vertical";
            public const string empty = block + "--empty";
        }

        // Shared by every choice row (text choices, save slots, sliders, toggles):  marker visibility + hover tint
        public static class ChoiceEntry
        {
            public const string block = "choice-entry";
            public const string highlighted = block + "--highlighted";
            public const string hidden = block + "--hidden";
            public const string marker = block + "__marker";
            public const string label = block + "__label";
        }

        public static class ChoiceSeparator
        {
            public const string block = "choice-separator";
            public const string minor = block + "--minor";
        }

        public static class ChoiceGroup
        {
            public const string block = "choice-group";
        }

        public static class SliderChoice
        {
            public const string block = "slider-choice";
            public const string slider = block + "__slider";
        }

        public static class ToggleChoice
        {
            public const string block = "toggle-choice";
            public const string on = block + "--on";
            public const string box = block + "__box";
            public const string check = block + "__check";
        }

        public static class ModelBoundLabel
        {
            public const string block = "model-bound-label";
            public const string empty = block + "--empty";
        }
    }
}
