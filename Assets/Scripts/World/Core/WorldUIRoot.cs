using UnityEngine;
using LowDefMustard.Localization;

namespace Frankie.World
{
    public class WorldUIRoot : MonoBehaviour, ILocalizationKeyRoot
    {
        // Tunables
        [SerializeField] private Transform worldOptionsParent;
        // Constants
        private const string _worldUIRootTag = "WorldUIRoot";
        
        #region StaticMethods
        public static WorldUIRoot FindWorldUIRoot() 
        {
            var worldUIRootGameObject = GameObject.FindGameObjectWithTag(_worldUIRootTag);
            return worldUIRootGameObject != null ? worldUIRootGameObject.GetComponent<WorldUIRoot>() : null;
        }
        #endregion

        #region PublicMethods
        public void DestroyExistingWorldOptions()
        {
            foreach (Transform child in worldOptionsParent)
            {
                Destroy(child.gameObject);
            }
        }

        public Transform GetWorldOptionsParent()
        {
            return worldOptionsParent;
        }
        #endregion
    }
}
