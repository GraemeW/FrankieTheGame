using Frankie.Saving;
using UnityEngine;
using UnityEngine.UI;

namespace Frankie.Utils.UI
{
    [RequireComponent(typeof(Image))]
    public class UIFrame : MonoBehaviour
    {
        // Cached References
        private Image frame;
        
        // Static
        private static bool _isFrameFlavourSet = false;
        private static Color _frameFlavourColor = Color.white;
        
        // Static Methods
        private static void InitializeFrameFlavour()
        {
            _isFrameFlavourSet = true;
            if (!PlayerPrefsController.FrameFlavourColourKeyExists()) { return; }
            _frameFlavourColor = PlayerPrefsController.GetFrameFlavourColour();
        }

        public static void SetGlobalFrameFlavour(Color frameFlavourColor)
        {
            _frameFlavourColor = frameFlavourColor;
        }
        
        // Local Methods
        private void OnEnable()
        {
            if (!_isFrameFlavourSet) { InitializeFrameFlavour(); }
            if (TryGetComponent(out frame)) { frame.color = _frameFlavourColor; }
        }
        
        public void OverwriteLocalFrameFlavour(Color overwriteColor)
        {
            if (frame != null) { frame.color = overwriteColor; }
        }
    }
}
