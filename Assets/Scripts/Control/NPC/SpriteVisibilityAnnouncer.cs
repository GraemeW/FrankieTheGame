using System;
using UnityEngine;

namespace Frankie.Control
{
    public class SpriteVisibilityAnnouncer : MonoBehaviour
    {
        // Events
        public event Action<bool> spriteVisibilityStatus;

        // Unity visibility events are only called if attached game object is rendered in camera
        // This is a light wrapper for pass-through events for parent game objects
        private void OnBecameInvisible()
        {
            spriteVisibilityStatus?.Invoke(false);
        }

        private void OnBecameVisible()
        {
            spriteVisibilityStatus?.Invoke(true);
        }
    }
}
