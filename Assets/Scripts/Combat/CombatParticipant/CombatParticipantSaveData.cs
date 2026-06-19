using System;

namespace Frankie.Combat
{
    [Serializable]
    public class CombatParticipantSaveData
    {
        public bool isDead;
        public float currentHP;
        public float currentAP;

        public CombatParticipantSaveData(bool isDead, float currentHP, float currentAP)
        {
            this.isDead = isDead;
            this.currentHP = currentHP;
            this.currentAP = currentAP;
        }
    }
}
