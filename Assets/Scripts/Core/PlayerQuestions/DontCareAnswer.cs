using System;
using UnityEngine.Localization;
using LowDefMustard.Localization;
using Frankie.Utils.Localization;

namespace Frankie.Core
{
    [Serializable]
    public class DontCareAnswer
    {
        [SimpleLocalizedString(LocalizationTableType.UI, false)] public LocalizedString entry;
    }
}
