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
        [SerializeField] private Image shadow;
        [SerializeField] private LayoutElement layoutElement;
        [SerializeField][Tooltip("Only first entry of the enum BattleEntityType is used")] private BattleEntityTypePropertySet[] battleEntityTypePropertyLookUp;
        [SerializeField] private float deathFadeTime = 1.0f;
        [SerializeField][Tooltip("In seconds")] private float halfPulsatingTime = 0.15f;
        [SerializeField] private float pulsatingOpaqueHoldTime = 0.2f;
        [SerializeField][Range(0f,1f)] private float pulsatingMinAlpha = 0.5f;
        
        // State
        private bool isPulsating = false;
        private float pulsatingTimer = 0f;
        private bool isAlphaDecreasing = true;
        
        // Data Structures
        [Serializable]
        public struct BattleEntityTypePropertySet
        {
            public BattleEntityType battleEntityType;
            public Vector2 imageSize;
        }

        #region PublicMethods
        public override void SetBattleEntity(BattleEntity setBattleEntity)
        {
            base.SetBattleEntity(setBattleEntity);
            UpdateImage(battleEntity.combatSprite, battleEntity.battleEntityType, battleEntity.spriteScaleFineTune);
        }
        #endregion

        #region ImplementedMethods
        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            HandleSlidePulsating(Time.deltaTime);
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
                        BlipDimSlide();
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
                    if (!string.IsNullOrWhiteSpace(statusEffectText)) { damageTextSpawner.AddToQueue(new DamageTextData(DamageTextType.Informational, statusEffectText)); }
                    break;
                }
                case StateAlteredType.BaseStateEffectApplied:
                    break;
                case StateAlteredType.Dead:
                {
                    button.enabled = false;
                    shadow.enabled = false;
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
            }
        }

        protected override void SetSelected(CombatParticipantType combatParticipantType, bool enable)
        {
            if (combatParticipantType != CombatParticipantType.Foe) { return; }
            shadow.enabled = enable;
            isPulsating = enable;
            
            switch (enable)
            {
                case true:
                    pulsatingTimer = 0f;
                    isAlphaDecreasing = true;
                    break;
                case false:
                    canvasGroup.alpha = 1.0f;
                    break;
            }
        }
        #endregion

        #region PrivateMethods
        private void UpdateImage(Sprite sprite, BattleEntityType battleEntityType, float spriteScaleFineTune)
        {
            if (sprite == null)  { return; }
            
            if (image != null)
            {
                image.sprite = sprite;
                image.preserveAspect = true;
                image.enabled = true;
            }

            if (shadow != null)
            {
                shadow.sprite = sprite;
                shadow.preserveAspect = true;
                shadow.enabled = false;   
            }
            
            if (layoutElement == null || battleEntityTypePropertyLookUp == null || battleEntityTypePropertyLookUp.Length == 0) { return; }

            // Setting size of image based on enemy type (e.g. mook small, standard medium, boss big)
            foreach (BattleEntityTypePropertySet battleEntityPropertySet in battleEntityTypePropertyLookUp)
            {
                if (battleEntityType != battleEntityPropertySet.battleEntityType) continue;
                layoutElement.preferredHeight = battleEntityPropertySet.imageSize.y * spriteScaleFineTune;
                return;
            }
        }

        private void HandleSlidePulsating(float deltaTime)
        {
            if (!isPulsating) { return; }
            
            pulsatingTimer += deltaTime;
            if ((isAlphaDecreasing && pulsatingTimer > halfPulsatingTime) || (!isAlphaDecreasing && pulsatingTimer > (halfPulsatingTime + pulsatingOpaqueHoldTime)))
            {
                isAlphaDecreasing = !isAlphaDecreasing;
                pulsatingTimer = 0f;
            }
            
            float targetAlpha = isAlphaDecreasing ? pulsatingMinAlpha : 1.0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, (1f - pulsatingMinAlpha) * deltaTime / halfPulsatingTime);
        }

        private IEnumerator DelayToDestroy(float secondForDelay)
        {
            yield return new WaitForSeconds(secondForDelay);
            Destroy(gameObject);
        }
        #endregion
    }
}
