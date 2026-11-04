using System;
using UnityEngine;
using UnityEngine.Localization;
using LowDefMustard.Localization;
using Frankie.Utils.Localization;

namespace Frankie.Control
{
    [Serializable]
    public sealed class DynamicCheckOption
    {
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedChoice;
        [SerializeField] private InteractionEvent interactionEvent;

        public LocalizedString GetLocalizedChoice() => localizedChoice;
        public InteractionEvent GetInteractionEvent() => interactionEvent;
    }
}
