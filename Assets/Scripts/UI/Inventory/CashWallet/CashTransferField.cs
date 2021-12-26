using Frankie.Utils.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory.UI
{
    public class CashTransferField : UIChoiceOption
    {
        [SerializeField] CashTransferFieldType cashTransferFieldType = CashTransferFieldType.One;

        public CashTransferFieldType GetCashTransferFieldType()
        {
            return cashTransferFieldType;
        }
    }
}