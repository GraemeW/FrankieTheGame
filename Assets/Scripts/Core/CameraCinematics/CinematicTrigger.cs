using UnityEngine;
using UnityEngine.Playables;
using Frankie.Saving;

namespace Frankie.Core
{
    [RequireComponent(typeof(PlayableDirector))]
    public class CinematicTrigger : MonoBehaviour, ISaveable
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
            Play();
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

        public SaveState CaptureState()
        {
            return new SaveState(GetLoadPriority(), isTriggered);
        }

        public void RestoreState(SaveState saveState)
        {
            isTriggered = (bool)saveState.GetState(typeof(bool));
        }
    }
}
