using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LowDefMustard.Rendering;
using Frankie.Combat.UI;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Spawn Battle Effect", menuName = "BattleAction/Effects/Spawn Battle Effect", order = 15)]
    public class SpawnBattleEffect : EffectStrategy
    {
        // Note:
        //  - Standard effect is sized to the recipient's slide and coloured by the effect tint (i.e. one recipe serves every colour variant)
        //  - Global effect covers the whole battle stage instead
        //  - A recipe is drawn by the shared BattleEffect material (w/o a recipe, the material is drawn as-is)

        [SerializeField] private Material effectMaterial;
        [SerializeField] private BattleEffectRecipe effectRecipe;
        [SerializeField] private BattleEffectTint effectTint = new();
        [SerializeField] private bool isGlobalEffect = false;
        [SerializeField][Min(0f)] private float delayAfterSeconds = 0.5f;
        [SerializeField][Tooltip("Set to min to never destroy")][Min(0f)] private float destroyAfterSeconds = 2.0f;
        
        public override IEnumerator StartEffect(CombatParticipant sender, IList<BattleEntity> recipients, DamageType damageType)
        {
            BattleUI battleUI = BattleUI.FindBattleUI();
            BattleStage battleStage = battleUI != null ? battleUI.GetBattleStage() : null;
            if (battleStage == null) { yield break; }

            if (isGlobalEffect) { battleStage.SpawnFullScreenEffect(effectMaterial, effectRecipe, effectTint, destroyAfterSeconds); }
            else if (recipients != null)
            {
                foreach (BattleEntity recipient in recipients)
                {
                    battleStage.SpawnEffect(recipient, effectMaterial, effectRecipe, effectTint, destroyAfterSeconds);
                }
            }
            yield return new WaitForSeconds(Mathf.Max(delayAfterSeconds, 0f));
        }
    }
}
