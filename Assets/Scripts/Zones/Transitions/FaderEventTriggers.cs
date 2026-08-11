using System;

namespace Frankie.ZoneManagement
{
    public struct FaderEventTriggers<T> where T : struct, Enum
    {
        public readonly Action<T> onFadeIn;
        public readonly Action onFadePeak;
        public readonly Action onFadeOut;
        public readonly Action onFadeComplete;

        public FaderEventTriggers(Action<T> onFadeIn, Action onFadePeak, Action onFadeOut, Action onFadeComplete)
        {
            this.onFadeIn = onFadeIn;
            this.onFadePeak = onFadePeak;
            this.onFadeOut = onFadeOut;
            this.onFadeComplete = onFadeComplete;
        }
    }
}
