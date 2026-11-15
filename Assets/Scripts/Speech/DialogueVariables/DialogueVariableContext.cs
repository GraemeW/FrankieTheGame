using Frankie.Stats;

namespace Frankie.Speech
{
    public readonly struct DialogueVariableContext
    {
        private readonly PartyBehaviour party;
        private readonly AIConversant conversant;

        public DialogueVariableContext(PartyBehaviour party, AIConversant conversant)
        {
            this.party = party;
            this.conversant = conversant;
        }

        public bool TryGetParty(out PartyBehaviour passParty) { passParty = party; return passParty != null; }
        public bool TryGetConversant(out AIConversant passConversant) { passConversant = conversant; return passConversant != null; }
    }
}
