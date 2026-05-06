#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Frankie.Utils.Editor
{
    public static class FrankieEditorTools
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
        
        [MenuItem("Tools/TempLocalizedLinker")]
        private static void TempLocalizedLinker()
        {
            foreach (Object selectedObject in Selection.objects)
            {
                //if (selectedObject is not GameObject gameObject) {  continue; }
                //if (!gameObject.TryGetComponent(out Shop localizedLinker)) { continue; }
                //localizedLinker.TempLinkStrings();
                
                //if (selectedObject is not InventoryItem inventoryItem) { continue; }
                //inventoryItem.TempLinkStrings();
            }
        }
        
        /* GameObject Base
        public void TempLinkStrings()
        {
            string key;
           
            key = LocalizationTool.GenerateTypeSpecificKey(gameObject, nameof(localizedMessageIntro).Replace("localized", ""));
            LocalizationTool.TryLocalizeEntry(localizationTableType, localizedMessageIntro, key, name);
           
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }
        */
        
        /* Scriptable Object Base
        public void TempLinkStrings()
        {
            string key;

           key = GetNameLocalizationKey();
           LocalizationTool.TryLocalizeEntry(localizationTableType, localizedDisplayName, key, name);
           
           EditorUtility.SetDirty(this);
           AssetDatabase.SaveAssetIfDirty(this);
        }
         */
    }
}
#endif
