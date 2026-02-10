using UnityEngine;

namespace Frankie.World
{
    public class WorldCanvas : MonoBehaviour
    {
        // Tunables
        [SerializeField] private Transform worldOptionsParent;
        // Constants
        private const string _worldCanvasTag = "WorldCanvas";
        
        #region StaticMethods
        public static WorldCanvas FindWorldCanvas() 
        {
            var worldCanvasGameObject = GameObject.FindGameObjectWithTag(_worldCanvasTag);
            return worldCanvasGameObject != null ? worldCanvasGameObject.GetComponent<WorldCanvas>() : null;
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
