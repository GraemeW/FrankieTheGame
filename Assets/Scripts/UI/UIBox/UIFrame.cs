using Frankie.Saving;
using UnityEngine;
using UnityEngine.UI;

namespace Frankie.Utils.UI
{
    [RequireComponent(typeof(Image))]
    public class UIFrame : MonoBehaviour
    {
        [SerializeField] [Range(.5f, 1.5f)] private float colourModifyFactor = 1.0f; 
        
        // State
        private Color currentColour = _frameFlavourColour;
        
        // Cached References
        private Image frame;
        
        // Static
        private static bool _isFrameFlavourSet = false;
        private static Color _frameFlavourColour = Color.white;
        
        // Static Methods
        private static void InitializeFrameFlavour()
        {
            _isFrameFlavourSet = true;
            
            PlayerPrefsController.frameFlavourUpdated -= SetGlobalFrameFlavour;
            PlayerPrefsController.frameFlavourUpdated += SetGlobalFrameFlavour;
            
            if (!PlayerPrefsController.FrameFlavourColourKeyExists()) { return; }
            _frameFlavourColour = PlayerPrefsController.GetFrameFlavourColour();
        }
        private static void SetGlobalFrameFlavour(Color frameFlavourColor) => _frameFlavourColour = frameFlavourColor;
        
        // Local Methods
        private void OnEnable()
        {
            if (!_isFrameFlavourSet) { InitializeFrameFlavour(); }
            UpdateCurrentColour();
        }

        private void UpdateCurrentColour()
        {
            if (!TryGetComponent(out frame)) { return;  }
            currentColour = new Color(_frameFlavourColour.r * colourModifyFactor, _frameFlavourColour.g * colourModifyFactor, _frameFlavourColour.b * colourModifyFactor, _frameFlavourColour.a);
            frame.color = currentColour;
        }
        
        public void OverwriteLocalFrameFlavour(Color overwriteColour)
        {
            if (frame == null) { return; }
            frame.color = new Color(overwriteColour.r * colourModifyFactor, overwriteColour.g * colourModifyFactor, overwriteColour.b * colourModifyFactor, _frameFlavourColour.a);
        }

        public void ResetToDefaultFrameFlavour()
        {
            if (frame == null) { return; }
            frame.color = currentColour;
        }
    }
}
