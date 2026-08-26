using UnityEngine;

namespace LowDefMustard.Utils.Tests.PlayMode
{
    public class DummyFindableTarget : MonoBehaviour
    {
    }

    // Models the "cache a reference to a Unity object, but re-fetch it if the object gets destroyed/recreated" pattern ReInitLazyValue is meant for
    // e.g. re-acquiring a pooled object's Transform after it's despawned and respawned
    public class ReInitLazyValueCachedReferenceExample : MonoBehaviour
    {
        public int initializerCallCount { get; private set; }
        public ReInitLazyValue<Transform> cachedTarget { get; private set; }

        private void Awake()
        {
            cachedTarget = new ReInitLazyValue<Transform>(() =>
            {
                initializerCallCount++;
                var found = FindAnyObjectByType<DummyFindableTarget>();
                return found != null ? found.transform : null;
            });
        }
    }
}
