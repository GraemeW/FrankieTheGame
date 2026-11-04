using System;
using System.Collections.Generic;
using UnityEngine;
using LowDefMustard.Utils;

namespace Frankie.Speech
{
    [Serializable]
    public sealed class DialogueConditionNode : ScriptableObject
    {
        // Note:
        //  - Traversal passes through to its children only while its condition holds
        //  - Never becomes the current node, so it raises no node entry/exit events
        
        // Tunables
        [SerializeField] private Condition condition;
        [SerializeField] private List<string> children = new();

        // Public Methods
        public List<string> GetChildren() => children;
        public bool CheckCondition(IEnumerable<IPredicateEvaluator> evaluators) => condition == null || condition.Check(evaluators);

#if UNITY_EDITOR
        internal void Initialize(Condition setCondition, IEnumerable<string> setChildren)
        {
            condition = setCondition;
            children = new List<string>(setChildren);
        }
#endif
    }
}
