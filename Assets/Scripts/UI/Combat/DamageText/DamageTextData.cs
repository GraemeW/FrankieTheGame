using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat.UI
{
    public class DamageTextData
    {
        public DamageTextType damageTextType;
        public float amount;

        public DamageTextData(DamageTextType damageTextType)
        {
            this.damageTextType = damageTextType;
            this.amount = 0f;
        }

        public DamageTextData(DamageTextType damageTextType, float amount)
        {
            this.damageTextType = damageTextType;
            this.amount = amount;
        }
    }
}