using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat.UI
{
    public class DamageTextSpawner : MonoBehaviour
    {
        // Tunables
        [Header("Text Qualities")]
        [SerializeField] Color loseHPTextColor = Color.red;
        [SerializeField] Color gainHPTextColor = Color.green;
        [SerializeField] Color loseAPTextColor = Color.magenta;
        [SerializeField] Color gainAPTextColor = Color.blue;
        [SerializeField] Color hitMissColor = Color.gray;
        [SerializeField] string hitMissText = "miss";
        [SerializeField] Color hitCritColor = Color.yellow;
        [SerializeField] string hitCritText = "CRIT!";
        [Header("Other Tunables")]
        [SerializeField] DamageText damageTextPrefab = null;
        [SerializeField] float minimumTimeBetweenSpawns = 0.1f;

        // State
        float timeSinceLastSpawn = Mathf.Infinity;
        Queue<DamageTextData> damageTextQueue = new Queue<DamageTextData>();

        // Methods
        private void FixedUpdate()
        {
            if (damageTextQueue.Count == 0) { return; }

            if ( timeSinceLastSpawn > minimumTimeBetweenSpawns)
            {
                Spawn(damageTextQueue.Dequeue());

                if (damageTextQueue.Count == 0) { timeSinceLastSpawn = Mathf.Infinity; }
                else { timeSinceLastSpawn = 0f; }
            }
            timeSinceLastSpawn += Time.deltaTime;
        }

        public void AddToQueue(DamageTextData damageTextData)
        {
            // Sanity check on data validity
            if (damageTextData.damageTextType == DamageTextType.HealthChanged && Mathf.Approximately(damageTextData.amount, 0f)) { return; }
            else if (damageTextData.damageTextType == DamageTextType.APChanged && Mathf.Approximately(damageTextData.amount, 0f)) { return; }

            damageTextQueue.Enqueue(damageTextData);
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
    }
}