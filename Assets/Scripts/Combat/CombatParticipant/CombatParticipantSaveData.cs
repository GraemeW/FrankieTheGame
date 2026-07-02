using System;
using UnityEngine;

namespace Frankie.Combat
{
    [Serializable]
    public class CombatParticipantSaveData
    {
        public bool isDead;
        public float hpRatio;
        public float apRatio;

        public CombatParticipantSaveData(bool isDead, float hpRatio, float apRatio)
        {
            this.isDead = isDead;
            this.hpRatio = Mathf.Clamp01(hpRatio);
            this.apRatio = Mathf.Clamp01(apRatio);
        }
    }
}
