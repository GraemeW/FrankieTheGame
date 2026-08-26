using UnityEngine;

namespace LowDefMustard.Utils.Tests.PlayMode
{
    // Case A: LazyValue is defined in Awake(), and - also in Awake() - immediately overwritten with a "loaded from save" value
    // This is the pattern for values that come from a save system: the initializer exists as a fallback default, but in the normal flow it should never actually run
    public class LazyValueCaseAExample : MonoBehaviour
    {
        // Tunables
        public int initializerCallCount { get; private set; }
        public LazyValue<int> score { get; private set; }

        // Exposed so tests can configure Awake-time behaviour before activation.
        public int savedValueToApply = 42;
        public bool applySavedValueInAwake = true;

        private void Awake()
        {
            score = new LazyValue<int>(() =>
            {
                initializerCallCount++;
                return -1; // deliberately implausible, so tests can tell if this ever ran
            });

            if (applySavedValueInAwake)
            {
                score.value = savedValueToApply;
            }
        }
    }
    
    // Case B: LazyValue is defined in Awake() but deliberately NOT accessed there
    // In Start(), ForceInit() is called explicitly, using a [SerializeField] default:
    // - Awake() runs synchronously for every object before any Start() runs
    // - Calling ForceInit() in Start() guarantees this value is ready at a known point in the frame, relative to every other object's Start()
    public class LazyValueCaseBExample : MonoBehaviour
    {
        // Tunables
        public int initializerCallCount { get; private set; }
        public LazyValue<int> score { get; private set; }
        [SerializeField] private int defaultScoreValue = 10;

        // Exposed purely so tests can override the serialized default before Awake
        public void SetDefaultScoreValueForTest(int value) => defaultScoreValue = value;

        private void Awake()
        {
            score = new LazyValue<int>(() =>
            {
                initializerCallCount++;
                return defaultScoreValue;
            });
        }

        private void Start()
        {
            score.ForceInit();
        }
    }
}
