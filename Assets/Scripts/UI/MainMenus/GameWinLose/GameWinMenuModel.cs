using System.Collections.Generic;
using Unity.Properties;

namespace Frankie.Menu.UI
{
    public sealed class GameWinMenuModel : LauncherModel
    {
        // State
        private string internalCreditsHeaderText = "";
        private IReadOnlyList<CreditsLine> internalCreditsLines = new List<CreditsLine>();

        [CreateProperty] public string creditsHeaderText
        {
            get => internalCreditsHeaderText;
            set => SetProperty(ref internalCreditsHeaderText, value);
        }

        [CreateProperty] public IReadOnlyList<CreditsLine> creditsLines
        {
            get => internalCreditsLines;
            set => SetProperty(ref internalCreditsLines, value);
        }
    }
}
