using UnityEditor;
using LowDefMustard.Rendering.Editor;

namespace Frankie.Core.Editor
{
    [InitializeOnLoad]
    public static class GraphicsWarmupFolderRegistration
    {
        static GraphicsWarmupFolderRegistration()
        {
            GraphicsWarmupFolders.RegisterMaterialFolder("Assets/Game/VFX/Shaders");
            GraphicsWarmupFolders.RegisterUIToolkitMaterialFolder("Assets/Game/VFX/Shaders/BattleBackgrounds");
            GraphicsWarmupFolders.RegisterUIToolkitMaterialFolder("Assets/Game/VFX/Shaders/BattleEffectsEntities");
        }
    }
}
