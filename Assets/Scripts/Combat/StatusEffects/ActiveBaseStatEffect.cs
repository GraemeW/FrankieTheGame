using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [RequireComponent(typeof(CombatParticipant))]
    public class ActiveBaseStatEffect : MonoBehaviour, IModifierProvider
    {
        // State
        Stat stat;
        float value = 0f;
        float duration = 0f;
        float timer = 0f;

        private void Update()
        {
            UpdateTimers();
        }

        public void Setup(Stat stat, float value, float duration)
        {
            this.stat = stat;
            this.value = value;
            this.duration = duration;
        }

        private void UpdateTimers()
        {
            timer += Time.deltaTime;

            if (timer > duration)
            {
                Destroy(this);
            }
        }

        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            if (this.stat == stat)
            {
                yield return value;
            }
            yield break;
        }
    }

}
