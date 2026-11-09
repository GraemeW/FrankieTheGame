using System;
using System.Collections.Generic;
using UnityEngine;
using LowDefMustard.Utils;

namespace Frankie.Speech
{
    [Serializable]
    public sealed class DialogueConditionNode : ScriptableObject
    {
        // Note: Never becomes the current node (no node entry/exit events)
        
        // Tunables
        [SerializeField] private string nodeID = "";
        [SerializeField] private Condition condition;
        [SerializeField] private List<string> children = new();

        // Public Methods
        public string GetNodeID() => nodeID;
        public List<string> GetChildren() => children;
        public bool CheckCondition(IEnumerable<IPredicateEvaluator> evaluators) => condition == null || condition.Check(evaluators);

#if UNITY_EDITOR
        internal void Initialize(string setNodeID, Condition setCondition, IEnumerable<string> setChildren)
        {
            nodeID = setNodeID;
            condition = setCondition;
            children = new List<string>(setChildren);
        }
#endif
    }
}
