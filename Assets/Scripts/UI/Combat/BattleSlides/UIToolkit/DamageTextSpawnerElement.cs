using System.Collections.Generic;
using System.Globalization;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Combat.UI
{
    public sealed class DamageTextSpawnerElement : VisualElement
    {
        // Const Tunables
        private const string _ussClassName = "damage-text-spawner";
        private const string _hpLossUssClassName = "damage-text--hp-loss";
        private const string _hpGainUssClassName = "damage-text--hp-gain";
        private const string _apLossUssClassName = "damage-text--ap-loss";
        private const string _apGainUssClassName = "damage-text--ap-gain";
        private const string _hitMissUssClassName = "damage-text--hit-miss";
        private const string _hitCritUssClassName = "damage-text--hit-crit";
        private const string _informationalUssClassName = "damage-text--informational";
        private const float _minimumTimeBetweenSpawns = 0.3f; // Note:  Must be set long enough for simultaneous effects (e.g. crit + HP + AP) to read separately

        // State
        private readonly Queue<DamageTextData> damageTextQueue = new();
        private float lastSpawnTime = Mathf.NegativeInfinity;

        // Cached References
        private readonly IVisualElementScheduledItem spawnUpdate;

        [CreateProperty] public string hitMissText { get; set; } = "";
        [CreateProperty] public string hitCritText { get; set; } = "";

        public DamageTextSpawnerElement(string hitMissSourcePropertyName, string hitCritSourcePropertyName)
        {
            AddToClassList(_ussClassName);
            pickingMode = PickingMode.Ignore;
            spawnUpdate = schedule.Execute(UpdateSpawns).Every(0);
            spawnUpdate.Pause();
            SetBinding(nameof(hitMissText), UIToolkitBindings.ToTarget(hitMissSourcePropertyName));
            SetBinding(nameof(hitCritText), UIToolkitBindings.ToTarget(hitCritSourcePropertyName));
        }

        #region PublicMethods
        public void Enqueue(DamageTextData damageTextData)
        {
            if (damageTextData.damageTextType is DamageTextType.HealthChanged or DamageTextType.APChanged && Mathf.Approximately(damageTextData.amount, 0f)) { return; }
            damageTextQueue.Enqueue(damageTextData);
            spawnUpdate.Resume();
        }
        #endregion

        #region PrivateMethods
        private void UpdateSpawns()
        {
            // Note:  Spacing is measured from the last spawn (not reset on an empty queue) - effects often arrive a frame apart
            if (damageTextQueue.Count == 0) { spawnUpdate.Pause(); return; }
            if (Time.time - lastSpawnTime < _minimumTimeBetweenSpawns) { return; }

            Spawn(damageTextQueue.Dequeue());
            lastSpawnTime = Time.time;
        }

        private void Spawn(DamageTextData damageTextData)
        {
            float amount = damageTextData.amount;
            (string text, string modifierUssClassName) = damageTextData.damageTextType switch
            {
                DamageTextType.HealthChanged => (Mathf.Abs(Mathf.RoundToInt(amount)).ToString(CultureInfo.InvariantCulture), amount < 0 ? _hpLossUssClassName : _hpGainUssClassName),
                DamageTextType.APChanged => (amount > 0 ? $"+{amount:n0}" : $"{amount:n0}", amount < 0 ? _apLossUssClassName : _apGainUssClassName),
                DamageTextType.HitMiss => (hitMissText, _hitMissUssClassName),
                DamageTextType.HitCrit => (hitCritText, _hitCritUssClassName),
                DamageTextType.Informational => (damageTextData.information, _informationalUssClassName),
                _ => (null, null)
            };
            if (string.IsNullOrEmpty(text)) { return; }
            Add(new DamageTextElement(text, modifierUssClassName));
        }
        #endregion
    }
}
