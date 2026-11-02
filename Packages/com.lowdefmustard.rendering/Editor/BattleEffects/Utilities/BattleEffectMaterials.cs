#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace LowDefMustard.Rendering.Editor
{
    public static class BattleEffectMaterials
    {
        public static Material Find()
        {
            // Note:  matched on the shader's recipe property (not by name / path)
            foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(Material)}", new[] { "Assets" }))
            {
                var candidate = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (BattleEffectPropertyRefs.IsBattleEffectMaterial(candidate)) { return candidate; }
            }
            return null;
        }
    }
}
#endif
