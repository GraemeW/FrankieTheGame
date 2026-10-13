using UnityEngine;
using LowDefMustard.UIBox;
using LowDefMustard.Utils;

namespace Frankie.Menu.UI
{
    [RequireComponent(typeof(UIToolkitMenuView))]
    public sealed class SplashScreen : MonoBehaviour
    {
        // Tunables
        [Header("Text")]
        [SerializeField] private string studioName = "Low Def Mustard Games";
        [SerializeField] private string gameTitle = "Frankie";
        [Header("Timing")]
        [SerializeField][Min(0f)] private float stageFadeSeconds = 0.7f;

        // State
        private readonly SplashModel splashModel = new();

        #region UnityMethods
        private void Awake()
        {
            splashModel.studioName = studioName;
            splashModel.gameTitle = gameTitle;
            splashModel.stageFadeSeconds = stageFadeSeconds;
            GetComponent<UIToolkitMenuView>().SetDataSource(splashModel);
        }
        #endregion

        #region PublicMethods
        public float GetFadeSeconds() => stageFadeSeconds;
        
        public bool TryShowNextStage() // False:  no stages left (the last stage fades out over fadeSeconds)
        {
            splashModel.shownStage = splashModel.shownStage.NextClamped();
            return splashModel.shownStage != SplashStage.Finished;
        }
        #endregion
    }
}
