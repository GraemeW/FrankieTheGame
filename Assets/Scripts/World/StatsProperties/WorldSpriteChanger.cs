using Frankie.Saving;
using UnityEngine;

namespace Frankie.World
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class WorldSpriteChanger : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] private Sprite alternateSprite;
        
        // State
        private Sprite originalSprite;
        private bool isAlternateSprite = false;
        
        // Cached References
        private SpriteRenderer spriteRenderer;

        #region UnityMethods
        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            originalSprite = spriteRenderer.sprite;
        }
        #endregion

        #region PublicMethods
        public void ApplyAlternateSprite() // Called via Unity Events
        {
            isAlternateSprite = true;
            UpdateSprite();
        }

        public void ApplyOriginalSprite() // Called via Unity Events
        {
            isAlternateSprite = false;
            UpdateSprite();
        }

        public void ToggleSprite() // Called via Unity Events
        {
            isAlternateSprite = !isAlternateSprite;
            UpdateSprite();
        }
        #endregion

        #region PrivateMethods
        private void UpdateSprite()
        {
            spriteRenderer.sprite = isAlternateSprite ? alternateSprite : originalSprite;
        }
        #endregion

        #region SaveInterface
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;

        public SaveState CaptureState()
        {
            return new SaveState(LoadPriority.ObjectProperty, isAlternateSprite);
        }

        public void RestoreState(SaveState saveState)
        {
            isAlternateSprite = (bool)saveState.GetState(typeof(bool));
            UpdateSprite();
        }
        #endregion
    }
}
