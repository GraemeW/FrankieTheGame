using UnityEngine;

namespace Frankie.Control
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class NPCChaseProbe : MonoBehaviour
    {
        // Tunables
        [SerializeField] LayerMask playerProbeLayerMask;
        
        // State
        private bool isPlayerInRange;
        private GameObject chaseObject;
        
        // Cached References
        private CircleCollider2D circleCollider2D;

        #region UnityMethods
        private void Awake()
        {
            circleCollider2D = GetComponent<CircleCollider2D>();
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
            if (probeCollider != null) { SetupChaseObjectReference(true, probeCollider.gameObject); }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other != null) { SetupChaseObjectReference(true, other.gameObject); }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other != null) { SetupChaseObjectReference(false, other.gameObject); }
        }
        #endregion
        
        #region PublicMethods
        public bool IsPlayerInRange() => isPlayerInRange;
        public GameObject GetChaseObject() => chaseObject;
        public void SetChaseRadius(float setChaseRadius)
        {
            circleCollider2D.radius = setChaseRadius;
        }
        #endregion
        
        #region PrivateMethods
        private void SetupChaseObjectReference(bool enable, GameObject playerProbe)
        {
            isPlayerInRange = enable;
            if (!enable) { 
                chaseObject = null;
                return;
            }
            
            // Avoid overwriting for multiple probe hits, since probe lives on character party members
            if (chaseObject != null) { return; }
            
            Transform chaseObjectTransform = playerProbe != null ? playerProbe.transform.parent : null;
            chaseObject = chaseObjectTransform != null ? chaseObjectTransform.gameObject : null;
        }
        #endregion
    }
}
