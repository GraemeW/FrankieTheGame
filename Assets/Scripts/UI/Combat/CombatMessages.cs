using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Combat;
using Frankie.Speech.UI;
using Frankie.Utils.Localization;

public class CombatMessages : MonoBehaviour, ILocalizable
{
    // Tunables
    [Header("Hook-Ups")]
    [SerializeField] private Transform messageParent;
    [SerializeField] private DialogueBox dialogueBoxPrefab;
    [Header("Messages")]
    [Header("Include {0} for item")]
    [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageItemToBeUsed;

    #region UnityMethods
    private void OnEnable()
    {
        BattleEventBus<BattleActionArmedEvent>.SubscribeToEvent(Setup);
    }

    private void OnDisable()
    {
        BattleEventBus<BattleActionArmedEvent>.UnsubscribeFromEvent(Setup);
    }
    #endregion
    
    #region LocalizationMethods

    public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
    public List<TableEntryReference> GetLocalizationEntries()
    {
        return new List<TableEntryReference>
        {
            localizedMessageItemToBeUsed.TableEntryReference
        };
    }
    #endregion

    #region PrivateMethods
    private void Setup(BattleActionArmedEvent battleActionArmedEvent)
    {
        IBattleActionSuper battleActionSuper = battleActionArmedEvent.battleActionSuper;

        if (battleActionSuper != null && battleActionSuper.IsItem())
        {
            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, messageParent);
            dialogueBox.AddText(string.Format(localizedMessageItemToBeUsed.GetSafeLocalizedString(), battleActionSuper.GetName()));
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
    #endregion
}
