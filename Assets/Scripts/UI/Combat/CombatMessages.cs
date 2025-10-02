using UnityEngine;
using Frankie.Combat;
using Frankie.Speech.UI;

public class CombatMessages : MonoBehaviour
{
    // Tunables
    [Header("Hook-Ups")]
    [SerializeField] Transform messageParent = null;
    [SerializeField] DialogueBox dialogueBoxPrefab = null;
    [Header("Messages")]
    [Tooltip("Include {0} for item")] [SerializeField] string messageItemToBeUsed = "Use the item {0} on whom?";

    private void OnEnable()
    {
        BattleEventBus<BattleActionArmedEvent>.SubscribeToEvent(Setup);
    }

    private void OnDisable()
    {
        BattleEventBus<BattleActionArmedEvent>.UnsubscribeFromEvent(Setup);
    }

    private void Setup(BattleActionArmedEvent battleActionArmedEvent)
    {
        IBattleActionSuper battleActionSuper = battleActionArmedEvent.battleActionSuper;

        if (battleActionSuper != null && battleActionSuper.IsItem())
        {
            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, messageParent);
            dialogueBox.AddText(string.Format(messageItemToBeUsed, battleActionSuper.GetName()));
            dialogueBox.SetGlobalInput(false);
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
