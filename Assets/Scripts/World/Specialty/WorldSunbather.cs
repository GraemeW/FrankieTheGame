using UnityEngine;

namespace Frankie.World
{
    [RequireComponent(typeof(Animator))]
    public class WorldSunbather : MonoBehaviour
    {
        // Static
        private static readonly int _topOnRef = Animator.StringToHash("TopOn");
        private static readonly int _bottomOnRef = Animator.StringToHash("BottomOn");
        
        // Clothing State
        private bool topEnabled = true;
        private bool bottomEnabled = true;

        // Cached References
        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        // Public Methods -- called via Unity events
        public void RemoveTop()
        {
            animator.SetBool(_topOnRef, false);
            topEnabled = false;
        }

        public void RemoveBottom()
        {
            if (topEnabled) { return; } // Bottom removable as standalone piece if top already removed -- otherwise call RemoveAll()
            animator.SetBool(_bottomOnRef, false);
            bottomEnabled = false;
        }

        public void RemoveAll()
        {
            animator.SetBool(_topOnRef, false);
            animator.SetBool(_bottomOnRef, false);
            topEnabled = false;
            bottomEnabled = false;
        }

        public void AddTop()
        {
            animator.SetBool(_topOnRef, true);
            topEnabled = true;
        }

        public void AddAll()
        {
            animator.SetBool(_topOnRef, true);
            animator.SetBool(_bottomOnRef, true);
            topEnabled = true;
            bottomEnabled = true;
        }

        public void ToggleAllClothing()
        {
            if (!bottomEnabled && !topEnabled) { AddAll(); }
            else if (!topEnabled) { AddTop(); }
            else { RemoveAll(); }
        }

        public void ToggleTop()
        {
            if (!topEnabled) { AddTop(); }
            else { RemoveTop(); }
        }
    }
}
