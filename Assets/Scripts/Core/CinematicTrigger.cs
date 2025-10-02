using UnityEngine;
using UnityEngine.Playables;
using Frankie.Saving;

namespace Frankie.Core
{
    [RequireComponent(typeof(PlayableDirector))]
    public class CinematicTrigger : MonoBehaviour, ISaveable
    {
        [SerializeField] bool playOnStart = false;

        // Cached References
        PlayableDirector playableDirector = null;

        // State
        bool isTriggered = false;

        // Methods
        private void Awake()
        {
            playableDirector = GetComponent<PlayableDirector>();
        }

        private void Start()
        {
            if (!playOnStart) { return; }

            Play();
        }

        public void Play() // Callable via Unity Events
        {
            if (!isTriggered)
            {
                playableDirector.Play();
                isTriggered = true;
            }
        }

        public void ForcePlay() // Callable via Unity Events
        {
            playableDirector.Play();
            isTriggered = true;
        }

        // Interface
        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectProperty;
        }

        public SaveState CaptureState()
        {
            UnityEngine.Debug.Log($"Capturing cinematic trigger state as:  {isTriggered}");
            return new SaveState(GetLoadPriority(), isTriggered);
        }

        public void RestoreState(SaveState saveState)
        {
            isTriggered = (bool)saveState.GetState(typeof(bool));
            UnityEngine.Debug.Log($"Restoring cinematic trigger state as:  {isTriggered}");
        }
    }
}
