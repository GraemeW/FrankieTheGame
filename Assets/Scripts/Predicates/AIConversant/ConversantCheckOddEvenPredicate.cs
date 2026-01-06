using UnityEngine;
using Frankie.Speech;

namespace Frankie.Core
{
    [CreateAssetMenu(fileName = "New Conversant Check Odd Even Predicate", menuName = "Predicates/AIConversant/ConversantCheckOddEven")]
    public class ConversantCheckOddEvenPredicate : PredicateAIConversant
    {
        [SerializeField] protected bool checkCountOdd = true;
        
        public override bool? Evaluate(AIConversant aiConversant)
        {
            if (aiConversant == null) { return null; }
            
            int dialogueCount = aiConversant.GetDialogueCount();
            return checkCountOdd ? dialogueCount % 2 == 1 : dialogueCount % 2 == 0;
        }
    }
}
