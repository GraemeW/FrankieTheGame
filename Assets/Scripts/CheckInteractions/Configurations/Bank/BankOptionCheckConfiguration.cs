using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Core;
using Frankie.Inventory;

namespace Frankie.Control
{
    [CreateAssetMenu(fileName = "New Bank Option Check Configuration", menuName = "CheckConfigurations/Bank/BankOptions", order = 5)]
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
        
        public override List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateMachine, CheckWithConfiguration callingCheck)
        {
            var interactActions = new List<ChoiceActionPair>();
            if (toggleWithdraw)
            {
                var withdrawAction = new ChoiceActionPair(localizedOptionWithdraw.GetSafeLocalizedString(), () => playerStateMachine.EnterBank(BankType.Withdraw));
                interactActions.Add(withdrawAction);
            }
            if (toggleDeposit)
            {
                var depositAction = new ChoiceActionPair(localizedOptionDeposit.GetSafeLocalizedString(), () => playerStateMachine.EnterBank(BankType.Deposit));
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
