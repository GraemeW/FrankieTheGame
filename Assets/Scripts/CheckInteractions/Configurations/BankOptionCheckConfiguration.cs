using Frankie.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    [CreateAssetMenu(fileName = "New Bank Option Check Configuration", menuName = "CheckConfigurations/BankOptions")]
    public class BankOptionCheckConfiguration : CheckConfiguration
    {
        // Tunables
        [SerializeField] string messageBankOptions = "What would you like to do?";
        [SerializeField] bool toggleDeposit = true;
        [SerializeField] string optionDeposit = "Deposit";
        [SerializeField] CheckConfiguration depositConfiguration = null;
        [SerializeField] bool toggleWithdraw = true;
        [SerializeField] string optionWithdraw = "Withdraw";
        [SerializeField] CheckConfiguration withdrawConfiguration = null;

        // Implementation
        public override List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateHandler playerStateHandler)
        {
            List<ChoiceActionPair> interactActions = new List<ChoiceActionPair>();
            if (toggleWithdraw)
            {
                AddDialogueSpawnOptionForConfiguration(ref interactActions, playerStateHandler, optionWithdraw, withdrawConfiguration);
            }
            if (toggleDeposit)
            {
                AddDialogueSpawnOptionForConfiguration(ref interactActions, playerStateHandler, optionDeposit, depositConfiguration);
            }

            return interactActions;
        }

        public override string GetMessage()
        {
            return messageBankOptions;
        }
    }
}