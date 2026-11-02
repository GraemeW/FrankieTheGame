using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Frankie.Inventory;

namespace Frankie.Combat.Editor
{
    public static class BattleActionAssets
    {
        // Note:
        //  - This is just a lightweight static helper class to assist with battle action creation/deletion (editor tooling)
        //  - Skills / action items generally own a battle action named after them in their corresponding folder in OnLoadAssets/BattleActions
        //      - as these are hard-coded tunables, if folder structure changes, the paths need to update in-kind

        // Const Tunables
        private const string _battleActionsRoot = "Assets/Game/OnLoadAssets/BattleActions";
        private const string _skillsFolder = _battleActionsRoot + "/Skills";
        private const string _actionItemsFolder = _battleActionsRoot + "/ActionItems";

        #region PublicMethods
        public static BattleAction GetBattleAction(Object owner)
        {
            string field = owner switch
            {
                Skill => Skill.battleActionField,
                ActionItem => ActionItem.battleActionField,
                _ => null
            };
            return field == null ? null : new SerializedObject(owner).FindProperty(field).objectReferenceValue as BattleAction;
        }
        
        public static bool TryFindFromOwner(Object owner, out BattleAction battleAction, out string path)
        {
            path = GetBattleActionPath(owner);
            battleAction = path == null ? null : AssetDatabase.LoadAssetAtPath<BattleAction>(path);
            return battleAction != null;
        }
        
        public static bool IsOwnedBy(BattleAction battleAction, Object owner)
        {
            string battleActionPath = GetBattleActionPath(owner);
            return battleAction != null && battleActionPath != null && AssetDatabase.GetAssetPath(battleAction) == battleActionPath;
        }
        
        public static bool IsUsedByAnyOwner(BattleAction battleAction) => FindAllOwners(null).Any(owner => GetBattleAction(owner) == battleAction);
        
        public static string GetBattleActionPath(Object owner)
        {
            string folder = owner switch
            {
                Skill => _skillsFolder,
                ActionItem => _actionItemsFolder,
                _ => null
            };
            return folder == null ? null : $"{folder}/{owner.name}.asset";
        }
        
        public static BattleAction FindOrCreateBattleAction(Object owner)
        {
            if (TryFindFromOwner(owner, out BattleAction battleAction, out string path)) { return battleAction; }

            WarnOnNameClash(owner.name);
            EnsureFolder(path[..path.LastIndexOf('/')]);
            
            battleAction = ScriptableObject.CreateInstance<BattleAction>();
            AssetDatabase.CreateAsset(battleAction, path);
            AssetDatabase.SaveAssets();
            return battleAction;
        }

        public static List<Object> FindOwners(string assetOrFolderPath)
        {
            if (!AssetDatabase.IsValidFolder(assetOrFolderPath))
            {
                Object asset = AssetDatabase.LoadMainAssetAtPath(assetOrFolderPath);
                return asset is Skill or ActionItem ? new List<Object> { asset } : new List<Object>();
            }
            return FindAllOwners(new[] { assetOrFolderPath });
        }

        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) { return; }
            Debug.LogWarning($"Folder {folder} did not exist - creating it");

            string parent = folder[..folder.LastIndexOf('/')];
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder[(folder.LastIndexOf('/') + 1)..]);
        }
        #endregion

        #region PrivateMethods
        private static List<Object> FindAllOwners(string[] searchFolders)
        {
            return AssetDatabase.FindAssets($"t:{nameof(Skill)}", searchFolders)
                .Concat(AssetDatabase.FindAssets($"t:{nameof(ActionItem)}", searchFolders))
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .Select(AssetDatabase.LoadMainAssetAtPath)
                .Where(asset => asset is Skill or ActionItem)
                .ToList();
        }

        private static void WarnOnNameClash(string name)
        {
            foreach (string guid in AssetDatabase.FindAssets($"{name} t:{nameof(BattleAction)}"))
            {
                string clashPath = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(clashPath) != name) { continue; }
                Debug.LogWarning($"A battle action named {name} already exists at {clashPath} - rename one of them");
            }
        }
        #endregion
    }
}
