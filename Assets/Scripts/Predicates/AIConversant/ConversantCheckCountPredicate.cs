using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Speech;

namespace Frankie.Core
{
    [CreateAssetMenu(fileName = "New Conversant Check Count Predicate", menuName = "Predicates/AIConversant/CheckConversantCheckCount")]
    public class ConversantCheckCountPredicate : PredicateAIConversant
    {
        [SerializeField] protected int checkCount = 0;
        [SerializeField][Tooltip("Supersedes isGreater, simple equality check")] protected bool isEqual = false;
        [SerializeField][Tooltip("True for >, False for <")] protected bool isGreater = true;

        public override bool? Evaluate(AIConversant aiConversant)
        {
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
