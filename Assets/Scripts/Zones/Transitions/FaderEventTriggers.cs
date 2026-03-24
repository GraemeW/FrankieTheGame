using System;

namespace Frankie.ZoneManagement
{
    public struct FaderEventTriggers
    {
        public readonly Action<TransitionType> onFadeIn;
        public readonly Action onFadePeak;
        public readonly Action onFadeOut;
        public readonly Action onFadeComplete;

        public FaderEventTriggers(Action<TransitionType> onFadeIn, Action onFadePeak, Action onFadeOut, Action onFadeComplete)
        {
            this.onFadeIn = onFadeIn;
            this.onFadePeak = onFadePeak;
            this.onFadeOut = onFadeOut;
            this.onFadeComplete = onFadeComplete;
        }
    }
}
