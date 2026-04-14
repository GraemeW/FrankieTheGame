using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
                if (selectedObject == null)
                {
                    continue;
                }

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
    }
}
