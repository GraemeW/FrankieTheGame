using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Inventory;
using Frankie.Utils;
using Frankie.Utils.Localization;

namespace Frankie.Control
{
    [CreateAssetMenu(fileName = "New Bank Option Check Configuration", menuName = "CheckConfigurations/BankOptions")]
    public class BankOptionCheckConfiguration : CheckConfiguration
    {
        // Tunables
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageBankOptions;
        [SerializeField] private bool toggleDeposit = true;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedOptionDeposit;
        [SerializeField] private bool toggleWithdraw = true;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedOptionWithdraw;
        
        // Implementation
        public override string GetMessage() => localizedMessageBankOptions.GetSafeLocalizedString();
        
        public override List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateHandler, CheckWithConfiguration callingCheck)
        {
            var interactActions = new List<ChoiceActionPair>();
            if (toggleWithdraw)
            {
                var withdrawAction = new ChoiceActionPair(localizedOptionWithdraw.GetSafeLocalizedString(), () => playerStateHandler.EnterBank(BankType.Withdraw));
                interactActions.Add(withdrawAction);
            }
            if (toggleDeposit)
            {
                var depositAction = new ChoiceActionPair(localizedOptionDeposit.GetSafeLocalizedString(), () => playerStateHandler.EnterBank(BankType.Deposit));
                interactActions.Add(depositAction);
            }
            return interactActions;
        }
        
        public override List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedMessageBankOptions.TableEntryReference,
                localizedOptionDeposit.TableEntryReference,
                localizedOptionWithdraw.TableEntryReference
            };
        }
    }
}
