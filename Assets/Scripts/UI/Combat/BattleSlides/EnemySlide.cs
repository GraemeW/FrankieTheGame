using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Frankie.Combat.UI
{
    public class EnemySlide : BattleSlide
    {
        // Tunables
        [Header("Enemy Slide Settings")]
        [SerializeField] Image image = null;
        [SerializeField] LayoutElement layoutElement = null;
        [SerializeField][Tooltip("Only first entry of the enum BattleEntityType is used")] BattleEntityTypePropertySet[] battleEntityTypePropertyLookUp;
        [SerializeField] float deathFadeTime = 1.0f;

        // Data Structures
        [System.Serializable]
        public struct BattleEntityTypePropertySet
        {
            public BattleEntityType battleEntityType;
            public Vector2 imageSize;
        }

        public override void SetBattleEntity(BattleEntity battleEntity)
        {
            base.SetBattleEntity(battleEntity);
            UpdateImage(this.battleEntity.combatParticipant.GetCombatSprite(), this.battleEntity.battleEntityType);
        }

        protected override void ParseState(CombatParticipant combatParticipant, StateAlteredData stateAlteredData)
        {
            switch (stateAlteredData.stateAlteredType)
            {
                case StateAlteredType.CooldownSet:
                    cooldownTimer.ResetTimer(stateAlteredData.points);
                    break;
                case StateAlteredType.CooldownExpired:
                    cooldownTimer.ResetTimer(0f);
                    break;
                case StateAlteredType.AdjustHPNonSpecific:
                case StateAlteredType.IncreaseHP:
                case StateAlteredType.DecreaseHP:
                    float points = stateAlteredData.points;
                    damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.HealthChanged, points));
                    if (stateAlteredData.stateAlteredType == StateAlteredType.DecreaseHP)
                    {
                        ShakeSlide(false);
                        BlipFadeSlide();
                    }
                    break;
                case StateAlteredType.IncreaseAP:
                case StateAlteredType.DecreaseAP:
                    break;
                case StateAlteredType.HitMiss:
                    damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.HitMiss));
                    break;
                case StateAlteredType.HitCrit:
                    damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.HitCrit));
                    break;
                case StateAlteredType.StatusEffectApplied:
                    PersistentStatus persistentStatus = stateAlteredData.persistentStatus;
                    if (persistentStatus != null)
                    {
                        AddStatusEffectBobble(persistentStatus);
                    }
                    break;
                case StateAlteredType.BaseStateEffectApplied:
                    break;
                case StateAlteredType.Dead:
                    button.enabled = false;
                    image.CrossFadeAlpha(0f, deathFadeTime, false);
                    cooldownTimer?.gameObject.SetActive(false);
                    StartCoroutine(DelayToDestroy(deathFadeTime));
                    break;
                case StateAlteredType.Resurrected:
                    // Support for resurrection nominally not supported -- breaks UI handling (otherwise need to have supervisor enable/disable)
                    break;
            }
        }

        protected override void SetSelected(CombatParticipantType combatParticipantType, bool enable)
        {
            if (combatParticipantType != CombatParticipantType.Foe) { return; }
            GetComponent<Shadow>().enabled = enable;
        }

        // Private Functions
        private void UpdateImage(Sprite sprite, BattleEntityType battleEntityType)
        {
            image.sprite = sprite;
            if (battleEntityTypePropertyLookUp == null || battleEntityTypePropertyLookUp.Length == 0) { return; }

            // Setting size of image based on enemy type (e.g. mook small, standard standard, boss big)
            foreach (BattleEntityTypePropertySet battleEntityPropertySet in battleEntityTypePropertyLookUp)
            {
                if (battleEntityType == battleEntityPropertySet.battleEntityType)
                {
                    layoutElement.preferredWidth = battleEntityPropertySet.imageSize.x;
                    layoutElement.preferredHeight = battleEntityPropertySet.imageSize.y;
                    return;
                }
            }
        }

        private IEnumerator DelayToDestroy(float secondForDelay)
        {
            yield return new WaitForSeconds(secondForDelay);
            Destroy(gameObject);
        }
    }
}
