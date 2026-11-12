using System.Collections.Generic;
using UnityEngine;

namespace LowDefMustard.GameStateModifiers.Tests
{
    public sealed class TestGameStateModifierHandler : MonoBehaviour, IGameStateModifierHandler
    {
        // Note: No [ExecuteInEditMode] / OnDestroy hook on purpose ~ tests drive the interface methods directly

        // IGameStateModifierHandler properties (req explicit backing fields)
        [SerializeField] private string backingHandlerGUID;
        [SerializeField] private int backingModifierListHashCheck;
        [SerializeField] private bool backingHasGameStateModifiers;
        [SerializeField] private List<string> backingGameStateModifierGUIDs;
        public string handlerGUID { get => backingHandlerGUID; set => backingHandlerGUID = value; }
        public int modifierListHashCheck { get => backingModifierListHashCheck; set => backingModifierListHashCheck = value; }
        public bool hasGameStateModifiers { get => backingHasGameStateModifiers; set => backingHasGameStateModifiers = value; }
        public List<string> gameStateModifierGUIDs { get => backingGameStateModifierGUIDs; set => backingGameStateModifierGUIDs = value; }

        // State
        public List<GameStateModifier> modifiersToReturn = new();
        public int getGameStateModifiersCallCount;

        // Public Methods
        public IList<GameStateModifier> GetGameStateModifiers()
        {
            getGameStateModifiersCallCount++;
            return modifiersToReturn;
        }
    }
}
