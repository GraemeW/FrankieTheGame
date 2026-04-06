using UnityEditor;
using UnityEngine;

namespace Frankie.Utils.Editor
{
    public class ExtraFrankieMenuTools
    {
        [MenuItem("Tools/Make Selection Dirty")]
        private static void MakeSelectionDirty()
        {
            foreach (Object selectedObject in Selection.objects) 
            {
                if (selectedObject == null) { continue; }
                Debug.Log($"Dirtying {selectedObject.name}");
                EditorUtility.SetDirty(selectedObject);
            }
        }
    }
}
