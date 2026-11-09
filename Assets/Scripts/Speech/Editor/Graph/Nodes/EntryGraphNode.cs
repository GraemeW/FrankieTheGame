using System;
using Unity.GraphToolkit.Editor;

namespace Frankie.Speech.Editor
{
    [Serializable]
    [UseWithGraph(typeof(DialogueGraph))]
    [Node("Dialogue", "", "Entry")]
    internal sealed class EntryGraphNode : Node
    {
        // Const Tunables
        public const string outPort = "Out";

        #region UnityMethods
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddOutputPort<DialogueFlow>(outPort).WithCapacity(PortCapacity.Multi).WithConnectorUI(PortConnectorUI.Arrowhead).Build();
        }
        #endregion

        #region Visuals
        public void RefreshVisuals()
        {
            DefaultColor = DialogueGraphPalette.entry;
            Title = "Entry";
        }
        #endregion
    }
}
