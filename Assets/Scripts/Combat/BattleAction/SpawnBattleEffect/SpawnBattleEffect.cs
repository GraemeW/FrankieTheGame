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
        //  - A recipe is drawn by the shared BattleEffect material (w/o a recipe, the material is drawn as-is)

        [SerializeField] private Material effectMaterial;
        [SerializeField] private BattleEffectRecipe effectRecipe;
        [SerializeField] private BattleEffectTint effectTint = new();
        [SerializeField] private BattleEffectPlacement placement = BattleEffectPlacement.Slide;
        [SerializeField][Min(0f)] private float delayAfterSeconds = 0.5f;
        [SerializeField][Tooltip("Set to min to never destroy")][Min(0f)] private float destroyAfterSeconds = 2.0f;
        
        public override IEnumerator StartEffect(CombatParticipant sender, IList<BattleEntity> recipients, DamageType damageType)
        {
            if (effectMaterial == null || effectRecipe == null) { yield break; }
            
            BattleUI battleUI = BattleUI.FindBattleUI();
            BattleStage battleStage = battleUI != null ? battleUI.GetBattleStage() : null;
            if (battleStage == null) { yield break; }

            switch (placement)
            {
                case BattleEffectPlacement.FullScreen:
                    battleStage.SpawnFullScreenEffect(effectMaterial, effectRecipe, effectTint, destroyAfterSeconds);
                    break;
                case BattleEffectPlacement.Row:
                    foreach (BattleRow row in GetEnemyRows(recipients)) { battleStage.SpawnRowEffect(row, effectMaterial, effectRecipe, effectTint, destroyAfterSeconds); }
                    break;
                case BattleEffectPlacement.Slide:
                default:
                    if (recipients == null) { break; }
                    foreach (BattleEntity recipient in recipients) { battleStage.SpawnEffect(recipient, effectMaterial, effectRecipe, effectTint, destroyAfterSeconds); }
                    break;
            }
            yield return new WaitForSeconds(Mathf.Max(delayAfterSeconds, 0f));
        }

        private static HashSet<BattleRow> GetEnemyRows(IList<BattleEntity> recipients)
        {
            // Note:  Party members are skipped (they're not actually on a battle row, despite defaulting to Middle)
            var rows = new HashSet<BattleRow>();
            if (recipients == null) { return rows; }
            foreach (BattleEntity recipient in recipients)
            {
                if (recipient != null && !recipient.isCharacter) { rows.Add(recipient.row); }
            }
            return rows;
        }
    }
}
