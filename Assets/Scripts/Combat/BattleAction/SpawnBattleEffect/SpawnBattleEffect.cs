using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Combat.UI;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Spawn Battle Effect", menuName = "BattleAction/Effects/Spawn Battle Effect", order = 15)]
    public class SpawnBattleEffect : EffectStrategy
    {
        // Note:
        //  - Standard effect is sized to the recipient's slide and tinted by the effect colour (i.e. one material serves every colour variant)
        //  - Global effect covers the whole battle stage instead
        //  - Recipe shapes are drawn by the shared BattleEffect material;  an empty recipe draws the material as-is

        [SerializeField] private Material effectMaterial;
        [SerializeField] private BattleEffectRecipe effectRecipe = new();
        [SerializeField] private Color effectColour = Color.white;
        [SerializeField] private bool isGlobalEffect = false;
        [SerializeField][Min(0f)] private float delayAfterSeconds = 0.5f;
        [SerializeField][Tooltip("Set to min to never destroy")][Min(0f)] private float destroyAfterSeconds = 2.0f;
        
        public override IEnumerator StartEffect(CombatParticipant sender, IList<BattleEntity> recipients, DamageType damageType)
        {
            BattleUI battleUI = BattleUI.FindBattleUI();
            BattleStage battleStage = battleUI != null ? battleUI.GetBattleStage() : null;
            if (battleStage == null) { yield break; }

            if (isGlobalEffect) { battleStage.SpawnFullScreenEffect(effectMaterial, effectRecipe, effectColour, destroyAfterSeconds); }
            else if (recipients != null)
            {
                foreach (BattleEntity recipient in recipients)
                {
                    battleStage.SpawnEffect(recipient, effectMaterial, effectRecipe, effectColour, destroyAfterSeconds);
                }
            }
            yield return new WaitForSeconds(Mathf.Max(delayAfterSeconds, 0f));
        }
    }
}
