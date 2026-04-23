#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Frankie.Utils.Editor
{
    public class LocalizationDeletionHandler : AssetModificationProcessor
    {
        static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (asset != null && asset is ILocalizable localizable)
            {
                Debug.Log($"ScriptableObject at {assetPath} is about to be deleted!");
                // TODO:  Special handling here
            }
            
            // Pass back to Unity to continue deletion
            return AssetDeleteResult.DidNotDelete;
        }
    }
}
#endif
