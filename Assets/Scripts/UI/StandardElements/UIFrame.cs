using UnityEngine;
using UnityEngine.UI;
using Unity.Scripting.LifecycleManagement;
using Frankie.Saving;

namespace Frankie.Utils.UI
{
    [RequireComponent(typeof(Image))]
    public partial class UIFrame : MonoBehaviour
    {
        [SerializeField] [Range(.5f, 1.5f)] private float colourModifyFactor = 1.0f; 
        
        // State
        private Color currentColour = _frameFlavourColour;
        
        // Cached References
        private Image frame;
        
        #region Static
        [AutoStaticsCleanup] private static bool _isFrameFlavourSet = false;
        [AutoStaticsCleanup] private static Color _frameFlavourColour = Color.white;
        
        private static void InitializeFrameFlavour()
        {
            _isFrameFlavourSet = true;
            
            PlayerPrefsController.frameFlavourUpdated -= SetGlobalFrameFlavour;
            PlayerPrefsController.frameFlavourUpdated += SetGlobalFrameFlavour;
            
            if (!PlayerPrefsController.FrameFlavourColourKeyExists()) { return; }
            _frameFlavourColour = PlayerPrefsController.GetFrameFlavourColour();
        }
        private static void SetGlobalFrameFlavour(Color frameFlavourColor) => _frameFlavourColour = frameFlavourColor;
        #endregion
        
        #region UnityMethods
        private void OnEnable()
        {
            if (!_isFrameFlavourSet) { InitializeFrameFlavour(); }
            UpdateCurrentColour();
        }
        #endregion
        
        #region PublicMethods
        public void OverwriteLocalFrameFlavour(Color overwriteColour)
        {
            if (frame == null) { return; }
            frame.color = GetScaledColour(overwriteColour);
        }

        public void ResetToDefaultFrameFlavour()
        {
            if (frame == null) { return; }
            frame.color = currentColour;
        }
        #endregion
        
        #region PrivateMethods
        private void UpdateCurrentColour()
        {
            if (!TryGetComponent(out frame)) { return;  }
            currentColour = GetScaledColour(_frameFlavourColour);
            frame.color = currentColour;
        }

        private Color GetScaledColour(Color color)
        {
            // Prevent clipping >1 (no HDR support) while maintaining uniform scaling 
            float maxScaling = Mathf.Min(colourModifyFactor, 1 / color.r, 1 / color.g, 1 / color.b);
            return new Color(color.r * maxScaling, color.g * maxScaling, color.b * maxScaling, color.a);
        }
        #endregion
    }
}
