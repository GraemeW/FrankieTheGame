using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;
using Frankie.Utils.Localization;

namespace Frankie.Combat.UI
{
    public sealed class EnemySlideHandle : EntryHandle
    {
        // Data Models:
        //  - enemy values (status effects) come from CombatParticipantModel
        //  - slide-only state (targeting, cooldown, effects, defeat) from the slide model
        // Note:  Releases itself once the enemy's defeat fade completes; otherwise call Release() when the battle UI is done with it

        // State
        private readonly BattleEntity battleEntity;
        private readonly Material targetShadowMaterial;
        private readonly Action onClicked;
        private readonly CombatParticipantModel participantModel;
        private readonly EnemySlideModel model = new();
        private bool isReleased = false;

        // Constructor
        public EnemySlideHandle(UIToolkitBoxView view, BattleEntity battleEntity, BattleSlideText slideText, Material targetShadowMaterial, Action onClicked) : base(view, GetRowType(battleEntity))
        {
            this.battleEntity = battleEntity;
            this.targetShadowMaterial = targetShadowMaterial;
            this.onClicked = onClicked;
            if (slideText != null) { slideText.ApplyTo(model); }
            if (!TryGetEnemy(out CombatParticipant enemy)) { isReleased = true; return; } // Nothing to show or listen to

            participantModel = new CombatParticipantModel(enemy);
            participantModel.stateAltered += ParseState;
        }

        private static Type GetRowType(BattleEntity battleEntity) => battleEntity?.row switch
        {
            BattleRow.Top => typeof(EnemySlideTopRow),
            BattleRow.Bottom => typeof(EnemySlideBottomRow),
            _ => typeof(EnemySlideMiddleRow)
        };

        #region EntryHandle
        protected override VisualElement CreateElement()
        {
            var enemySlideElement = new EnemySlideElement(battleEntity, targetShadowMaterial) { dataSource = model, enemyDataSource = participantModel };
            enemySlideElement.clicked += HandleClicked;
            enemySlideElement.defeatFadeCompleted += Release;
            return enemySlideElement;
        }

        protected override void UnhookElement(VisualElement detachingElement)
        {
            if (detachingElement is not EnemySlideElement enemySlideElement) { return; }
            enemySlideElement.clicked -= HandleClicked;
            enemySlideElement.defeatFadeCompleted -= Release;
        }
        #endregion

        #region PublicMethods
        public void Release()
        {
            if (isReleased) { return; }
            isReleased = true;

            if (participantModel != null)
            {
                participantModel.stateAltered -= ParseState;
                participantModel.Dispose();
            }
            Remove();
        }

        public void HighlightSlide(BattleEntitySelectionType selectionType, IEnumerable<BattleEntity> battleEntities)
        {
            if (selectionType != BattleEntitySelectionType.Target || isReleased) { return; }
            model.isTargeted = TryGetEnemy(out CombatParticipant enemy) && battleEntities != null && battleEntities.Any(target => target != null && target.combatParticipant == enemy);
        }

        // The slide's element, for placing other stage elements over it (e.g. battle effects)
        public bool TryGetSlideElement(out VisualElement slideElement)
        {
            slideElement = isReleased || element?.panel == null ? null : element;
            return slideElement != null;
        }
        #endregion

        #region PrivateMethods
        private void HandleClicked()
        {
            if (isReleased || model.isDefeated) { return; }
            onClicked?.Invoke();
        }

        private bool TryGetEnemy(out CombatParticipant enemy)
        {
            enemy = battleEntity?.combatParticipant;
            return enemy != null;
        }

        private void QueueInformationalText(string text)
        {
            if (!string.IsNullOrWhiteSpace(text)) { model.QueueDamageText(new DamageTextData(DamageTextType.Informational, text)); }
        }

        private void ParseState(StateAlteredInfo stateAlteredInfo)
        {
            if (isReleased) { return; }

            switch (stateAlteredInfo.stateAlteredType)
            {
                case StateAlteredType.CooldownSet:
                    model.cooldown = CooldownTiming.Restart(stateAlteredInfo.points);
                    break;
                case StateAlteredType.CooldownExpired:
                    model.cooldown = CooldownTiming.Restart(0f);
                    break;
                case StateAlteredType.AdjustHPNonSpecific:
                case StateAlteredType.IncreaseHP:
                case StateAlteredType.DecreaseHP:
                    model.QueueDamageText(new DamageTextData(DamageTextType.HealthChanged, stateAlteredInfo.points));
                    if (stateAlteredInfo.stateAlteredType == StateAlteredType.DecreaseHP)
                    {
                        model.Shake(false);
                        model.BlipDim();
                    }
                    break;
                case StateAlteredType.HitMiss:
                    model.QueueDamageText(new DamageTextData(DamageTextType.HitMiss));
                    break;
                case StateAlteredType.HitCrit:
                    model.QueueDamageText(new DamageTextData(DamageTextType.HitCrit));
                    break;
                case StateAlteredType.StatusEffectApplied:
                {
                    PersistentStatus persistentStatus = stateAlteredInfo.persistentStatus;
                    if (persistentStatus == null) { break; }

                    QueueInformationalText(LocalizationNames.GetStatusEffectText(persistentStatus.GetStatusEffectType(), persistentStatus.IsIncrease()));
                    break;
                }
                case StateAlteredType.Dead:
                    // Note:  Resurrection isn't supported for enemies - the slide is gone once its fade completes
                    model.isTargeted = false;
                    model.isDefeated = true;
                    break;
                case StateAlteredType.FriendFound:
                    QueueInformationalText(model.friendFoundText);
                    break;
                case StateAlteredType.FriendIgnored:
                    QueueInformationalText(model.friendIgnoredText);
                    break;
                case StateAlteredType.ActionDequeued:
                    model.BlipGrow();
                    break;
            }
        }
        #endregion
    }
}
