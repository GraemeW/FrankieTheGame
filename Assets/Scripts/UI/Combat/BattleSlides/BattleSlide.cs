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
        [SerializeField] protected DamageTextSpawner damageTextSpawner = null;
        [SerializeField] protected CanvasGroup canvasGroup = null;

        [Header("Shake Effects")]
        [SerializeField] float damageShakeMagnitude = 10f;
        [SerializeField] float criticalDamageShakeMultiplier = 2.0f;
        [SerializeField] float shakeDuration = 0.4f;
        [SerializeField] int shakeCount = 4;

        [Header("Dimming Effects")]
        [SerializeField] float dimmingMin = 0.7f;
        [SerializeField] [Tooltip("in seconds")] float halfDimmingTime = 0.05f;

        // State
        protected CombatParticipant combatParticipant = null;
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
        private void Awake()
        {
            button = GetComponent<Button>();
            battleController = GameObject.FindGameObjectWithTag("BattleController")?.GetComponent<BattleController>();
        }

        protected virtual void OnEnable()
        {
            if (combatParticipant != null)
            {
                combatParticipant.stateAltered += ParseState;
            }
            if (battleController != null)
            {
                battleController.selectedCombatParticipantChanged += HighlightSlide;
                AddButtonClickEvent( delegate{ TryAddBattleQueue(); });
            }
        }

        protected virtual void OnDisable()
        {
            button.onClick.RemoveAllListeners();

            if (canvasDimming != null) { StopCoroutine(canvasDimming); }
            canvasDimming = null;

            if (combatParticipant != null)
            {
                combatParticipant.stateAltered -= ParseState;
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
        public virtual void SetCombatParticipant(CombatParticipant combatParticipant)
        {
            if (this.combatParticipant != null) { this.combatParticipant.stateAltered -= ParseState; }

            this.combatParticipant = combatParticipant;
            this.combatParticipant.stateAltered += ParseState;
        }

        public virtual void AddButtonClickEvent(UnityAction unityAction)
        {
            button.onClick.AddListener(unityAction);
        }

        public CombatParticipant GetCombatParticipant()
        {
            return combatParticipant;
        }
        #endregion

        #region PublicMethodsOther
        public void HighlightSlide(CombatParticipantType combatParticipantType, IEnumerable<CombatParticipant> combatParticipants)
        {
            SetSelected(combatParticipantType, false);
            if (combatParticipants == null) { return; }

            foreach (CombatParticipant combatParticipant in combatParticipants)
            {
                if (combatParticipant == this.combatParticipant)
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
        private void TryAddBattleQueue()
        {
            if (battleController == null || !battleController.IsBattleActionArmed()) { return; }

            battleController.AddToBattleQueue(new[] { combatParticipant });
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