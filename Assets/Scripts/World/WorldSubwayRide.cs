using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control.Specialization
{
    [RequireComponent(typeof(NPCMover))]
    public class WorldSubwayRide : MonoBehaviour
    {
        // Tunables
        [SerializeField] Transform movePosition = null;

        // Cached References
        NPCMover npcMover = null;

        private void Awake()
        {
            npcMover = GetComponent<NPCMover>();
        }

        public void StartRide()
        {
            if (movePosition == null) { return; }

            npcMover.SetMoveTarget(movePosition.position);
        }
    }
}
