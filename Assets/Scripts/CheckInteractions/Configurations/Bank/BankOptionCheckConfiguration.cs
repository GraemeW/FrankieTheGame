using System.Collections.Generic;
using UnityEngine;
using Frankie.Inventory;
using Frankie.Utils;

namespace Frankie.Control
{
    [CreateAssetMenu(fileName = "New Bank Option Check Configuration", menuName = "CheckConfigurations/BankOptions")]
    public class BankOptionCheckConfiguration : CheckConfiguration
    {
        // Tunables
        [SerializeField] private string messageBankOptions = "What would you like to do?";
        [SerializeField] private bool toggleDeposit = true;
        [SerializeField] private string optionDeposit = "Deposit";
        [SerializeField] private bool toggleWithdraw = true;
        [SerializeField] private string optionWithdraw = "Withdraw";

        // Implementation
        public override string GetMessage() => messageBankOptions;
        
        public override List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateHandler, CheckWithConfiguration callingCheck)
        {
            var interactActions = new List<ChoiceActionPair>();
            if (toggleWithdraw)
            {
                var withdrawAction = new ChoiceActionPair(optionWithdraw, () => playerStateHandler.EnterBank(BankType.Withdraw));
                interactActions.Add(withdrawAction);
            }
            if (toggleDeposit)
            {
                var depositAction = new ChoiceActionPair(optionDeposit, () => playerStateHandler.EnterBank(BankType.Deposit));
                interactActions.Add(depositAction);
            }
            return interactActions;
        }
    }
}
