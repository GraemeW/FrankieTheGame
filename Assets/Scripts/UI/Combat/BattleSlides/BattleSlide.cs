using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Frankie.Combat.UI
{
    public abstract class BattleSlide : MonoBehaviour
    {
        // Tunables
        [Header("Hook-Ups")]
        [SerializeField] protected CooldownTimer cooldownTimer = null;
        [SerializeField] protected Transform statusEffectPanel = null;
        [SerializeField] protected DamageTextSpawner damageTextSpawner = null;
        [SerializeField] protected CanvasGroup canvasGroup = null;

        [Header("Status Effects")]
        [SerializeField] int maxStatusEffectToShow = 8;
        [SerializeField] StatusEffectBobble statusEffectBobblePrefab = null;

        [Header("Shake Effects")]
        [SerializeField] float damageShakeMagnitude = 10f;
        [SerializeField] float criticalDamageShakeMultiplier = 2.0f;
        [SerializeField] float shakeDuration = 0.4f;
        [SerializeField] int shakeCount = 4;

        [Header("Dimming Effects")]
        [SerializeField] float dimmingMin = 0.7f;
        [SerializeField] [Tooltip("in seconds")] float halfDimmingTime = 0.05f;

        // State
        protected BattleEntity battleEntity = null;
        Coroutine canvasDimming = null;
        float currentShakeMagnitude = 0f;
        float currentShakeTime = Mathf.Infinity;
        float currentShakeTimeStep = 0f;
        float lastRotationTarget = 0f;
        float currentRotationTarget = 0f;
        float fadeTarget = 1f;

        // Cached References
        protected BattleController battleController = null;
        protected Button button = null;

        #region UnityMethods
        protected virtual void Awake()
        {
            button = GetComponent<Button>();
            battleController = GameObject.FindGameObjectWithTag("BattleController")?.GetComponent<BattleController>();
        }

        protected virtual void OnEnable()
        {
            if (battleEntity != null)
            {
                battleEntity.combatParticipant.stateAltered += ParseState;
            }
            if (battleController != null)
            {
                battleController.selectedCombatParticipantChanged += HighlightSlide;
                AddButtonClickEvent( delegate{ HandleClickInBattle(); });
            }
        }

        protected virtual void OnDisable()
        {
            RemoveButtonClickEvents();

            if (canvasDimming != null) { StopCoroutine(canvasDimming); }
            canvasDimming = null;

            if (battleEntity != null)
            {
                battleEntity.combatParticipant.stateAltered -= ParseState;
            }
            if (battleController != null)
            {
                battleController.selectedCombatParticipantChanged -= HighlightSlide;
            }
        }

        private void FixedUpdate()
        {
            HandleSlideShaking();
            HandleSlideFading();
        }
        #endregion

        #region AbstractMethods
        protected abstract void SetSelected(CombatParticipantType combatParticipantType, bool enable);

        protected abstract void ParseState(CombatParticipant combatParticipant, StateAlteredData stateAlteredData);

        #endregion

        #region PublicSettersGetters
        public virtual void SetBattleEntity(BattleEntity battleEntity)
        {
            if (this.battleEntity != null) { this.battleEntity.combatParticipant.stateAltered -= ParseState; }

            this.battleEntity = battleEntity;
            InitializeStatusEffectBobbles();
            this.battleEntity.combatParticipant.stateAltered += ParseState;
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

            foreach (BattleEntity battleEntity in battleEntities)
            {
                if (battleEntity.combatParticipant == this.battleEntity.combatParticipant)
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

        #region PrivateMethods

        protected virtual bool HandleClickInBattle()
        {
            if (battleController == null || !battleController.HasActiveBattleAction()) { return false; }

            battleController.AddToBattleQueue(new List<BattleEntity> { battleEntity });
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

        protected void CapVisibleStatusEffects()
        {
            int statusEffectCount = 0;
            foreach(Transform child in statusEffectPanel)
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