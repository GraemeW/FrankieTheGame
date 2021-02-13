using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

        private void Awake()
        {
            battleController = GameObject.FindGameObjectWithTag("BattleController").GetComponent<BattleController>();
        }

        protected virtual void OnEnable()
        {
            // implemented in child classes
        }

        protected virtual void OnDisable()
        {
            if (combatParticipant != null)
            {
                combatParticipant.stateAltered -= ParseState;
            }
        }

        private void FixedUpdate()
        {
            HandleSlideShaking();
        }

        private void HandleSlideShaking()
        {
            if (currentShakeTime > shakeDuration) { return; }

            if (currentShakeTimeStep > (shakeDuration / shakeCount))
            {
                SetTargetShakeRotation();
                currentShakeTimeStep = 0f;
            }
            gameObject.transform.rotation = Quaternion.Euler(0f,0f, Mathf.Lerp(lastRotationTarget, currentRotationTarget, currentShakeTimeStep / (shakeDuration / shakeCount)));

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

        protected void HighlightSlide(CombatParticipant combatParticipant)
        {
            if (combatParticipant == this.combatParticipant)
            {
                SetSelected(true);
            }
            else
            {
                SetSelected(false);
            }
        }

        protected void ShakeSlide(bool strongShakeEnable)
        {
            currentShakeMagnitude = damageShakeMagnitude;
            if (strongShakeEnable) { currentShakeMagnitude *= criticalDamageShakeMultiplier; }
            SetTargetShakeRotation();
            currentShakeTime = 0f;
        }

        protected virtual void SetSelected(bool enable)
        {
            // implemented in child classes
        }

        protected virtual void ParseState(CombatParticipant combatParticipant, StateAlteredData stateAlteredData)
        {
            // implemented in child classes
        }
    }
}