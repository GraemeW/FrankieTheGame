using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Frankie.Combat.UI
{
    public class BattleTransition : MonoBehaviour
    {
        // Tunables
        [SerializeField] Image GoodEntry = null;
        [SerializeField] Image BadEntry = null;
        [SerializeField] Image NeutralEntry = null;
        [SerializeField] float fadeInDuration = 2.0f;

        // State
        Image currentBattleEntry = null;

        private void Start()
        {
            ResetBattleOverlays();
        }

        public void QueueBattleEntry(BattleEntryType battleEntryType)
        {
            UnityEngine.Debug.Log("DUDE???");
            if (battleEntryType == BattleEntryType.Good) 
            { 
                GoodEntry.gameObject.SetActive(true);
                currentBattleEntry = GoodEntry;
            }
            else if (battleEntryType == BattleEntryType.Bad) 
            { 
                BadEntry.gameObject.SetActive(true);
                currentBattleEntry = BadEntry;
            }
            else if (battleEntryType == BattleEntryType.Neutral) 
            { 
                NeutralEntry.gameObject.SetActive(true);
                currentBattleEntry = NeutralEntry;
            }
            currentBattleEntry.CrossFadeAlpha(0f, 0f, true);
            currentBattleEntry.CrossFadeAlpha(1, fadeInDuration, false);
        }

        private void ResetBattleOverlays()
        {
            GoodEntry.gameObject.SetActive(false);
            BadEntry.gameObject.SetActive(false);
            NeutralEntry.gameObject.SetActive(false);
        }
    }
}