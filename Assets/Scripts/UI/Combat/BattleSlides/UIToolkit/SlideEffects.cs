using UnityEngine;
using UnityEngine.UIElements;

namespace Frankie.Combat.UI
{
    public sealed class SlideEffects
    {
        // Note:  Requested via a BattleSlideModel's effect events

        // Const Tunables
        private const float _damageShakeMagnitude = 10f;
        private const float _criticalDamageShakeMultiplier = 2f;
        private const float _shakeDuration = 0.4f;
        private const int _shakeCount = 4;
        private const float _dimmingMin = 0.7f;
        private const float _halfDimmingTime = 0.1f;
        private const float _growingMax = 1.2f;
        private const float _halfGrowingTime = 0.125f;
        private const float _halfPulsingTime = 0.15f;
        private const float _pulsingOpaqueHoldTime = 0.2f;
        private const float _pulsingMinOpacity = 0.5f;

        // State
        private BattleSlideModel subscribedModel;
        private float shakeTime = Mathf.Infinity;
        private float shakeStepTime;
        private float shakeMagnitude;
        private float lastRotationTarget;
        private float currentRotationTarget;
        private float dimTime = Mathf.Infinity;
        private float growTime = Mathf.Infinity;
        private bool isPulsing = false;
        private float pulseTime;

        // Cached References
        private readonly VisualElement target;
        private readonly bool growVertically;
        private readonly IVisualElementScheduledItem effectsUpdate;

        public SlideEffects(VisualElement target, bool growVertically)
        {
            this.target = target;
            this.growVertically = growVertically;
            effectsUpdate = target.schedule.Execute(UpdateEffects).Every(0);
            effectsUpdate.Pause();
        }

        #region PublicMethods
        public void Subscribe(BattleSlideModel model)
        {
            if (subscribedModel != null)
            {
                subscribedModel.shakeRequested -= StartShake;
                subscribedModel.dimBlipRequested -= StartDim;
                subscribedModel.growBlipRequested -= StartGrow;
            }
            subscribedModel = model;
            if (subscribedModel == null) { return; }

            subscribedModel.shakeRequested += StartShake;
            subscribedModel.dimBlipRequested += StartDim;
            subscribedModel.growBlipRequested += StartGrow;
        }

        public void SetPulsing(bool enable)
        {
            if (isPulsing == enable) { return; }
            isPulsing = enable;
            pulseTime = 0f;
            effectsUpdate.Resume();
        }
        #endregion

        #region PrivateMethods
        private void StartShake(bool isStrong)
        {
            shakeMagnitude = _damageShakeMagnitude * (isStrong ? _criticalDamageShakeMultiplier : 1f);
            SetShakeTarget();
            shakeTime = 0f;
            shakeStepTime = 0f;
            effectsUpdate.Resume();
        }

        private void StartDim()
        {
            dimTime = 0f;
            effectsUpdate.Resume();
        }

        private void StartGrow()
        {
            growTime = 0f;
            effectsUpdate.Resume();
        }

        private void UpdateEffects()
        {
            float deltaTime = Time.deltaTime;
            bool isShaking = UpdateShake(deltaTime);
            bool isFading = UpdateOpacity(deltaTime);
            bool isGrowing = UpdateGrow(deltaTime);
            if (!isShaking && !isFading && !isGrowing) { effectsUpdate.Pause(); }
        }

        private bool UpdateShake(float deltaTime)
        {
            if (shakeTime > _shakeDuration)
            {
                target.style.rotate = StyleKeyword.Null;
                return false;
            }

            const float stepDuration = _shakeDuration / _shakeCount;
            if (shakeStepTime > stepDuration)
            {
                SetShakeTarget();
                shakeStepTime = 0f;
            }
            target.style.rotate = new Rotate(Mathf.Lerp(lastRotationTarget, currentRotationTarget, shakeStepTime / stepDuration));

            shakeStepTime += deltaTime;
            shakeTime += deltaTime;
            return true;
        }

        private void SetShakeTarget()
        {
            shakeMagnitude = Mathf.Max(0f, shakeMagnitude - _damageShakeMagnitude / _shakeCount);
            lastRotationTarget = currentRotationTarget;
            currentRotationTarget = Random.Range(-shakeMagnitude, shakeMagnitude);
        }

        // Note:  A dim blip takes over the opacity from the targeting pulse while it runs
        private bool UpdateOpacity(float deltaTime)
        {
            bool isDimming = dimTime < 2f * _halfDimmingTime;
            if (isPulsing) { pulseTime += deltaTime; }

            if (isDimming)
            {
                dimTime += deltaTime;
                float dimProgress = dimTime < _halfDimmingTime ? dimTime / _halfDimmingTime : 2f - dimTime / _halfDimmingTime;
                target.style.opacity = Mathf.Lerp(1f, _dimmingMin, Mathf.Clamp01(dimProgress));
                return true;
            }
            if (isPulsing)
            {
                // Fade out, fade back in, hold opaque
                float cycleTime = Mathf.Repeat(pulseTime, 2f * _halfPulsingTime + _pulsingOpaqueHoldTime);
                float pulseProgress = cycleTime < _halfPulsingTime ? cycleTime / _halfPulsingTime : 2f - cycleTime / _halfPulsingTime;
                target.style.opacity = Mathf.Lerp(1f, _pulsingMinOpacity, Mathf.Clamp01(pulseProgress));
                return true;
            }

            target.style.opacity = StyleKeyword.Null;
            return false;
        }

        private bool UpdateGrow(float deltaTime)
        {
            if (growTime >= 2f * _halfGrowingTime)
            {
                target.style.scale = StyleKeyword.Null;
                return false;
            }

            growTime += deltaTime;
            float growProgress = growTime < _halfGrowingTime ? growTime / _halfGrowingTime : 2f - growTime / _halfGrowingTime;
            float growScale = Mathf.Lerp(1f, _growingMax, Mathf.Clamp01(growProgress));
            target.style.scale = new Scale(new Vector2(growScale, growVertically ? growScale : 1f));
            return true;
        }
        #endregion
    }
}
