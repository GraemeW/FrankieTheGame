using Frankie.Control;
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Frankie.Dialogue
{
    public class AIConversant : Check
    {
        // Tunables
        [Tooltip("Override by AIConversant attached to dialogue node")][SerializeField] string defaultName = "";
        [SerializeField] Dialogue dialogue = null;

        // State
        BaseStats baseStats = null;

        private void Awake()
        {
            baseStats = GetComponentInParent<BaseStats>(); // Standard implementation has conversant on a childed game object to override 2D collider
            if (baseStats == null)
            {
                baseStats = GetComponent<BaseStats>();
            }
        }

        private void Start()
        {
            if (dialogue != null)
            {
                dialogue.OverrideSpeakerNames();
            }
        }

        public string GetConversantName()
        {
            if (baseStats != null)
            {
                // Split apart name on lower case followed by upper case w/ or w/out underscores
                return Regex.Replace(baseStats.GetCharacterName().ToString("G"), "([a-z])_?([A-Z])", "$1 $2");
            }
            return defaultName;
        }

        public override bool HandleRaycast(PlayerController callingController, string interactButtonOne = "Fire1", string interactButtonTwo = "Fire2")
        {
            if (dialogue == null) { return false; }

            if (!this.CheckDistance(gameObject, transform.position, callingController,
                overrideDefaultInteractionDistance, interactionDistance))
            {
                return false;
            }

            if (Input.GetButtonDown(interactButtonOne))
            {
                if (checkInteraction != null)
                {
                    checkInteraction.Invoke(callingController);
                }
                callingController.GetComponent<DialogueController>().InitiateConversation(this, dialogue);
            }
            return true;
        }

        public override bool HandleRaycast(PlayerController callingController, KeyCode interactKeyOne = KeyCode.E, KeyCode interactKeyTwo = KeyCode.Return)
        {
            if (dialogue == null) { return false; }

            if (!this.CheckDistance(gameObject, transform.position, callingController,
                overrideDefaultInteractionDistance, interactionDistance))
            {
                return false;
            }

            if (Input.GetKeyDown(interactKeyOne))
            {
                if (checkInteraction != null)
                {
                    checkInteraction.Invoke(callingController);
                }
                callingController.GetComponent<DialogueController>().InitiateConversation(this, dialogue);
            }
            return true;
        }

        public override CursorType GetCursorType()
        {
            return CursorType.Talk;
        }
    }
}