using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Localization;
using Frankie.Utils.UI;

namespace Frankie.Menu.UI
{
    [ExecuteInEditMode]
    public class FrameFlavourChoice : UIChoiceButton, ILocalizable
    {
        [Header("Parameters")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedFrameFlavour;
        [SerializeField] private Color frameFlavourColour;
        [Header("Hookups")]
        [SerializeField] private UIFrame uiFrame;

        // Localization
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedFrameFlavour.TableEntryReference,
            };
        }
        
        // Getters
        public string GetFrameFlavour() => localizedFrameFlavour.GetSafeLocalizedString();
        public Color GetFrameFlavourColour() => frameFlavourColour;

        public void OverwriteUIFrameColour()
        {
            if (uiFrame == null) { return; }
            uiFrame.OverwriteLocalFrameFlavour(frameFlavourColour);
        }

        // Unity Methods
        private void Start()
        {
            SetText(localizedFrameFlavour.GetSafeLocalizedString());
            selectHighlightColor = frameFlavourColour;
            UseHighlightSelected(true);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ILocalizable.TriggerOnDestroy(this);
        }
    }
}
