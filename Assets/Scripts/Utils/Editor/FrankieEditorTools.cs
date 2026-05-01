#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using Frankie.Stats;

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
                //if (!gameObject.TryGetComponent(out Temp localizedLinker)) { continue; }

                if (selectedObject is not CharacterProperties localizedLinker) { continue; }
                //localizedLinker.TempLinkStrings();
            }
        }
        
        /*
        public void TempLinkStrings()
        {
           string keyStem;
           string key;
           TableEntryReference tableEntryReference;
           
           keyStem = nameof(localizedCheckMessage).Replace("localized", "");
           key = LocalizationTool.GenerateKindaUniqueKey(GetType(), gameObject, keyStem);
           tableEntryReference = key;
           if (localizedCheckMessage == null || LocalizationTool.GetEnglishEntry(localizationTableType, localizedCheckMessage.TableEntryReference) != checkMessage)
           {
               LocalizationTool.AddUpdateEnglishEntry(localizationTableType, tableEntryReference, checkMessage);
               LocalizationTool.SafelyUpdateReference(localizationTableType, localizedCheckMessage, key);
           }
           
            EditorUtility.SetDirty(this);
           AssetDatabase.SaveAssetIfDirty(this);
        }
        */
        
        /*
           string key;
           TableEntryReference tableEntryReference;

           key = $"CharacterProperties.{name}";
           tableEntryReference = key;
           if (localizedDisplayName == null || LocalizationTool.GetEnglishEntry(localizationTableType, localizedDisplayName.TableEntryReference) != name)
           {
               LocalizationTool.AddUpdateEnglishEntry(localizationTableType, tableEntryReference, name);
               LocalizationTool.SafelyUpdateReference(localizationTableType, localizedDisplayName, key);
           }
           
           EditorUtility.SetDirty(this);
           AssetDatabase.SaveAssetIfDirty(this);
         
         */
    }
}
#endif
