using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor;
using LowDefMustard.Utils;

namespace Frankie.Speech.Editor
{
    [Serializable]
    [UseWithGraph(typeof(DialogueGraph))]
    [Node("Dialogue", "", "Condition")]
    internal sealed class ConditionGraphNode : ContextNode
    {
        // Const Tunables
        public const string inPort = "In";
        public const string outPort = "Out";

        #region UnityMethods
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort<DialogueFlow>(inPort).WithCapacity(PortCapacity.Multi).WithConnectorUI(PortConnectorUI.Arrowhead).Build();
            context.AddOutputPort<DialogueFlow>(outPort).WithCapacity(PortCapacity.Multi).WithConnectorUI(PortConnectorUI.Arrowhead).Build();
        }
        #endregion

        #region PublicMethods
        public IEnumerable<PredicateGraphBlock> GetPredicateBlocks() => BlockNodes.OfType<PredicateGraphBlock>();

        public void AddPredicates(IEnumerable<Condition.PredicateWrapper> predicateWrappers)
        {
            foreach (Condition.PredicateWrapper predicateWrapper in predicateWrappers)
            {
                var block = new PredicateGraphBlock();
                AddBlockNode(block);
                block.SetPredicate(predicateWrapper.GetPredicate(), predicateWrapper.GetNegate());
            }
        }

        public Condition BuildCondition()
        {
            // One single-predicate disjunction per block: an AND of the blocks
            var condition = new Condition();
            foreach (PredicateGraphBlock block in GetPredicateBlocks())
            {
                if (block.GetPredicate() == null) { continue; }
                var disjunction = new Condition.Disjunction();
                disjunction.AddPredicateWrapper(new Condition.PredicateWrapper(block.GetPredicate(), block.GetNegate()));
                condition.AddDisjunction(disjunction);
            }
            return condition;
        }
        #endregion

        #region Visuals
        public void RefreshVisuals()
        {
            DefaultColor = DialogueGraphPalette.condition;
            Title = "Condition";
            Subtitle = BlockCount > 1 ? $"if all of {BlockCount}" : "if";
            foreach (PredicateGraphBlock block in GetPredicateBlocks()) { block.RefreshVisuals(); }
        }
        #endregion
    }
}
