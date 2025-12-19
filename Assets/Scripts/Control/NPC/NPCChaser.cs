using System.Linq;
using UnityEngine;
using Frankie.Utils;

namespace Frankie.Control
{
    [RequireComponent(typeof(NPCStateHandler))]
    [RequireComponent(typeof(NPCMover))]
    public class NPCChaser : MonoBehaviour
    {
        // Tunables
        [Header("Chase Parameters")]
        [SerializeField] private bool willChasePlayer = false;
        [SerializeField] private float chaseDistance = 3.0f;
        [SerializeField] private float aggravationTime = 3.0f;
        [SerializeField] private float suspicionTime = 3.0f;
        [Header("Shout Parameters")]
        [SerializeField] bool willShout = false;
        [Tooltip("Must be true to be shouted at, regardless of group")][SerializeField] private bool canBeShoutedAt = true;
        [Tooltip("From interaction center point of NPC")][SerializeField] private float shoutDistance = 2.0f;
        [Tooltip("Set to nothing to aggro everything shoutable")][SerializeField] private NPCChaser[] shoutGroup;

        // State
        private float timeSinceLastSawPlayer = Mathf.Infinity;
        private bool chasingActive = false;
        private bool skipAggressionUntilEnable = false;
        private bool shoutingActive = false;

        // Cached References
        private NPCStateHandler npcStateHandler;
        private NPCMover npcMover;

        #region UnityMethods
        private void Awake()
        {
            npcStateHandler = GetComponent<NPCStateHandler>();
            npcMover = GetComponent<NPCMover>();
        }

        private void OnEnable()
        {
            timeSinceLastSawPlayer = Mathf.Infinity;
            chasingActive = willChasePlayer;
            skipAggressionUntilEnable = false;
            shoutingActive = willShout;
            npcStateHandler.npcStateChanged += HandleNPCStateChange;
        }

        private void OnDisable()
        {
            npcStateHandler.npcStateChanged -= HandleNPCStateChange;
        }

        private void Update()
        {
            if (!chasingActive) { return; }

            CheckForPlayerProximity();
            timeSinceLastSawPlayer += Time.deltaTime;
        }
        #endregion

        #region PublicMethods
        public bool IsShoutable() => canBeShoutedAt;
        
        public void SetChaseDisposition(bool enable) // Called via Unity Methods
        {
            chasingActive = enable;
            skipAggressionUntilEnable = !enable;
            npcStateHandler.SetNPCIdle();
        }

        public void SetFrenziedWithoutShout() // Called via Unity Methods
        {
            shoutingActive = false;
            npcStateHandler.SetNPCFrenzied();
        }
        #endregion

        #region PrivateMethods
        private bool CheckDistanceToPlayer(float distance)
        {
            return SmartVector2.CheckDistance(npcMover.GetInteractionPosition(), npcStateHandler.GetPlayerInteractionPosition(), distance);
        }

        private void CheckForPlayerProximity()
        {
            if (CheckDistanceToPlayer(chaseDistance)) { timeSinceLastSawPlayer = 0f; }

            if (timeSinceLastSawPlayer < aggravationTime)
            {
                if (!skipAggressionUntilEnable) { npcStateHandler.SetNPCAggravated(); }
            }
            else if (timeSinceLastSawPlayer > aggravationTime && (timeSinceLastSawPlayer - aggravationTime) < suspicionTime)
            {
                npcStateHandler.SetNPCSuspicious();
            }
            else if ((timeSinceLastSawPlayer - aggravationTime) > suspicionTime)
            {
                npcStateHandler.SetNPCIdle();
            }
            timeSinceLastSawPlayer += Time.deltaTime;
        }

        private void HandleNPCStateChange(NPCStateType npcStateType, bool isNPCAfraid)
        {
            switch (npcStateType)
            {
                case NPCStateType.Occupied:
                    chasingActive = false;
                    break;
                case NPCStateType.Idle:
                    chasingActive = willChasePlayer;
                    shoutingActive = willShout;
                    break;
                case NPCStateType.Suspicious:
                case NPCStateType.Aggravated:
                    if (shoutingActive && shoutDistance > 0f)
                    {
                        ShoutToNearbyNPCs();
                    }
                    chasingActive = willChasePlayer;
                    break;
                case NPCStateType.Frenzied:
                    if (shoutingActive && shoutDistance > 0f)
                    {
                        ShoutToNearbyNPCs();
                    }
                    chasingActive = true;
                    break;
            }
        }

        private void ShoutToNearbyNPCs()
        {
            RaycastHit2D[] hits = npcMover.NPCCastFromSelf(shoutDistance);
            foreach (RaycastHit2D hit in hits)
            {
                if (!hit.collider.gameObject.TryGetComponent(out NPCChaser npcInRange)) continue;
                if (!npcInRange.IsShoutable() || npcInRange == this) { continue; }
                
                if (shoutGroup.Length == 0 || shoutGroup.Contains(npcInRange)) // Default behavior, not set, aggro everything shoutable
                {
                    npcInRange.SetFrenziedWithoutShout();
                }
            }
        }

        #endregion

#if UNITY_EDITOR
        public void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, chaseDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, shoutDistance);
        }
#endif
    }
}
