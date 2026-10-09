using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Combat.UI;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Spawn Target Prefab Effect", menuName = "BattleAction/Effects/Spawn Target Prefab Effect", order = 15)]
    public class SpawnTargetPrefabEffect : EffectStrategy
    {
        // Note:
        //  - Standard effect is sized to the recipient's slide and tinted by the effect colour (i.e. one material serves every colour variant)
        //  - Global effect covers the whole battle stage instead

        [SerializeField] private Material effectMaterial;
        [SerializeField] private Color effectColour = Color.white;
        [SerializeField] private bool isGlobalEffect = false;
        [SerializeField][Min(0f)] private float delayAfterSeconds = 0.5f;
        [SerializeField][Tooltip("Set to min to never destroy")][Min(0f)] private float destroyAfterSeconds = 2.0f;
        
        public override IEnumerator StartEffect(CombatParticipant sender, IList<BattleEntity> recipients, DamageType damageType)
        {
            BattleCanvas battleCanvas = BattleCanvas.FindBattleCanvas();
            BattleStage battleStage = battleCanvas != null ? battleCanvas.GetBattleStage() : null;
            if (battleStage == null) { yield break; }

            if (isGlobalEffect) { battleStage.SpawnFullScreenEffect(effectMaterial, effectColour, destroyAfterSeconds); }
            else if (recipients != null)
            {
                foreach (BattleEntity recipient in recipients)
                {
                    battleStage.SpawnEffect(recipient, effectMaterial, effectColour, destroyAfterSeconds);
                }
            }
            yield return new WaitForSeconds(Mathf.Max(delayAfterSeconds, 0f));
        }
    }
}
