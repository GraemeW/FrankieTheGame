using UnityEngine;

namespace LowDefMustard.Utils.Tests
{
    public class DummyEventSource : MonoBehaviour
    {
        public event System.Action onFired;
        public void Fire() => onFired?.Invoke();
    }

    // Models the OnEnable-subscribe / OnDisable-unsubscribe pattern w/ TryGetSafely's allowReInit parameter
    // OnEnable w/ allowReInit: true (default) - a normal subscribe wants the freshest reference
    // OnDisable w/ allowReInit: false - unsubscribing should never trigger a fresh lookup mid-teardown
    public class ReInitLazyValueSubscribeUnsubscribeExample : MonoBehaviour
    {
        public int initializerCallCount { get; private set; }
        public int eventReceivedCount { get; private set; }
        public bool onDisableRan { get; private set; }
        public ReInitLazyValue<DummyEventSource> source { get; private set; }

        private void Awake()
        {
            source = new ReInitLazyValue<DummyEventSource>(() =>
            {
                initializerCallCount++;
                return FindAnyObjectByType<DummyEventSource>();
            });
        }

        private void OnEnable()
        {
            if (source.TryGetSafely(out var found)) { found.onFired += HandleFired; }
        }

        private void OnDisable()
        {
            onDisableRan = true;
            if (source.TryGetSafely(out var found, allowReInit: false)) { found.onFired -= HandleFired; }
        }

        private void HandleFired() => eventReceivedCount++;
    }
}
