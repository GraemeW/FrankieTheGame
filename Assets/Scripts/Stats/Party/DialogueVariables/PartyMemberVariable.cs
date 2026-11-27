using UnityEngine;
using Frankie.Speech;

namespace Frankie.Stats
{
    [CreateAssetMenu(fileName = "New Party Member Variable", menuName = "Dialogue/Variables/Party Member", order = 5)]
    public class PartyMemberVariable : ScriptableObject, IDialogueVariable
    {
        // Tunables
        [Tooltip("0 = party leader ~ clamped: <0 -> leader, >end -> last member")]
        [SerializeField] private int partyIndex = 0;

        // Editor Field References
        internal const string partyIndexField = nameof(partyIndex);

        // Dialogue Variable
        public string GetDialogueVariable(DialogueVariableContext context)
        {
            if (!context.TryGetParty(out PartyBehaviour party)) { return ""; }

            int partySize = party.GetPartySize();
            if (partyIndex <= 0 || partySize == 0) { return party.GetPartyLeaderName(); }
            return CharacterProperties.GetCharacterDisplayName(party.GetMembers()[Mathf.Min(partyIndex, partySize - 1)]);
        }
    }
}
