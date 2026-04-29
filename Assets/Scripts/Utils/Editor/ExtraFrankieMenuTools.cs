using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using Frankie.Control;

namespace Frankie.Utils.Editor
{
    public static class ExtraFrankieMenuTools
    {
        private const string _debugSceneRef = "Assets/Scenes/_Debug/TEST_BattleRoyale.unity";

        [MenuItem("Tools/Make Selection Dirty", false, 100)]
        private static void MakeSelectionDirty()
        {
            foreach (Object selectedObject in Selection.objects)
            {
                if (selectedObject == null) { continue; }
                Debug.Log($"Dirtying {selectedObject.name}");
                EditorUtility.SetDirty(selectedObject);
            }
        }

        [MenuItem("Tools/Force Reserialize Assets", false, 101)]
        private static void ForceReserializeAssets()
        {
            AssetDatabase.ForceReserializeAssets();
        }

        [MenuItem("Tools/Open Debug Scene", false)]
        private static void OpenDebugScene()
        {
            EditorSceneManager.OpenScene(_debugSceneRef);
        }

        [MenuItem("Tools/TempLinkCheckPrefabs")]
        private static void TempLinkCheckPrefabs()
        {
            List<Check> prefabChecks = new List<Check>();
            List<Check> prefabVariantChecks = new List<Check>();
            foreach (Object selectedObject in Selection.objects)
            {
                if (selectedObject is not GameObject gameObject) {  continue; }
                if (!gameObject.TryGetComponent(out Check check)) { continue; }
                PrefabAssetType type = PrefabUtility.GetPrefabAssetType(gameObject);
                switch (type)
                {
                    case PrefabAssetType.Regular:
                        prefabChecks.Add(check);
                        break;
                    case PrefabAssetType.Variant:
                        prefabVariantChecks.Add(check);
                        break;
                    case PrefabAssetType.NotAPrefab:
                    case PrefabAssetType.MissingAsset:
                    case PrefabAssetType.Model:
                    default:
                        break;
                }
            }

            foreach (Check prefabCheck in prefabChecks)
            {
                // Comment/uncomment for prefabs
                prefabCheck.TempCreateCheckEntries();
            }

            foreach (Check prefabVariantCheck in prefabVariantChecks)
            {
                // Comment/uncomment for variants
                //prefabVariantCheck.TempCreateCheckEntries();
            }
        }
        
        [MenuItem("Tools/TempLinkCheckTheRest")]
        private static void TempLinkCheckTheRest()
        {
            foreach (Object selectedObject in Selection.objects)
            {
                if (selectedObject is not GameObject gameObject) {  continue; }
                if (!gameObject.TryGetComponent(out Check check)) { continue; }
                
                check.TempCreateCheckEntries();
                
            }
        }
    }
}
