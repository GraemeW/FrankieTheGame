using System.IO;
using System.Linq;
using UnityEditor;
using LowDefMustard.Core;

namespace Frankie.Speech.Editor
{
    internal sealed class DialogueNameGuard : AssetModificationProcessor
    {
        // Localization keys start with the dialogue's file name (by design for easy translator look-up) ~ thus: two dialogues sharing a name fight over localization entries
        //  - N.B. we cannot cover duplicates / new assets (Unity has no blocking hook for those) ~ the graph's sync will warn / hold off

        private static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
        {
            string newName = Path.GetFileNameWithoutExtension(destinationPath);
            if (newName == Path.GetFileNameWithoutExtension(sourcePath) || AssetDatabase.GetMainAssetTypeAtPath(sourcePath) != typeof(Dialogue)) { return AssetMoveResult.DidNotMove; }

            string clashPath = AssetDatabase.FindAssets($"t:{nameof(Dialogue)} {newName}").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(path => path != sourcePath && Path.GetFileNameWithoutExtension(path) == newName);
            if (clashPath == null) { return AssetMoveResult.DidNotMove; }

            LDMLog.Warning(LDMLogCategory.Speech, $"Didn't rename '{sourcePath}' to '{newName}': '{clashPath}' already uses that name, and dialogue localization keys start with the name.");
            return AssetMoveResult.FailedMove;
        }
    }
}
