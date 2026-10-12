using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Combat.UI
{
    public sealed class EnemySlideElement : Button
    {
        // Data sources:
        //  - EnemySlideModel (dataSource) for slide state
        //  - CombatParticipantModel (enemyDataSource) for the enemy's values

        // Const Tunables
        private const string _ussClassName = "enemy-slide";
        private const string _standardUssClassName = _ussClassName + "--standard";
        private const string _mookUssClassName = _ussClassName + "--mook";
        private const string _bossUssClassName = _ussClassName + "--boss";
        private const string _targetedUssClassName = _ussClassName + "--targeted";
        private const string _defeatedUssClassName = _ussClassName + "--defeated";
        private const string _shadowUssClassName = _ussClassName + "__shadow";
        private const string _imageUssClassName = _ussClassName + "__image";
        private const string _cooldownUssClassName = _ussClassName + "__cooldown";
        private const string _statusEffectsUssClassName = _ussClassName + "__status-effects";
        private const string _damageTextUssClassName = _ussClassName + "__damage-text";
        // Slide size per battle entity type, before the sprite's fine-tune scale
        private static readonly CustomStyleProperty<float> _widthProperty = new("--enemy-slide-width");
        private static readonly CustomStyleProperty<float> _heightProperty = new("--enemy-slide-height");
        private const float _defeatFadeTime = 1f;

        // State
        private readonly float sizeScale;
        private bool internalIsTargeted = false;
        private bool internalIsDefeated = false;
        private EnemySlideModel subscribedModel;
        private float defeatTime;
        public int column { get; }

        // Cached References
        private readonly VisualElement image;
        private readonly VisualElement shadow;
        private readonly StatusEffectPanelElement statusEffectPanel;
        private readonly DamageTextSpawnerElement damageTextSpawner;
        private readonly SlideEffects slideEffects;
        private readonly IVisualElementScheduledItem defeatUpdate;

        // Events
        public event Action defeatFadeCompleted;

        [CreateProperty] public bool isTargeted
        {
            get => internalIsTargeted;
            set
            {
                internalIsTargeted = value;
                EnableInClassList(_targetedUssClassName, value);
                slideEffects.SetPulsing(value && !internalIsDefeated);
            }
        }

        [CreateProperty] public bool isDefeated
        {
            get => internalIsDefeated;
            set
            {
                if (internalIsDefeated == value) { return; }
                internalIsDefeated = value;
                EnableInClassList(_defeatedUssClassName, value);
                if (!value) { return; }

                pickingMode = PickingMode.Ignore;
                slideEffects.SetPulsing(false);
                defeatTime = 0f;
                defeatUpdate.Resume();
            }
        }

        public object enemyDataSource
        {
            set => statusEffectPanel.dataSource = value;
        }

        public EnemySlideElement(BattleEntity battleEntity, Material targetShadowMaterial)
        {
            focusable = false; // Keyboard/gamepad input handled via IInputReceiver - avoid UITK navigation double-firing
            column = battleEntity.column;
            sizeScale = battleEntity.spriteScaleFineTune;
            AddToClassList(_ussClassName);
            AddToClassList(battleEntity.battleEntityType switch
            {
                BattleEntityType.Mook => _mookUssClassName,
                BattleEntityType.Boss => _bossUssClassName,
                _ => _standardUssClassName
            });

            // Note:  The shadow is the sprite drawn in a flat colour by its material - skipped without one
            shadow = AddPart(new VisualElement(), _shadowUssClassName);
            if (targetShadowMaterial != null)
            {
                shadow.style.backgroundImage = new StyleBackground(battleEntity.combatSprite);
                shadow.style.unityMaterial = new StyleMaterialDefinition(targetShadowMaterial);
            }
            image = AddPart(new VisualElement(), _imageUssClassName);
            image.style.backgroundImage = new StyleBackground(battleEntity.combatSprite);

            AddPart(new CooldownTimerElement(nameof(BattleSlideModel.cooldown)), _cooldownUssClassName);
            statusEffectPanel = AddPart(new StatusEffectPanelElement(nameof(CombatParticipantModel.statusEffects)), _statusEffectsUssClassName);
            damageTextSpawner = AddPart(new DamageTextSpawnerElement(nameof(BattleSlideModel.hitMissText), nameof(BattleSlideModel.hitCritText)), _damageTextUssClassName);

            slideEffects = new SlideEffects(this, true);
            defeatUpdate = schedule.Execute(UpdateDefeatFade).Every(0);
            defeatUpdate.Pause();

            SetBinding(nameof(isTargeted), UIToolkitBindings.ToTarget(nameof(EnemySlideModel.isTargeted)));
            SetBinding(nameof(isDefeated), UIToolkitBindings.ToTarget(nameof(EnemySlideModel.isDefeated)));
            RegisterCallback<CustomStyleResolvedEvent>(HandleCustomStyleResolved);
            RegisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(_ => SubscribeToModel(null));
        }

        #region PrivateMethods
        private T AddPart<T>(T part, string partUssClassName) where T : VisualElement
        {
            part.AddToClassList(partUssClassName);
            part.pickingMode = PickingMode.Ignore;
            Add(part);
            return part;
        }

        private void HandleCustomStyleResolved(CustomStyleResolvedEvent customStyleResolvedEvent)
        {
            ICustomStyle newStyle = customStyleResolvedEvent.customStyle;
            if (newStyle.TryGetValue(_widthProperty, out float resolvedWidth)) { style.width = resolvedWidth * sizeScale; }
            if (newStyle.TryGetValue(_heightProperty, out float resolvedHeight)) { style.height = resolvedHeight * sizeScale; }
        }

        private void HandleAttachToPanel(AttachToPanelEvent attachToPanelEvent)
        {
            SubscribeToModel(dataSource as EnemySlideModel);
            if (parent is EnemySlideRow enemySlideRow) { schedule.Execute(enemySlideRow.SortByColumn); } // Deferred 1 frame:  avoid re-ordering the hierarchy mid-attach
        }

        private void SubscribeToModel(EnemySlideModel model)
        {
            if (subscribedModel != null) { subscribedModel.damageTextQueued -= damageTextSpawner.Enqueue; }
            subscribedModel = model;
            if (subscribedModel != null) { subscribedModel.damageTextQueued += damageTextSpawner.Enqueue; }
            slideEffects.Subscribe(subscribedModel);
        }

        private void UpdateDefeatFade()
        {
            defeatTime += Time.deltaTime;
            image.style.opacity = 1f - Mathf.Clamp01(defeatTime / _defeatFadeTime);
            if (defeatTime < _defeatFadeTime) { return; }

            defeatUpdate.Pause();
            defeatFadeCompleted?.Invoke();
        }
        #endregion
    }
}
