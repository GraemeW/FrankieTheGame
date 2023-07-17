using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public class NPCVisibilityDestroyer : MonoBehaviour
    {
        [SerializeField] bool destroyOnInvisible = false;

        private void OnBecameInvisible()
        {
            // Note NPC parent object one level above renderer
            if (destroyOnInvisible) { Destroy(transform.parent.gameObject); }
        }
    }
}