using UnityEngine;
using Frankie.Speech;

namespace Frankie.Tests
{
    public sealed class TestDialogueVariable : ScriptableObject, IDialogueVariable
    {
        // State
        [SerializeField] private string value = "";
        public DialogueVariableContext lastContext { get; private set; }
        public int resolveCount { get; private set; }

        // Public Methods
        public TestDialogueVariable Configure(string setValue)
        {
            value = setValue;
            return this;
        }

        public string GetDialogueVariable(DialogueVariableContext context)
        {
            resolveCount++;
            lastContext = context;
            return value;
        }
    }
}
