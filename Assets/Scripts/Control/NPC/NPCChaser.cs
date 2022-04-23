using Frankie.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Control
{
    [RequireComponent(typeof(NPCStateHandler))]
    [RequireComponent(typeof(NPCMover))]
    public class NPCChaser : MonoBehaviour
    {
        // Tunables
        [Header("Chase Parameters")]
        [SerializeField] bool willChasePlayer = false;
        [SerializeField] float chaseDistance = 3.0f;
        [SerializeField] float aggravationTime = 3.0f;
        [SerializeField] float suspicionTime = 3.0f;
        [Header("Shout Parameters")]
        [SerializeField] bool willShout = false;
        [Tooltip("Must be true to be shouted at, regardless of group")] [SerializeField] bool canBeShoutedAt = true;
        [Tooltip("From interaction center point of NPC")] [SerializeField] float shoutDistance = 2.0f;
        [Tooltip("Set to nothing to aggro everything shoutable")] [SerializeField] NPCChaser[] shoutGroup = null;

        // State
        float timeSinceLastSawPlayer = Mathf.Infinity;
        bool chasingActive = false;
        bool skipChaseUntilEnable = false;
        bool shoutingActive = false;

        // Cached References
        NPCStateHandler npcStateHandler = null;
        NPCMover npcMover = null;

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
            skipChaseUntilEnable = false;
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
        public void SetChaseDisposition(bool enable) // Called via Unity Methods
        {
            chasingActive = enable;
            skipChaseUntilEnable = !enable;
            npcStateHandler.SetNPCIdle();
        }

        public void SetFrenziedWithoutShout()
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

        private bool IsShoutable()
        {
            return canBeShoutedAt;
        }

        private void CheckForPlayerProximity()
        {
            if (CheckDistanceToPlayer(chaseDistance))
            {
                timeSinceLastSawPlayer = 0f;
            }

            if (timeSinceLastSawPlayer < aggravationTime)
            {
                if (!skipChaseUntilEnable) { npcStateHandler.SetNPCAggravated(); }
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
                case NPCStateType.occupied:
                    chasingActive = false;
                    break;
                case NPCStateType.idle:
                    chasingActive = willChasePlayer;
                    shoutingActive = willShout;
                    break;
                case NPCStateType.suspicious:
                case NPCStateType.aggravated:
                    if (shoutingActive && shoutDistance > 0f)
                    {
                        ShoutToNearbyNPCs();
                    }
                    chasingActive = willChasePlayer;
                    break;
                case NPCStateType.frenzied:
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
                if (hit.collider.gameObject.TryGetComponent(out NPCChaser npcInRange))
                {
                    if (!npcInRange.IsShoutable() || npcInRange == this) { continue; }
                    if (shoutGroup.Length == 0 || shoutGroup.Contains(npcInRange)) // Default behavior, not set, aggro everything shoutable
                    {
                        npcInRange.SetFrenziedWithoutShout();
                    }
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