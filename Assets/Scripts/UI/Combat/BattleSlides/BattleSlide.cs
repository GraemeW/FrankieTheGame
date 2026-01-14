using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Frankie.Stats;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

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
        [SerializeField][Range(0f, 1f)] private float dimmingMin = 0.7f;
        [SerializeField][Tooltip("in seconds")] private float halfDimmingTime = 0.1f;
        
        [Header("Growing Effects")]
        [SerializeField][Range(1f, 2f)] private float growingMax = 1.2f;
        [SerializeField][Tooltip("in seconds")] private float halfGrowingTime = 0.125f;
        [SerializeField] private bool xScaleGrow = true;
        [SerializeField] private bool yScaleGrow = true;

        // State
        protected BattleEntity battleEntity;
        private readonly List<StatusEffectBobble> statusEffectBobbles = new();
        private Coroutine slideDimming;
        private Coroutine slideGrowing;
        private float currentShakeMagnitude;
        private float currentShakeTime = Mathf.Infinity;
        private float currentShakeTimeStep;
        private float lastRotationTarget;
        private float currentRotationTarget;
        private float fadeTarget = 1f;
        private float xScaleTarget = 1f;
        private float yScaleTarget = 1f;

        // Cached References
        protected Button button;

        #region StaticMethods
        public static string GetStatusEffectText(PersistentStatus persistentStatus)
        {
            return persistentStatus.GetStatusEffectType() switch
            {
                Stat.HP => persistentStatus.IsIncrease() ? "+HP" : "-HP",
                Stat.AP => persistentStatus.IsIncrease() ? "+AP" : "-AP",
                Stat.Brawn => persistentStatus.IsIncrease() ? "STRONG" : "WEAK",
                Stat.Beauty => persistentStatus.IsIncrease() ? "FETCHING" : "FOUL",
                Stat.Smarts => persistentStatus.IsIncrease() ? "BRIGHT" : "DIM",
                Stat.Nimble => persistentStatus.IsIncrease() ? "FAST" : "SLOW",
                Stat.Luck => persistentStatus.IsIncrease() ? "BLESSED" : "JINXED",
                Stat.Pluck => persistentStatus.IsIncrease() ? "BRAVE" : "COWARD",
                Stat.Stoic => persistentStatus.IsIncrease() ? "STURDY" : "FRAIL",
                _ => ""
            };
        }
        #endregion
        
        #region UnityMethods
        protected virtual void Awake()
        {
            button = GetComponent<Button>();
        }

        protected virtual void OnEnable()
        {
            if (battleEntity != null) { battleEntity.combatParticipant.SubscribeToStateUpdates(ParseState); }
            SetupBattleListeners(true);
        }

        protected virtual void OnDisable()
        {
            RemoveButtonClickEvents();

            if (slideDimming != null) { StopCoroutine(slideDimming); slideDimming = null; }
            battleEntity?.combatParticipant.UnsubscribeToStateUpdates(ParseState);
            SetupBattleListeners(false);
        }

        private void FixedUpdate()
        {
            HandleSlideShaking();
            HandleSlideFading(Time.deltaTime);
            HandleSlideGrowing(Time.deltaTime);
        }
        #endregion

        #region AbstractMethods
        protected abstract void SetSelected(CombatParticipantType combatParticipantType, bool enable);

        protected abstract void ParseState(StateAlteredInfo stateAlteredInfo);

        #endregion

        #region PublicSettersGetters
        public BattleEntity GetBattleEntity() => battleEntity;
        
        public virtual void SetBattleEntity(BattleEntity setBattleEntity)
        {
            if (battleEntity != null) { battleEntity.combatParticipant.UnsubscribeToStateUpdates(ParseState); }

            battleEntity = setBattleEntity;
            InitializeStatusEffectBobbles();
            battleEntity.combatParticipant.SubscribeToStateUpdates(ParseState);
        }

        public void AddButtonClickEvent(UnityAction unityAction)
        {
            button.onClick.AddListener(unityAction);
        }

        public void RemoveButtonClickEvents()
        {
            button.onClick.RemoveAllListeners();
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
            if (slideDimming != null)
            {
                StopCoroutine(slideDimming);
                canvasGroup.alpha = 1.0f;
                slideDimming = null;
            }

            slideDimming = StartCoroutine(BlipFade());
        }

        protected void BlipGrowSlide()
        {
            if (slideGrowing != null)
            {
                StopCoroutine(slideGrowing);
                slideGrowing = null;
            }

            slideGrowing = StartCoroutine(BlipGrow());
        }
        #endregion

        #region EventBusHandlers
        private void SetupBattleListeners(bool enable)
        {
            if (enable)
            {
                AddButtonClickEvent(delegate { HandleClickInBattle(); });
                BattleEventBus<BattleEntitySelectedEvent>.SubscribeToEvent(HandleBattleEntitySelectedEvent);
            }
            else
            {
                BattleEventBus<BattleEntitySelectedEvent>.UnsubscribeFromEvent(HandleBattleEntitySelectedEvent);
            }
        }

        private void HandleBattleEntitySelectedEvent(BattleEntitySelectedEvent battleEntitySelectedEvent)
        {
            HighlightSlide(battleEntitySelectedEvent.combatParticipantType, battleEntitySelectedEvent.battleEntities);
        }
        #endregion
        
        #region PrivateMethods

        protected virtual bool HandleClickInBattle()
        {
            var targets = new List<BattleEntity> { battleEntity };
            BattleEventBus<BattleQueueAddAttemptEvent>.Raise(new BattleQueueAddAttemptEvent(targets));
            
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
            statusEffectBobbles.Add(statusEffectBobble);
            if (!skipCap) { CapVisibleStatusEffects(); }
        }

        protected void ClearStatusEffectBobbles()
        {
            foreach (StatusEffectBobble statusEffectBobble in statusEffectBobbles.Where(statusEffectBobble => statusEffectBobble != null))
            {
                statusEffectBobble.ForceClearBobble();
            }
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

        private void HandleSlideFading(float deltaTime)
        {
            if (Mathf.Approximately(canvasGroup.alpha, fadeTarget)) { return; }

            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, fadeTarget, (1f - dimmingMin) * deltaTime / halfDimmingTime);
        }

        private IEnumerator BlipGrow()
        {
            if (xScaleGrow) { xScaleTarget = growingMax; }
            if (yScaleGrow) { yScaleTarget = growingMax; }
            yield return new WaitForSeconds(halfGrowingTime);
            xScaleTarget = 1.0f;
            yScaleTarget = 1.0f;
        }

        private void HandleSlideGrowing(float deltaTime)
        {
            if (Mathf.Approximately(gameObject.transform.localScale.x, xScaleTarget) 
                && Mathf.Approximately(gameObject.transform.localScale.x, yScaleTarget))
            { return; }
            
            float xScale = Mathf.MoveTowards(gameObject.transform.localScale.x, xScaleTarget, (growingMax - 1f) * deltaTime /  halfGrowingTime);
            float yScale = Mathf.MoveTowards(gameObject.transform.localScale.y, yScaleTarget, (growingMax - 1f) * deltaTime /  halfGrowingTime);
            
            UnityEngine.Debug.Log($"{gameObject.name}: {xScale}, {yScale}");
            gameObject.transform.localScale = new Vector3(xScale, yScale, gameObject.transform.localScale.z);
        }
        #endregion
    }
}
