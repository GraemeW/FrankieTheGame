using UnityEngine;
using UnityEngine.Localization;
using Frankie.Utils.Localization;

namespace Frankie.Quests
{
    [System.Serializable]
    public struct LocalizedQuestString
    {
        // For use with Unity's serialization of lists, arrays
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Quests, true)] public LocalizedString entry; 
    }
}
