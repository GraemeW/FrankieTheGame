using Frankie.Combat;
using Frankie.Speech.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatMessages : MonoBehaviour
{
    // Tunables
    [Header("Hook-Ups")]
    [SerializeField] Transform messageParent = null;
    [SerializeField] GameObject dialogueBoxPrefab = null;
    [Header("Messages")]
    [Tooltip("Include {0} for item")] [SerializeField] string messageItemToBeUsed = "Use the item {0} on which foe?";


    // Cached References
    BattleController battleController = null;

    private void Awake()
    {
        battleController = GameObject.FindGameObjectWithTag("BattleController").GetComponent<BattleController>();
    }

    private void OnEnable()
    {
        battleController.battleActionArmedStateChanged += Setup;
    }

    private void OnDisable()
    {
        battleController.battleActionArmedStateChanged -= Setup;
    }

    private void Setup(BattleAction battleAction)
    {
        if (battleAction.battleActionType == BattleActionType.ActionItem)
        {
            GameObject dialogueBoxObject = Instantiate(dialogueBoxPrefab, messageParent);
            DialogueBox dialogueBox = dialogueBoxObject.GetComponent<DialogueBox>();
            dialogueBox.AddText(string.Format(messageItemToBeUsed, battleAction.name));
            dialogueBox.SetGlobalCallbacks(null);
        }
        else
        {
            foreach (Transform child in messageParent)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
