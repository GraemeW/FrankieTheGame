using UnityEngine;

namespace Frankie.Control.Specialization
{
    [RequireComponent(typeof(Animator))]
    public class WorldSunbather : MonoBehaviour
    {
        // Clothing State
        bool topEnabled = true;
        bool bottomEnabled = true;

        // Cached References
        Animator animator = null;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        // Public Methods -- called via Unity events
        public void RemoveTop()
        {
            animator.SetBool("TopOn", false);
            topEnabled = false;
        }

        public void RemoveBottom()
        {
            if (topEnabled) { return; } // Bottom removable as standalone piece if top already removed -- otherwise call RemoveAll()
            animator.SetBool("BottomOn", false);
            bottomEnabled = false;
        }

        public void RemoveAll()
        {
            animator.SetBool("TopOn", false);
            animator.SetBool("BottomOn", false);
            topEnabled = false;
            bottomEnabled = false;
        }

        public void AddTop()
        {
            animator.SetBool("TopOn", true);
            topEnabled = true;
        }

        public void AddAll()
        {
            animator.SetBool("TopOn", true);
            animator.SetBool("BottomOn", true);
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
