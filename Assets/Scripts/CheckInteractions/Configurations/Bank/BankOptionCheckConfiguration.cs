using Frankie.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Inventory;

namespace Frankie.Control
{
    [CreateAssetMenu(fileName = "New Bank Option Check Configuration", menuName = "CheckConfigurations/BankOptions")]
    public class BankOptionCheckConfiguration : CheckConfiguration
    {
        // Tunables
        [SerializeField] string messageBankOptions = "What would you like to do?";
        [SerializeField] bool toggleDeposit = true;
        [SerializeField] string optionDeposit = "Deposit";
        [SerializeField] bool toggleWithdraw = true;
        [SerializeField] string optionWithdraw = "Withdraw";

        // Implementation
        public override List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateHandler, CheckWithConfiguration callingCheck)
        {
            List<ChoiceActionPair> interactActions = new List<ChoiceActionPair>();
            if (toggleWithdraw)
            {
                ChoiceActionPair withdrawAction = new ChoiceActionPair(optionWithdraw, () => playerStateHandler.EnterBank(BankType.Withdraw));
                interactActions.Add(withdrawAction);
            }
            if (toggleDeposit)
            {
                ChoiceActionPair depositAction = new ChoiceActionPair(optionDeposit, () => playerStateHandler.EnterBank(BankType.Deposit));
                interactActions.Add(depositAction);
            }

            return interactActions;
        }

        public override string GetMessage()
        {
            return messageBankOptions;
        }
    }
}