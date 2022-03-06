using Frankie.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    [RequireComponent(typeof(NPCStateHandler))]
    public class NPCChaser : MonoBehaviour
    {
        // Tunables
        [SerializeField] bool willChasePlayer = false;
        [SerializeField] float chaseDistance = 3.0f;
        [SerializeField] float aggravationTime = 3.0f;
        [SerializeField] float suspicionTime = 3.0f;

        // State
        float timeSinceLastSawPlayer = Mathf.Infinity;
        bool chasingActive = false;

        // Cached References
        NPCStateHandler npcStateHandler = null;

        #region UnityMethods
        private void Awake()
        {
            npcStateHandler = GetComponent<NPCStateHandler>();
        }

        private void OnEnable()
        {
            timeSinceLastSawPlayer = Mathf.Infinity;
            chasingActive = willChasePlayer;
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
            npcStateHandler.SetNPCIdle();
        }
        #endregion

        #region PrivateMethods
        private void CheckForPlayerProximity()
        {
            if (npcStateHandler.CheckDistanceToPlayer(chaseDistance))
            {
                timeSinceLastSawPlayer = 0f;
            }

            if (timeSinceLastSawPlayer < aggravationTime)
            {
                npcStateHandler.SetNPCState(NPCStateType.aggravated);
            }
            else if (timeSinceLastSawPlayer > aggravationTime && (timeSinceLastSawPlayer - aggravationTime) < suspicionTime)
            {
                npcStateHandler.SetNPCState(NPCStateType.suspicious);
            }
            else if ((timeSinceLastSawPlayer - aggravationTime) > suspicionTime)
            {
                npcStateHandler.SetNPCState(NPCStateType.idle);
            }
            timeSinceLastSawPlayer += Time.deltaTime;
        }

        private void HandleNPCStateChange(NPCStateType npcStateType)
        {
            switch (npcStateType)
            {
                case NPCStateType.occupied:
                    chasingActive = false;
                    break;
                case NPCStateType.idle:
                case NPCStateType.suspicious:
                case NPCStateType.aggravated:
                case NPCStateType.frenzied:
                    chasingActive = willChasePlayer;
                    break;
            }
        }

        #endregion

#if UNITY_EDITOR
        public void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, chaseDistance);
        }
#endif
    }
}