using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Frankie.Core;

namespace Frankie.Control
{
    public class NPCController : MonoBehaviour
    {
        // Tunables
        [SerializeField] Transform interactionCenterPoint = null;
        [SerializeField] TransitionType battleEntryType = TransitionType.BattleGood; // HACK -- TO REMOVE, TESTING

        // Cached References
        Animator animator = null;
        Rigidbody2D npcRigidBody2D = null;

        // State
        Vector2 lookDirection = new Vector2();
        float currentSpeed = 0;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            npcRigidBody2D = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            lookDirection = Vector2.down;
        }

        public void SetLookDirectionToPlayer(PlayerController callingController)
        {
            Vector2 lookDirection = callingController.GetInteractionPosition() - (Vector2)interactionCenterPoint.position;
            SetLookDirection(lookDirection);
            UpdateAnimator();

            callingController.EnterCombat(this, battleEntryType);
        }

        public void SetLookDirection(Vector2 lookDirection)
        {
            this.lookDirection = lookDirection;
        }

        private void UpdateAnimator()
        {
            animator.SetFloat("Speed", currentSpeed);
            animator.SetFloat("xLook", lookDirection.x);
            animator.SetFloat("yLook", lookDirection.y);
        }
    }
}