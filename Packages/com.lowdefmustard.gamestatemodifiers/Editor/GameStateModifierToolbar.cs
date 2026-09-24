using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace LowDefMustard.GameStateModifiers.Editor
{
    public static class GameStateModifierToolbar
    {
        // Const Tunables
        private const string _assetsFolderRef = "Assets";
        
        #region PublicMethods
        [MenuItem("Tools/GameStateModifiers/CleanAllDanglingModifiers", false, 401)]
        public static void CleanAllDanglingModifiers()
        {
            List<GameStateModifier> gameStateModifiers = FindAllGameStateModifiers(_assetsFolderRef);
            int gameStateModifierCount = gameStateModifiers.Count;
            int removedHandlerCount = 0;

            try
            {
                for (int i = 0; i < gameStateModifierCount; i++)
                {
                    GameStateModifier gameStateModifier = gameStateModifiers[i];
                    if (gameStateModifier == null) { continue; }
                    
                    EditorUtility.DisplayProgressBar("GameStateModifiers:  Cleaning Dangling Modifiers", $"Progress: {i}/{gameStateModifierCount}", i / (float)gameStateModifierCount);
                    removedHandlerCount += gameStateModifier.CleanDanglingModifierHandlerData();
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("GameStateModifiers:  Cleaning Dangling Modifiers", $"Total Removed Handlers: {removedHandlerCount}", "OK");
            }
        }
        
        #endregion
        
        #region HelperMethods
        private static List<GameStateModifier> FindAllGameStateModifiers(string rootFolder)
        {
            if (!Directory.Exists(rootFolder)) { return new List<GameStateModifier>(); }
            
            string[] guids = AssetDatabase.FindAssets("t:GameStateModifier", new[] { rootFolder });

            List<GameStateModifier> gameStateModifiers = new();
            foreach (string guid in guids)
            {
                if (string.IsNullOrEmpty(guid)) { continue; }
                
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var gameStateModifier = AssetDatabase.LoadAssetAtPath<GameStateModifier>(path);
                if (gameStateModifier != null)
                {
                    gameStateModifiers.Add(gameStateModifier);
                }
            }

            return gameStateModifiers;
        }
        #endregion
    }
}
