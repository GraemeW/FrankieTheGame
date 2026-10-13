using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Menu.UI
{
    [CreateAssetMenu(fileName = "Credits", menuName = "UI/Credits", order = 40)]
    public sealed class Credits : ScriptableObject
    {
        // Tunables
        [Tooltip("Displayed in order; roles may repeat")][SerializeField] private List<CreditsEntry> creditsEntries = new();

        public IReadOnlyList<CreditsEntry> GetCreditsEntries() => creditsEntries;
    }
}
