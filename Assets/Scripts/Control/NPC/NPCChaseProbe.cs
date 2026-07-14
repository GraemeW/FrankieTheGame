using System;
using UnityEngine;

namespace Frankie.Control
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class NPCChaseProbe : MonoBehaviour
    {
        // Tunables
        [SerializeField] private LayerMask playerProbeLayerMask;
        
        // State
        private bool isPlayerInRange;
        private float defaultShoutDistance;
        private GameObject chaseTarget;
        
        // Cached References
        private CircleCollider2D circleCollider2D;
        
        // Events
        public event Action<GameObject> chaseTargetUpdated;

        #region UnityMethods
        private void Awake()
        {
            circleCollider2D = GetComponent<CircleCollider2D>();
            defaultShoutDistance = circleCollider2D.radius;
        }
        
        // Note:
        // Only need to handle:
        // 1. NPC pop-in via OnEnable (check for overlap)
        // 2. OnTrigger2D Enter/Exit events
        // , but NOT player instantiation / destruction, since player is singleton and is expected to never pop in/out
        // This logic does NOT hold for probing things that can be destroyed, as this does not prompt a trigger event

        private void OnEnable()
        {
            Collider2D probeCollider = Physics2D.OverlapCircle(transform.position, circleCollider2D.radius, playerProbeLayerMask);
            if (probeCollider != null) { SetupChaseTargetReference(true, probeCollider.gameObject); }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other != null) { SetupChaseTargetReference(true, other.gameObject); }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other != null) { SetupChaseTargetReference(false, other.gameObject); }
        }
        #endregion
        
        #region PublicMethods
        public bool IsPlayerInRange() => isPlayerInRange;
        public GameObject GetChaseObject() => chaseTarget;
        public void ResetChaseRadius() => circleCollider2D.radius = defaultShoutDistance;
        
        public void OverrideDefaultChaseRadius(float setChaseRadius)
        {
            defaultShoutDistance = setChaseRadius;
            ResetChaseRadius();
        }

        public void GrowChaseRadius(float increment)
        {
            float newChaseRadius = Mathf.Max(circleCollider2D.radius, defaultShoutDistance + increment);
            circleCollider2D.radius = newChaseRadius;
        }
        #endregion
        
        #region PrivateMethods
        private void SetupChaseTargetReference(bool enable, GameObject playerProbe)
        {
            isPlayerInRange = enable;
            if (!enable) { 
                chaseTarget = null;
                return;
            }
            
            // Avoid overwriting for multiple probe hits, since probe lives on character party members
            if (chaseTarget != null) { return; }
            
            Transform chaseObjectTransform = playerProbe != null ? playerProbe.transform.parent : null;
            chaseTarget = chaseObjectTransform != null ? chaseObjectTransform.gameObject : null;
            if (chaseTarget != null) { chaseTargetUpdated?.Invoke(chaseTarget); }
        }
        #endregion
    }
}
