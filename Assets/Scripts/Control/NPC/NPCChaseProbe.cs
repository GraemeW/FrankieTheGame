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
        private GameObject playerGameObject;
        
        // Cached References
        CircleCollider2D circleCollider2D;

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
            Collider2D playerProbeCollider = Physics2D.OverlapCircle(transform.position, circleCollider2D.radius, playerProbeLayerMask);
            if (playerProbeCollider != null) { SetupPlayerReference(true, playerProbeCollider.gameObject); }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other !=  null) { SetupPlayerReference(true, other.gameObject); }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other != null) { SetupPlayerReference(false, other.gameObject); }
        }
        #endregion
        
        #region PublicMethods
        public bool IsPlayerInRange() => isPlayerInRange;
        public GameObject GetPlayer() => playerGameObject;
        public void SetChaseRadius(float setChaseRadius)
        {
            circleCollider2D.radius = setChaseRadius;
        }
        #endregion
        
        #region PrivateMethods
        private void SetupPlayerReference(bool enable, GameObject playerProbe)
        {
            Transform playerTransform = playerProbe.transform.parent;
            playerGameObject = playerTransform.gameObject;
            isPlayerInRange = enable;
        }
        #endregion
    }
}
