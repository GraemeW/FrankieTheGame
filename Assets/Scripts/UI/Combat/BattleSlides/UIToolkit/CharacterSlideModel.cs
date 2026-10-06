using System;
using Unity.Properties;
using LowDefMustard.UIBox;

namespace Frankie.Combat.UI
{
    public sealed class CharacterSlideModel : BindableModel
    {
        // Slide-only bound state (labels, highlight, cooldown) - character values come from a CombatParticipantModel
        
        // State
        private string internalHPLabel = "";
        private string internalAPLabel = "";
        private string internalHitMissText = "";
        private string internalHitCritText = "";
        private CharacterSlideState internalSlideState = CharacterSlideState.Ready;
        private CooldownTiming internalCooldown = CooldownTiming.Restart(0f);

        // Events
        public event Action<DamageTextData> damageTextQueued;
        public event Action<bool> shakeRequested; // true -> strong (critical) shake
        public event Action dimBlipRequested;
        public event Action growBlipRequested;

        #region BoundProperties
        [CreateProperty] public string hpLabel
        {
            get => internalHPLabel;
            set => SetProperty(ref internalHPLabel, value);
        }

        [CreateProperty] public string apLabel
        {
            get => internalAPLabel;
            set => SetProperty(ref internalAPLabel, value);
        }

        [CreateProperty] public string hitMissText
        {
            get => internalHitMissText;
            set => SetProperty(ref internalHitMissText, value);
        }

        [CreateProperty] public string hitCritText
        {
            get => internalHitCritText;
            set => SetProperty(ref internalHitCritText, value);
        }

        [CreateProperty] public CharacterSlideState slideState
        {
            get => internalSlideState;
            set => SetProperty(ref internalSlideState, value);
        }

        [CreateProperty] public CooldownTiming cooldown
        {
            get => internalCooldown;
            set => SetProperty(ref internalCooldown, value);
        }
        #endregion

        #region Effects
        public void QueueDamageText(DamageTextData damageTextData) => damageTextQueued?.Invoke(damageTextData);
        public void Shake(bool isStrong) => shakeRequested?.Invoke(isStrong);
        public void BlipDim() => dimBlipRequested?.Invoke();
        public void BlipGrow() => growBlipRequested?.Invoke();
        #endregion
    }
}
