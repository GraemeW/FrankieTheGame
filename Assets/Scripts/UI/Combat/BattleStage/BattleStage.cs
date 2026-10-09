using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Combat.UI
{
    [RequireComponent(typeof(UIToolkitMenuView))]
    public sealed class BattleStage : MonoBehaviour
    {
        // Note:  Builds itself from battle events;  overlay boxes (options, skills, messages) live elsewhere - see BattleCanvas

        // Tunables
        [SerializeField] private BattleSlideText battleSlideText;
        [SerializeField] private Material enemyTargetShadowMaterial;
        [SerializeField] private MovingBackgroundProperties defaultMovingBackgroundProperties;

        // State
        private readonly BattleStageModel stageModel = new();
        private readonly List<CharacterSlideHandle> characterSlides = new();
        private readonly Dictionary<BattleEntity, EnemySlideHandle> enemySlideLookup = new();
        private readonly List<BattleEffectHandle> effects = new();
        private BattleState battleState = BattleState.Inactive;
        private bool isFirstCharacterSelected = false;

        // Cached References
        private UIToolkitMenuView stageView;
        private BattleController battleController;

        #region UnityMethods
        private void Awake()
        {
            stageView = GetComponent<UIToolkitMenuView>();
            stageView.SetDataSource(stageModel);

            // Note:  Sprites are sized against the panel's own reference
            if (TryGetComponent(out PanelRenderer panelRenderer) && panelRenderer.panelSettings != null)
            {
                stageModel.spritePixelsPerUnit = panelRenderer.panelSettings.referenceSpritePixelsPerUnit;
            }
        }

        private void OnEnable()
        {
            BattleEventBus<BattleStagingEvent>.SubscribeToEvent(HandleBattleStagingEvent);
            BattleEventBus<BattleStateChangedEvent>.SubscribeToEvent(HandleBattleStateChangedEvent);
            BattleEventBus<BattleEntityAddedEvent>.SubscribeToEvent(HandleBattleEntityAddedEvent);
            BattleEventBus<BattleEntitySelectedEvent>.SubscribeToEvent(HandleBattleEntitySelectedEvent);
        }

        private void OnDisable()
        {
            BattleEventBus<BattleStagingEvent>.UnsubscribeFromEvent(HandleBattleStagingEvent);
            BattleEventBus<BattleStateChangedEvent>.UnsubscribeFromEvent(HandleBattleStateChangedEvent);
            BattleEventBus<BattleEntityAddedEvent>.UnsubscribeFromEvent(HandleBattleEntityAddedEvent);
            BattleEventBus<BattleEntitySelectedEvent>.UnsubscribeFromEvent(HandleBattleEntitySelectedEvent);
        }

        private void OnDestroy()
        {
            ClearEffects();
            foreach (CharacterSlideHandle characterSlide in characterSlides) { characterSlide.Release(); }
            characterSlides.Clear();
            foreach (EnemySlideHandle enemySlide in enemySlideLookup.Values) { enemySlide.Release(); }
            enemySlideLookup.Clear();
        }
        #endregion

        #region PublicMethods
        public void Setup(BattleController setBattleController)
        {
            battleController = setBattleController;
        }

        // Effect over an enemy's slide (matching its size);  non-positive lifetime = until its enemy is gone or the battle leaves combat
        public void SpawnEffect(BattleEntity enemy, Material effectMaterial, Color effectColour, float lifetime)
        {
            if (effectMaterial == null || enemy == null) { return; }
            if (!enemySlideLookup.TryGetValue(enemy, out EnemySlideHandle enemySlide) || !enemySlide.TryGetSlideElement(out VisualElement slideElement)) { return; }

            AddEffect(BattleEffectHandle.OverAnchor(stageView, slideElement, effectMaterial, effectColour, lifetime));
        }

        // Effect over the whole stage (bands + party slides included);  non-positive lifetime = until the battle leaves combat
        public void SpawnFullScreenEffect(Material effectMaterial, Color effectColour, float lifetime)
        {
            if (effectMaterial == null) { return; }
            AddEffect(BattleEffectHandle.FullScreen(stageView, effectMaterial, effectColour, lifetime));
        }
        #endregion

        #region EventHandlers
        private void HandleBattleStagingEvent(BattleStagingEvent battleStagingEvent)
        {
            if (battleStagingEvent.battleStagingType != BattleStagingType.BattleSetUp || !battleStagingEvent.optionalParametersSet) { return; }
            SetupBackground(battleStagingEvent.GetEnemyEntities());
        }

        private void HandleBattleStateChangedEvent(BattleStateChangedEvent battleStateChangedEvent)
        {
            battleState = battleStateChangedEvent.battleState;
            if (battleState is not (BattleState.PreCombat or BattleState.Combat)) { ClearEffects(); } // Effects don't outlive the fight (e.g. never-expiring ones)
        }

        private void HandleBattleEntityAddedEvent(BattleEntityAddedEvent battleEntityAddedEvent)
        {
            BattleEntity battleEntity = battleEntityAddedEvent.battleEntity;
            if (battleEntity == null || battleEntity.combatParticipant == null) { return; }
            if (battleEntity.isAssistCharacter) { return; } // No slide for assist characters

            if (battleEntity.isCharacter) { SetupCharacter(battleEntity); }
            else { SetupEnemy(battleEntity); }
        }

        private void HandleBattleEntitySelectedEvent(BattleEntitySelectedEvent battleEntitySelectedEvent)
        {
            foreach (CharacterSlideHandle characterSlide in characterSlides)
            {
                characterSlide.HighlightSlide(battleEntitySelectedEvent.selectionType, battleEntitySelectedEvent.battleEntities);
            }
            foreach (EnemySlideHandle enemySlide in enemySlideLookup.Values)
            {
                enemySlide.HighlightSlide(battleEntitySelectedEvent.selectionType, battleEntitySelectedEvent.battleEntities);
            }
        }
        #endregion

        #region PrivateMethods
        private void SetupBackground(IList<BattleEntity> enemies)
        {
            MovingBackgroundProperties movingBackgroundProperties = null;
            IList<CombatParticipant> viableEnemies = CombatParticipant.GetPriorityCombatParticipants(enemies);
            if (viableEnemies.Count > 0) { movingBackgroundProperties = viableEnemies[Random.Range(0, viableEnemies.Count)].GetMovingBackgroundProperties(); }
            if (movingBackgroundProperties == null || movingBackgroundProperties.tileSpriteImage == null || movingBackgroundProperties.shaderMaterial == null) { movingBackgroundProperties = defaultMovingBackgroundProperties; }

            stageModel.backgroundTile = movingBackgroundProperties.tileSpriteImage;
            stageModel.backgroundMaterial = movingBackgroundProperties.shaderMaterial;
        }

        private void SetupEnemy(BattleEntity enemy)
        {
            var enemySlide = new EnemySlideHandle(stageView, enemy, battleSlideText, enemyTargetShadowMaterial, () => HandleEnemySlideClicked(enemy));
            stageView.AddEntry(enemySlide);
            enemySlideLookup[enemy] = enemySlide;
        }

        private void SetupCharacter(BattleEntity character)
        {
            var characterSlide = new CharacterSlideHandle(stageView, character, battleSlideText);
            characterSlide.AddButtonClickEvent(() => HandleCharacterSlideClicked(character));
            stageView.AddEntry(characterSlide);
            characterSlides.Add(characterSlide);

            if (isFirstCharacterSelected || battleController == null) { return; }
            battleController.SetSelectedCharacter(character.combatParticipant);
            isFirstCharacterSelected = true;
        }

        private static void HandleEnemySlideClicked(BattleEntity enemy)
        {
            BattleEventBus<BattleQueueAddAttemptEvent>.Raise(new BattleQueueAddAttemptEvent(new List<BattleEntity> { enemy }));
        }

        private void HandleCharacterSlideClicked(BattleEntity character)
        {
            if (battleController == null || character.combatParticipant == null) { return; }

            // With no action selected, a click selects the character;  otherwise it targets the character with the action
            if (battleController.GetActiveBattleAction() == null)
            {
                if (battleState == BattleState.Combat) { battleController.SetSelectedCharacter(character.combatParticipant); }
                return;
            }
            BattleEventBus<BattleQueueAddAttemptEvent>.Raise(new BattleQueueAddAttemptEvent(new List<BattleEntity> { character }));
        }

        private void AddEffect(BattleEffectHandle effect)
        {
            effects.RemoveAll(existingEffect => !existingEffect.isAlive);
            effects.Add(effect);
            stageView.AddEntry(effect);
        }

        private void ClearEffects()
        {
            foreach (BattleEffectHandle effect in effects) { effect.Remove(); }
            effects.Clear();
        }
        #endregion
    }
}
