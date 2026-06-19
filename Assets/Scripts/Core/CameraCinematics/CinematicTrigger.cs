using UnityEngine;
using UnityEngine.Playables;
using Frankie.Saving;
using Frankie.Utils;

namespace Frankie.Core
{
    [RequireComponent(typeof(PlayableDirector))]
    public class CinematicTrigger : MonoBehaviour, ISaveable<bool>
    {
        [SerializeField] private bool playOnStart = false;

        // Cached References
        private PlayableDirector playableDirector;

        // State
        private bool isTriggered = false;

        // Methods
        private void Awake()
        {
            playableDirector = GetComponent<PlayableDirector>();
        }

        private void Start()
        {
            if (!playOnStart) { return; }

            bool debugSkipPlay = false;
#if UNITY_EDITOR
            debugSkipPlay = FrankieDebugger.IsCinematicAutoplayDisabled();
#endif
            if (!debugSkipPlay)
            {
                Play();
            }
        }

        public void Play() // Callable via Unity Events
        {
            if (isTriggered) { return; }
            
            playableDirector.Play();
            isTriggered = true;
        }

        public void ForcePlay() // Callable via Unity Events
        {
            playableDirector.Play();
            isTriggered = true;
        }

        // Interface
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;

        public SaveState CaptureState() => ManualGetStateFromData(isTriggered);

        public void RestoreState(SaveState saveState)
        {
            isTriggered = ManualGetDataFromState(saveState);
        }

        public SaveState ManualGetStateFromData(bool data) => new(GetLoadPriority(), data);

        public bool ManualGetDataFromState(SaveState saveState)
        {
            if (saveState == null) { return isTriggered; }
            return (bool)saveState.GetState(typeof(bool));
        }
    }
}
