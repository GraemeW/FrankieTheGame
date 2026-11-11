using System.Collections.Generic;
using UnityEngine;
using LowDefMustard.GameStateModifiers;
using LowDefMustard.Saving;
using LowDefMustard.Utils;

namespace Frankie.Core
{
    [ExecuteInEditMode]
    public class PredicateChildToggler : MonoBehaviour, ISaveable<bool>, IGameStateModifierHandler
    {
        // Tunables
        [SerializeField] private Condition condition;

        // Editor Field References
        internal const string conditionField = nameof(condition);

        // GameState Modifier Properties
        [SerializeField][HideInInspector] private string backingHandlerGUID;
        public string handlerGUID { get => backingHandlerGUID; set => backingHandlerGUID = value; }
        [SerializeField][HideInInspector] private int backingListHashCheck;
        public int modifierListHashCheck { get => backingListHashCheck; set => backingListHashCheck = value; }
        [SerializeField][HideInInspector] private bool backingHasGameStateModifiers;
        public bool hasGameStateModifiers { get => backingHasGameStateModifiers; set => backingHasGameStateModifiers = value; }
        [SerializeField][HideInInspector] private List<string> backingGameStateModifierGUIDs;
        public List<string> gameStateModifierGUIDs { get => backingGameStateModifierGUIDs; set => backingGameStateModifierGUIDs = value ?? new List<string>(); }

        // State
        private bool childrenEnabled = true;

        #region UnityMethods
        private void OnEnable()
        {
            if (!Application.isPlaying) { return; } // [ExecuteInEditMode] -> never toggle the scene's children in the editor
            if (condition == null) { return; }
            ToggleChildrenOnCondition();
        }

        private void OnDestroy()
        {
            IGameStateModifierHandler.TriggerOnDestroy(this);
        }

        private void OnDrawGizmos()
        {
            IGameStateModifierHandler.TriggerOnGizmos(this);
        }
        #endregion

        #region PublicMethods
        public void ToggleChildrenOnCondition() // Callable via Unity Events
        {
            PlayerStateMachine playerStateMachine = Player.FindPlayerStateMachine();
            if (playerStateMachine == null) { return; }
            
            if (condition.Check(playerStateMachine.GetComponents<IPredicateEvaluator>()))
            {
                foreach (Transform child in transform)
                {
                    child.gameObject.SetActive(true);
                }
                childrenEnabled = true;
            }
            else
            {
                foreach (Transform child in transform)
                {
                    child.gameObject.SetActive(false);
                }
                childrenEnabled = false;
            }
        }
        #endregion

        #region Interfaces
        public IList<GameStateModifier> GetGameStateModifiers() => IGameStateModifierPredicate.CollectGameStateModifiers(condition?.GetPredicates());
        #endregion

        #region SaveInterface
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;

        public SaveState CaptureState() => ManualGetStateFromData(childrenEnabled);

        public void RestoreState(SaveState saveState)
        {
            TryManualGetDataFromState(saveState, out childrenEnabled);
            foreach (Transform child in transform) { child.gameObject.SetActive(childrenEnabled); }
        }
        
        public SaveState ManualGetStateFromData(bool data) => new(GetLoadPriority(), data);
        
        public bool TryManualGetDataFromState(SaveState saveState, out bool value)
        {
            if (saveState != null && saveState.TryGetState(out value)) { return true; }
            value = childrenEnabled;
            return true;
        }
        #endregion
    }
}
