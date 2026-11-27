using UnityEditor;
using UnityEngine.SceneManagement;
using LowDefMustard.Saving;
using LowDefMustard.Zones.Editor;

namespace Frankie.Saving.Editor
{
    [InitializeOnLoad]
    public static class SavingSceneCheckRegistration
    {
        static SavingSceneCheckRegistration()
        {
            SavingSystem.editorIsSceneLoadable = AddressableSceneEntries.IsLoadableScene;
            SavingSystem.editorGetActiveSceneKey = () => AssetDatabase.AssetPathToGUID(SceneManager.GetActiveScene().path);
        }
    }
}
