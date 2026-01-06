using UnityEngine;
using Frankie.Speech;

namespace Frankie.Core
{
    [CreateAssetMenu(fileName = "New Conversant Check Count Predicate", menuName = "Predicates/AIConversant/ConversantCheckCount")]
    public class ConversantCheckCountPredicate : PredicateAIConversant
    {
        [SerializeField] protected int checkCount = 0;
        [SerializeField][Tooltip("Supersedes isGreater, simple equality check")] protected bool isEqual = false;
        [SerializeField][Tooltip("True for >, False for <")] protected bool isGreater = true;

        public override bool? Evaluate(AIConversant aiConversant)
        {
            if (aiConversant == null) { return null; }
            
            int dialogueCount = aiConversant.GetDialogueCount();

            if (isEqual) { return dialogueCount == checkCount; }
            else
            {
                if (isGreater) { return dialogueCount > checkCount; }
                else { return dialogueCount < checkCount; }
            }
        }
    }

}
