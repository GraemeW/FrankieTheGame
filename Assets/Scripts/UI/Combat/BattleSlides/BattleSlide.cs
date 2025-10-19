using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Frankie.Combat.UI
{
    public abstract class BattleSlide : MonoBehaviour
    {
        // Tunables
        [Header("Hook-Ups")]
        [SerializeField] protected CooldownTimer cooldownTimer;
        [SerializeField] protected Transform statusEffectPanel;
        [SerializeField] protected DamageTextSpawner damageTextSpawner;
        [SerializeField] protected CanvasGroup canvasGroup;

        [Header("Status Effects")]
        [SerializeField] private int maxStatusEffectToShow = 8;
        [SerializeField] private StatusEffectBobble statusEffectBobblePrefab;

        [Header("Shake Effects")]
        [SerializeField] private float damageShakeMagnitude = 10f;
        [SerializeField] private float criticalDamageShakeMultiplier = 2.0f;
        [SerializeField] private float shakeDuration = 0.4f;
        [SerializeField] private int shakeCount = 4;

        [Header("Dimming Effects")]
        [SerializeField] private float dimmingMin = 0.7f;
        [SerializeField][Tooltip("in seconds")] private float halfDimmingTime = 0.05f;

        // State
        protected BattleEntity battleEntity;
        private Coroutine canvasDimming;
        private float currentShakeMagnitude;
        private float currentShakeTime = Mathf.Infinity;
        private float currentShakeTimeStep;
        private float lastRotationTarget;
        private float currentRotationTarget;
        private float fadeTarget = 1f;
        
        // Battle State
        private BattleEntity selectedCharacter;
        private IBattleActionSuper selectedBattleActionSuper;
        private IList<BattleEntity> cachedCharacters;
        private IList<BattleEntity> cachedEnemies;

        // Cached References
        protected Button button;

        #region UnityMethods
        protected virtual void Awake()
        {
            button = GetComponent<Button>();
        }

        protected virtual void OnEnable()
        {
            if (battleEntity != null) { battleEntity.combatParticipant.SubscribeToStateUpdates(ParseState); }

            if (BattleEventBus.inBattle) { SetupBattleListeners(); }
            else { BattleEventBus<BattleEnterEvent>.SubscribeToEvent(SetupBattleListeners); }
        }

        protected virtual void OnDisable()
        {
            RemoveButtonClickEvents();

            if (canvasDimming != null) { StopCoroutine(canvasDimming); canvasDimming = null; }
            battleEntity?.combatParticipant.UnsubscribeToStateUpdates(ParseState);
            SetupBattleListeners(false);
        }

        private void FixedUpdate()
        {
            HandleSlideShaking();
            HandleSlideFading();
        }
        #endregion

        #region AbstractMethods
        protected abstract void SetSelected(CombatParticipantType combatParticipantType, bool enable);

        protected abstract void ParseState(StateAlteredInfo stateAlteredInfo);

        #endregion

        #region PublicSettersGetters
        public virtual void SetBattleEntity(BattleEntity setBattleEntity)
        {
            battleEntity?.combatParticipant.UnsubscribeToStateUpdates(ParseState);

            battleEntity = setBattleEntity;
            InitializeStatusEffectBobbles();
            battleEntity.combatParticipant.SubscribeToStateUpdates(ParseState);
        }

        public virtual void AddButtonClickEvent(UnityAction unityAction)
        {
            button.onClick.AddListener(unityAction);
        }

        public void RemoveButtonClickEvents()
        {
            button.onClick.RemoveAllListeners();
        }

        public BattleEntity GetBattleEntity()
        {
            return battleEntity;
        }
        #endregion

        #region PublicMethodsOther
        public void HighlightSlide(CombatParticipantType combatParticipantType, IEnumerable<BattleEntity> battleEntities)
        {
            SetSelected(combatParticipantType, false);
            if (battleEntities == null) { return; }

            foreach (BattleEntity tempBattleEntity in battleEntities)
            {
                if (tempBattleEntity.combatParticipant == battleEntity.combatParticipant)
                {
                    SetSelected(combatParticipantType, true);
                }
            }
        }

        public void HighlightSlide(CombatParticipantType combatParticipantType, bool enable)
        {
            SetSelected(combatParticipantType, enable);
        }

        protected void ShakeSlide(bool strongShakeEnable)
        {
            currentShakeMagnitude = damageShakeMagnitude;
            if (strongShakeEnable) { currentShakeMagnitude *= criticalDamageShakeMultiplier; }
            SetTargetShakeRotation();
            currentShakeTime = 0f;
        }

        protected void BlipFadeSlide()
        {
            if (canvasDimming != null)
            {
                StopCoroutine(canvasDimming);
                canvasGroup.alpha = 1.0f;
                canvasDimming = null;
            }

            canvasDimming = StartCoroutine(BlipFade());
        }
        #endregion

        #region EventBusHandlers
        private void SetupBattleListeners(BattleEnterEvent battleEnterEvent = null)
        {
            SetupBattleListeners(true);
        }

        private void SetupBattleListeners(bool enable)
        {
            if (enable)
            {
                BattleEventBus<BattleStateChangedEvent>.SubscribeToEvent(HandleBattleStateChangedEvent);
                BattleEventBus<BattleEntitySelectedEvent>.SubscribeToEvent(HandleBattleEntitySelectedEvent);
                BattleEventBus<BattleActionSelectedEvent>.SubscribeToEvent(HandleBattleActionSelectedEvent);
                AddButtonClickEvent(delegate { HandleClickInBattle(); });
            }
            else
            {
                BattleEventBus<BattleEnterEvent>.UnsubscribeFromEvent(SetupBattleListeners);
                BattleEventBus<BattleStateChangedEvent>.UnsubscribeFromEvent(HandleBattleStateChangedEvent);
                BattleEventBus<BattleEntitySelectedEvent>.UnsubscribeFromEvent(HandleBattleEntitySelectedEvent);
                BattleEventBus<BattleActionSelectedEvent>.UnsubscribeFromEvent(HandleBattleActionSelectedEvent);
            }
        }

        private void HandleBattleActionSelectedEvent(BattleActionSelectedEvent battleActionSelectedEvent)
        {
            selectedBattleActionSuper = battleActionSelectedEvent.battleActionSuper;
        }

        private void HandleBattleEntitySelectedEvent(BattleEntitySelectedEvent battleEntitySelectedEvent)
        {
            HighlightSlide(battleEntitySelectedEvent.combatParticipantType, battleEntitySelectedEvent.battleEntities);

            if (battleEntitySelectedEvent.combatParticipantType == CombatParticipantType.Friendly)
            {
                selectedCharacter = battleEntitySelectedEvent.battleEntities.FirstOrDefault();
            }
        }

        private void HandleBattleStateChangedEvent(BattleStateChangedEvent battleStateChangedEvent)
        {
            if (battleStateChangedEvent.battleState != BattleState.Combat) { return; }
            cachedCharacters = battleStateChangedEvent.characters;
            cachedEnemies = battleStateChangedEvent.enemies;
        }
        #endregion
        
        #region PrivateMethods

        protected virtual bool HandleClickInBattle()
        {
            if (selectedCharacter == null || selectedBattleActionSuper == null) { return false; }
            if (cachedCharacters == null || cachedEnemies == null) { return false; }

            var battleActionData = new BattleActionData(selectedCharacter.combatParticipant); 
            battleActionData.SetTargets(new List<BattleEntity> { battleEntity });
            
            var localCharacters = new List<BattleEntity>(cachedCharacters);
            var localEnemies = new List<BattleEntity>(cachedEnemies);
            selectedBattleActionSuper.GetTargets(null, battleActionData, localCharacters, localEnemies); // Select targets with null traverse to apply filters & pass back
            
            if (battleActionData.targetCount == 0) { return false; }
            
            var battleSequence = new BattleSequence(selectedBattleActionSuper, battleActionData);
            BattleEventBus<BattleQueueUpdatedEvent>.Raise(new BattleQueueUpdatedEvent(battleSequence));
            
            return true;
        }

        private void InitializeStatusEffectBobbles()
        {
            if (battleEntity == null) { return; }

            PersistentStatus[] persistentStatuses = battleEntity.combatParticipant.GetComponents<PersistentStatus>();
            foreach (PersistentStatus persistentStatus in persistentStatuses)
            {
                AddStatusEffectBobble(persistentStatus, true); // Skip capping during loop
            }
            CapVisibleStatusEffects(); // Cap at end
        }

        protected void AddStatusEffectBobble(PersistentStatus persistentStatus, bool skipCap = false)
        {
            StatusEffectBobble statusEffectBobble = Instantiate(statusEffectBobblePrefab, statusEffectPanel);
            statusEffectBobble.Setup(persistentStatus);
            if (!skipCap) { CapVisibleStatusEffects(); }
        }

        private void CapVisibleStatusEffects()
        {
            int statusEffectCount = 0;
            foreach (Transform child in statusEffectPanel)
            {
                statusEffectCount++;
                if (statusEffectCount > maxStatusEffectToShow)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private void HandleSlideShaking()
        {
            if (currentShakeTime > shakeDuration) { return; }

            if (currentShakeTimeStep > (shakeDuration / shakeCount))
            {
                SetTargetShakeRotation();
                currentShakeTimeStep = 0f;
            }
            gameObject.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(lastRotationTarget, currentRotationTarget, currentShakeTimeStep / (shakeDuration / shakeCount)));

            currentShakeTimeStep += Time.deltaTime;
            currentShakeTime += Time.deltaTime;
        }

        private void SetTargetShakeRotation()
        {
            currentShakeMagnitude = Mathf.Max(0f, currentShakeMagnitude - damageShakeMagnitude / shakeCount);
            lastRotationTarget = currentRotationTarget;
            currentRotationTarget = Random.Range(-currentShakeMagnitude, currentShakeMagnitude);
        }

        private IEnumerator BlipFade()
        {
            fadeTarget = dimmingMin;
            yield return new WaitForSeconds(halfDimmingTime);
            fadeTarget = 1.0f;
        }

        private void HandleSlideFading()
        {
            if (Mathf.Approximately(canvasGroup.alpha, fadeTarget)) { return; }

            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, fadeTarget, (1f - dimmingMin) / halfDimmingTime);
        }
        #endregion
    }
}
