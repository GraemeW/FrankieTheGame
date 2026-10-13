using System;
using Unity.Properties;
using LowDefMustard.UIBox;

namespace Frankie.Combat.UI
{
    public abstract class BattleSlideModel : BindableModel
    {
        // Note:  Participant values come from a CombatParticipantModel

        // State
        private string internalHitMissText = "";
        private string internalHitCritText = "";
        private CooldownTiming internalCooldown = CooldownTiming.Restart(0f);

        // Events
        public event Action<DamageTextData> damageTextQueued;
        public event Action<bool> shakeRequested; // true -> strong (critical) shake
        public event Action dimBlipRequested;
        public event Action growBlipRequested;

        #region BoundProperties
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
