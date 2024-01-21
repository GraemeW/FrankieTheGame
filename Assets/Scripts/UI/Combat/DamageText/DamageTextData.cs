using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat.UI
{
    public class DamageTextData
    {
        public DamageTextType damageTextType;
        public float amount;
        public string information;

        public DamageTextData(DamageTextType damageTextType)
        {
            this.damageTextType = damageTextType;
            this.amount = 0f;
            this.information = "";
        }

        public DamageTextData(DamageTextType damageTextType, float amount)
        {
            this.damageTextType = damageTextType;
            this.amount = amount;
            this.information = "";
        }

        public DamageTextData(DamageTextType damageTextType, string information)
        {
            this.damageTextType = damageTextType;
            this.amount = 0f;
            this.information = information;
        }
    }
}