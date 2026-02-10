using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat.UI
{
    public class DamageTextSpawner : MonoBehaviour
    {
        // Tunables
        [Header("Text Qualities")]
        [SerializeField] private Color loseHPTextColor = Color.red;
        [SerializeField] private Color gainHPTextColor = Color.green;
        [SerializeField] private Color loseAPTextColor = Color.magenta;
        [SerializeField] private Color gainAPTextColor = Color.blue;
        [SerializeField] private Color hitMissColor = Color.gray;
        [SerializeField] private string hitMissText = "miss";
        [SerializeField] private Color hitCritColor = Color.yellow;
        [SerializeField] private string hitCritText = "CRIT!";
        [SerializeField] private Color informationalTextColor = Color.gray;

        [Header("Other Tunables")]
        [SerializeField] private DamageText damageTextPrefab;
        [SerializeField] private float minimumTimeBetweenSpawns = 0.1f;

        // State
        private float timeSinceLastSpawn = Mathf.Infinity;
        private readonly Queue<DamageTextData> damageTextQueue = new();

        // Methods
        private void FixedUpdate()
        {
            if (damageTextQueue.Count == 0) { return; }

            if (timeSinceLastSpawn > minimumTimeBetweenSpawns)
            {
                if (Spawn(damageTextQueue.Dequeue()))
                {
                    timeSinceLastSpawn = damageTextQueue.Count == 0 ? Mathf.Infinity : 0f;
                }
            }
            timeSinceLastSpawn += Time.deltaTime;
        }

        public void AddToQueue(DamageTextData damageTextData)
        {
            switch (damageTextData.damageTextType)
            {
                case DamageTextType.HealthChanged when Mathf.Approximately(damageTextData.amount, 0f):
                case DamageTextType.APChanged when Mathf.Approximately(damageTextData.amount, 0f):
                    return;
                default:
                    damageTextQueue.Enqueue(damageTextData);
                    break;
            }
        }

        private bool Spawn(DamageTextData damageTextData)
        {
            DamageText damageTextInstance = Instantiate(damageTextPrefab, transform);

            return damageTextData.damageTextType switch
            {
                DamageTextType.HealthChanged => SpawnHealthChange(damageTextInstance, damageTextData.amount),
                DamageTextType.APChanged => SpawnAPChange(damageTextInstance, damageTextData.amount),
                DamageTextType.HitMiss => SpawnHitMiss(damageTextInstance),
                DamageTextType.HitCrit => SpawnHitCrit(damageTextInstance),
                DamageTextType.Informational => SpawnInformational(damageTextInstance, damageTextData.information),
                _ => false,
            };
        }


        private bool SpawnHealthChange(DamageText damageTextInstance, float amount)
        {
            damageTextInstance.SetText(amount);
            if (amount > 0)
            {
                damageTextInstance.SetColor(gainHPTextColor);
            }
            else if (amount < 0)
            {
                damageTextInstance.SetColor(loseHPTextColor);
            }
            return true;
        }

        private bool SpawnAPChange(DamageText damageTextInstance, float amount)
        {
            if (amount > 0)
            {
                damageTextInstance.SetText($"+{amount:n0}");
                damageTextInstance.SetColor(gainAPTextColor);
            }
            else if (amount < 0)
            {
                damageTextInstance.SetText($"{amount:n0}");
                damageTextInstance.SetColor(loseAPTextColor);
            }
            return true;
        }

        private bool SpawnHitMiss(DamageText damageTextInstance)
        {
            damageTextInstance.SetText(hitMissText);
            damageTextInstance.SetColor(hitMissColor);
            return true;
        }

        private bool SpawnHitCrit(DamageText damageTextInstance)
        {
            damageTextInstance.SetText(hitCritText);
            damageTextInstance.SetColor(hitCritColor);
            return true;
        }

        private bool SpawnInformational(DamageText damageTextInstance, string information)
        {
            damageTextInstance.SetText(information);
            damageTextInstance.SetColor(informationalTextColor);
            return true;
        }
    }
}
