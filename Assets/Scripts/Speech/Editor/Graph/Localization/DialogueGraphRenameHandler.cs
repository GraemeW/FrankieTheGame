using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using Frankie.Utils.Localization;

namespace Frankie.Speech.Editor
{
    internal sealed class DialogueGraphRenameHandler : AssetPostprocessor
    {
        // Note:
        //  - A. Deliberately NOT doing the localization updates in the Importer itself (must not write other assets)
        //  - B. Force re-importing assets here because Unity doesn't reliably run ScriptedImporter on rename alone (imported data would have old name baked)
        
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            for (int i = 0; i < movedAssets.Length; i++)
            {
                if (Path.GetExtension(movedAssets[i]) != "." + DialogueGraph.assetExtension) { continue; }
                string oldName = Path.GetFileNameWithoutExtension(movedFromAssetPaths[i]);
                string newName = Path.GetFileNameWithoutExtension(movedAssets[i]);
                if (oldName == newName) { continue; }

                RenameKeys(oldName, newName);
                AssetDatabase.ImportAsset(movedAssets[i], ImportAssetOptions.ForceUpdate); // Critical Step!! -> imported nodes carry the dialogue name in their keys
            }
        }

        private static void RenameKeys(string oldName, string newName)
        {
            if (!LocalizationTool.GetOrMakeTableCollection(LocalizationTableType.Speech, out StringTableCollection collection)) { return; }
            List<(string oldKey, string newKey)> renames = collection.SharedData.Entries.Where(entry => entry.Key.StartsWith(DialogueLineKeys.GetPrefix(oldName))).Select(entry => (entry.Key, newName + entry.Key[oldName.Length..])).ToList();
            LocalizationTool.RenameKeys(LocalizationTableType.Speech, renames);
        }
    }
}
