using System.Collections;
using UnityEngine;
using LowDefMustard.Core;

namespace LowDefMustard.Zones
{
    public sealed class BootstrapSceneLoader : MonoBehaviour
    {
        // Note:
        //  - Keep the bootstrap scene free of other references:  anything it references is built in, and duplicates its Addressables copy
        //  - Boot() dies with the bootstrap scene once the first scene activates (the load itself completes)
        //      - so never put work after the load:  do it in the first scene or a persistent object (e.g. the SceneLoader)

        // Tunables
        [SerializeField] private SceneReference firstScene;

        #region UnityMethods
        private void Start()
        {
            if (!firstScene.IsSet()) { LDMLog.Error(LDMLogCategory.Zones, "Bootstrap has no first scene set. Set it on the bootstrap scene's loader.", this); return; }
            StartCoroutine(Boot());
        }
        #endregion

        #region PrivateMethods
        private IEnumerator Boot()
        {
            if (!firstScene.TryGetSceneGuid(out string sceneGuid))
            {
                LDMLog.Error(LDMLogCategory.Zones, $"Bootstrap's first scene '{firstScene.SceneName}' has no scene GUID, so the game can't start. Re-link it on the bootstrap scene's loader.", this);
                yield break;
            }
            yield return AddressableSceneLoader.LoadSceneAsync(sceneGuid, isLoaded =>
            {
                if (!isLoaded) { LDMLog.Error(LDMLogCategory.Zones, $"Bootstrap couldn't load its first scene '{firstScene.SceneName}', so the game can't start.", this); }
            });
        }
        #endregion

        #region PublicMethods
        public SceneReference GetFirstScene() => firstScene;
        #endregion
    }
}
