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
            if (asset is ILocalizable localizable)
            {
                Debug.Log($"Scriptable Object at {assetPath} is about to be deleted.  Deleting localization entries.");
                localizable.HandleDeletion();
            }
            else if (asset is GameObject gameObject)
            {
                if (gameObject == null || !gameObject.TryGetComponent(out localizable)) { return AssetDeleteResult.DidNotDelete; }
                
                Debug.Log($"Prefab at {assetPath} is about to be deleted.  Deleting localization entries.");
                localizable.HandleDeletion();
            }
            
            // Pass back to Unity to continue deletion
            return AssetDeleteResult.DidNotDelete;
        }
    }
}
#endif
