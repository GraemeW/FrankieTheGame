using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LowDefMustard.Core;

namespace Frankie.Combat.Editor
{
    public sealed class OwnedBattleActionDeleteProcessor : AssetModificationProcessor
    {
        // Clean Up Utility:  Deleting a skill / action item also deletes the battle action it owns (as long as nothing else using it)

        #region UnityMethods
        private static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
        {
            List<string> ownedPaths = BattleActionAssets.FindOwners(path)
                .Select(owner => (owner, battleAction: BattleActionAssets.GetBattleAction(owner)))
                .Where(pair => BattleActionAssets.IsOwnedBy(pair.battleAction, pair.owner))
                .Select(pair => AssetDatabase.GetAssetPath(pair.battleAction))
                .Distinct()
                .ToList();
            if (ownedPaths.Count > 0) { EditorApplication.delayCall += () => DeleteUnused(ownedPaths, options); }
            return AssetDeleteResult.DidNotDelete;
        }
        #endregion

        #region PrivateMethods
        private static void DeleteUnused(IEnumerable<string> battleActionPaths, RemoveAssetOptions options)
        {
            foreach (string battleActionPath in battleActionPaths)
            {
                var battleAction = AssetDatabase.LoadAssetAtPath<BattleAction>(battleActionPath);
                if (battleAction == null) { continue; } // Already deleted (e.g. in the same selection)
                if (BattleActionAssets.IsUsedByAnyOwner(battleAction))
                {
                    LDMLog.Info(LDMLogCategory.Combat, $"Keeping battle action '{battleActionPath}': its owner was deleted, but another skill or action item still uses it.");
                    continue;
                }

                bool isDeleted = options == RemoveAssetOptions.MoveAssetToTrash ? AssetDatabase.MoveAssetToTrash(battleActionPath) : AssetDatabase.DeleteAsset(battleActionPath);
                if (isDeleted) { LDMLog.Info(LDMLogCategory.Combat, $"Deleted battle action '{battleActionPath}' along with the skill or action item that owned it."); }
                else { LDMLog.Warning(LDMLogCategory.Combat, $"Could not delete battle action '{battleActionPath}' (its owning skill or action item was deleted). Delete it by hand."); }
            }
        }
        #endregion
    }
}
