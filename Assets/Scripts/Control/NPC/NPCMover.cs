using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public class NPCMover : Mover
    {
        // Tunables
        [SerializeField] Transform interactionCenterPoint = null;

        // Cached References
        Animator animator = null;

        protected override void Awake()
        {
            base.Awake();
            animator = GetComponent<Animator>();
        }

        public void SetLookDirectionToPlayer(PlayerStateHandler playerStateHandler) // called via Unity Event
        {
            PlayerController callingController = playerStateHandler.GetComponent<PlayerController>();
            Vector2 lookDirection = callingController.GetInteractionPosition() - (Vector2)interactionCenterPoint.position;
            SetLookDirection(lookDirection);
            UpdateAnimator();
        }

        private void UpdateAnimator()
        {
            animator.SetFloat("Speed", currentSpeed);
            animator.SetFloat("xLook", lookDirection.x);
            animator.SetFloat("yLook", lookDirection.y);
        }
    }
}
