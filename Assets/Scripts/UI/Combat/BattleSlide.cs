using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Frankie.Combat.UI
{
    public class BattleSlide : MonoBehaviour
    {
        // Tunables
        [Header("Damage Effects")]
        float damageShakeMagnitude = 10f;
        float criticalDamageShakeMultiplier = 2.0f;
        float shakeDuration = 0.4f;
        int shakeCount = 4;

        // State
        protected CombatParticipant combatParticipant = null;
        float currentShakeMagnitude = 0f;
        float currentShakeTime = Mathf.Infinity;
        float currentShakeTimeStep = 0f;
        public float lastRotationTarget = 0f;
        public float currentRotationTarget = 0f;

        // Cached References
        protected BattleController battleController = null;
        protected Button button = null;

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
        }

        public virtual void AddButtonClickEvent(UnityAction unityAction)
        {
            button.onClick.AddListener(unityAction);
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

        public virtual void SetCombatParticipant(CombatParticipant combatParticipant)
        {
            if (this.combatParticipant != null) { this.combatParticipant.stateAltered -= ParseState; }

            this.combatParticipant = combatParticipant;
            this.combatParticipant.stateAltered += ParseState;
        }

        public CombatParticipant GetCombatParticipant()
        {
            return combatParticipant;
        }

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

        private void TryAddBattleQueue()
        {
            if (battleController == null || !battleController.IsBattleActionArmed()) { return; }

            battleController.AddToBattleQueue(new[] { combatParticipant });
        }

        protected virtual void SetSelected(CombatParticipantType combatParticipantType, bool enable)
        {
            // implemented in child classes
        }

        protected virtual void ParseState(CombatParticipant combatParticipant, StateAlteredData stateAlteredData)
        {
            // implemented in child classes
        }
    }
}