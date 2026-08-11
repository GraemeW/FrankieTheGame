using UnityEngine;
using LowDefMustard.UIBox;

namespace Frankie.Inventory.UI
{
    public class CashTransferField : UIChoiceButton
    {
        [SerializeField] CashTransferFieldType cashTransferFieldType = CashTransferFieldType.One;

        public CashTransferFieldType GetCashTransferFieldType()
        {
            return cashTransferFieldType;
        }
    }
}
