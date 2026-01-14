using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Frankie.Combat.UI
{
    public class EnemySlide : BattleSlide
    {
        // Tunables
        [Header("Enemy Slide Settings")]
        [SerializeField] private Image image;
        [SerializeField] private LayoutElement layoutElement;
        [SerializeField][Tooltip("Only first entry of the enum BattleEntityType is used")] private BattleEntityTypePropertySet[] battleEntityTypePropertyLookUp;
        [SerializeField] private float deathFadeTime = 1.0f;

        // Data Structures
        [Serializable]
        public struct BattleEntityTypePropertySet
        {
            public BattleEntityType battleEntityType;
            public Vector2 imageSize;
        }

        public override void SetBattleEntity(BattleEntity setBattleEntity)
        {
            base.SetBattleEntity(setBattleEntity);
            UpdateImage(battleEntity.combatSprite, battleEntity.battleEntityType, battleEntity.spriteScaleFineTune);
        }

        protected override void ParseState(StateAlteredInfo stateAlteredInfo)
        {
            switch (stateAlteredInfo.stateAlteredType)
            {
                case StateAlteredType.CooldownSet:
                {
                    cooldownTimer.ResetTimer(stateAlteredInfo.points);
                    cooldownTimer.SetPaused(float.IsPositiveInfinity(stateAlteredInfo.points));
                    break;
                }
                case StateAlteredType.CooldownExpired:
                {
                    cooldownTimer.ResetTimer(0f);
                    break;
                }
                case StateAlteredType.AdjustHPNonSpecific:
                case StateAlteredType.IncreaseHP:
                case StateAlteredType.DecreaseHP:
                {
                    float points = stateAlteredInfo.points;
                    damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.HealthChanged, points));
                    if (stateAlteredInfo.stateAlteredType == StateAlteredType.DecreaseHP)
                    {
                        ShakeSlide(false);
                        BlipFadeSlide();
                    }

                    break;
                }
                case StateAlteredType.AdjustAPNonSpecific:
                case StateAlteredType.IncreaseAP:
                case StateAlteredType.DecreaseAP:
                    break;
                case StateAlteredType.HitMiss:
                {
                    damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.HitMiss));
                    break;
                }
                case StateAlteredType.HitCrit:
                {
                    damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.HitCrit));
                    break;
                }
                case StateAlteredType.StatusEffectApplied:
                {
                    PersistentStatus persistentStatus = stateAlteredInfo.persistentStatus;
                    if (persistentStatus == null) { break; }
                    
                    AddStatusEffectBobble(persistentStatus);
                    string statusEffectText = GetStatusEffectText(persistentStatus);
                    if (!string.IsNullOrWhiteSpace(statusEffectText)) { damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.Informational, statusEffectText)); };
                    break;
                }
                case StateAlteredType.BaseStateEffectApplied:
                    break;
                case StateAlteredType.Dead:
                {
                    button.enabled = false;
                    image.CrossFadeAlpha(0f, deathFadeTime, false);
                    cooldownTimer?.gameObject.SetActive(false);
                    ClearStatusEffectBobbles();
                    StartCoroutine(DelayToDestroy(deathFadeTime));
                    break;
                }
                case StateAlteredType.Resurrected:
                    // Support for resurrection nominally not supported -- breaks UI handling (otherwise need to have supervisor enable/disable)
                    break;
                case StateAlteredType.FriendFound:
                {
                    damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.Informational, "*hello!*"));
                    break;
                }
                case StateAlteredType.FriendIgnored:
                {
                    damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.Informational, "*lonely*"));
                    break;
                }
                case StateAlteredType.ActionDequeued:
                {
                    BlipGrowSlide();
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void SetSelected(CombatParticipantType combatParticipantType, bool enable)
        {
            if (combatParticipantType != CombatParticipantType.Foe) { return; }
            GetComponent<Shadow>().enabled = enable;
        }

        // Private Functions
        private void UpdateImage(Sprite sprite, BattleEntityType battleEntityType, float spriteScaleFineTune)
        {
            image.sprite = sprite;
            if (battleEntityTypePropertyLookUp == null || battleEntityTypePropertyLookUp.Length == 0) { return; }

            // Setting size of image based on enemy type (e.g. mook small, standard medium, boss big)
            foreach (BattleEntityTypePropertySet battleEntityPropertySet in battleEntityTypePropertyLookUp)
            {
                if (battleEntityType != battleEntityPropertySet.battleEntityType) continue;
                layoutElement.preferredHeight = battleEntityPropertySet.imageSize.y * spriteScaleFineTune;
                return;
            }
        }

        private IEnumerator DelayToDestroy(float secondForDelay)
        {
            yield return new WaitForSeconds(secondForDelay);
            Destroy(gameObject);
        }
    }
}
