using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LowDefMustard.Control;

namespace LowDefMustard.UIBox
{
    public abstract class UIBoxBase : MonoBehaviour
    {
        // Note:  Methods marked internal are for test visibility
        
        // Const/Static Fixed
        private const float _spatialAngleCheck = 85f;
        private static readonly float _spatialCosAngleCheck = Mathf.Cos(_spatialAngleCheck * Mathf.Deg2Rad);
        
        // Key State Parameters
        protected bool handleGlobalInput { get; set; } = true;
        protected bool clearChoiceOptionsOnEnable { get; set; } = false; // Choices persist across enables unless a box re-creates them each time (e.g. TextScanBox)
        protected bool preventEscapeOptionExit { get; set; } = false;
        protected bool keepPointerInputWhenInactive { get; set; } = false; // e.g. for menus whose choices swap out the child box on top
        
        // State -- Standard
        protected BaseController controller;
        public bool destroyQueued { get; set; } = false;

        // State -- Choices
        private bool isChoiceAvailable = false;
        private bool clearDisableCallbacksOnChoose = false;
        protected readonly List<IUIChoice> choiceOptions = new();
        protected IUIChoice highlightedChoiceOption;

        // Cached References
        private UIToolkitBoxView boxView; // Note:  A box without a view has no visuals of its own ~ boxView enforces PanelRenderer attachment
        private bool isBoxViewResolved = false;

        // Event Handles
        private event Action<ReceiverModifiedType, ReceiverModifiedData> receiverModified;
        
        #region UtilityMethods
        protected bool TryGetBoxView(out UIToolkitBoxView view)
        {
            if (!isBoxViewResolved)
            {
                TryGetComponent(out boxView);
                isBoxViewResolved = true;
            }
            view = boxView;
            return view != null;
        }
        
        public void SubscribeToReceiverUpdates(bool enable, Action<ReceiverModifiedType, ReceiverModifiedData> action)
        {
            receiverModified -= action;
            if (enable) { receiverModified += action; }
        }
        
        public void SetHandleGlobalInput(bool enable) => handleGlobalInput = enable; // Warning:  Only use if a known receiver exists to handle input
        protected void SetVisible(bool enable) { if (TryGetBoxView(out UIToolkitBoxView view)) { view.SetVisible(enable); } }
        protected abstract void SimpleTriggerUIBoxModified(ReceiverModifiedType dialogueBoxModifiedType); // Implemented via UIBox
        protected void TriggerUIBoxModified(ReceiverModifiedType dialogueBoxModifiedType, ReceiverModifiedData uiBoxModifiedData) => receiverModified?.Invoke(dialogueBoxModifiedType, uiBoxModifiedData);
        public void ClearDisableCallbacksOnChoose(bool enable) => clearDisableCallbacksOnChoose = enable;
        public void ClearDisableCallbacks() => SimpleTriggerUIBoxModified(ReceiverModifiedType.ClearDisableCallbacks);
        #endregion

        #region ChoiceSetup
        // Use state variable instead of counting for co-ex with dialogue system
        protected bool IsChoiceAvailable() => isChoiceAvailable;
        protected void SetChoiceAvailable(bool enable) => isChoiceAvailable = enable;

        protected static bool IsChoiceAlive(IUIChoice choice) => choice is { isAlive: true };

        protected void SetHighlightedChoice(IUIChoice choice)
        {
            ClearChoiceSelections();
            highlightedChoiceOption = choice;
            choice.Highlight(true);
            TriggerChoiceModified(ReceiverModifiedType.ItemHighlighted);
        }

        // Note:  Choice sounds are box-level - highlight / select are raised here for the box's sound box to play
        protected void TriggerChoiceModified(ReceiverModifiedType receiverModifiedType) => SimpleTriggerUIBoxModified(receiverModifiedType);

        protected IUIChoice AddChoiceOption(string choiceText, Action action) => CreateChoiceOption(choiceText, () => StandardChoiceExecution(action, true));
        
        protected IUIChoice AddNonDestroyChoiceOption(string choiceText, Action action, Action onHighlight = null) => CreateChoiceOption(choiceText, () => StandardChoiceExecution(action, false), onHighlight);
        
        protected void AddSeparator(ChoiceSeparatorType separatorType = ChoiceSeparatorType.Major) { if (TryGetBoxView(out UIToolkitBoxView view)) { view.CreateChoiceSeparator(separatorType); } }

        private IUIChoice CreateChoiceOption(string choiceText, Action onChoose, Action onHighlight = null)
        {
            if (!TryGetBoxView(out UIToolkitBoxView view)) { Debug.LogWarning($"UIBox[{name}] has no view to create choice options in."); return null; }

            IUIChoice choice = view.CreateChoiceOption(choiceText, onChoose, onHighlight);
            choiceOptions.Add(choice);
            return choice;
        }

        protected void ClearChoiceSelections()
        {
            highlightedChoiceOption = null;
            foreach (IUIChoice choiceOption in choiceOptions.Where(IsChoiceAlive))
            {
                choiceOption.Highlight(false);
            }
        }
        #endregion
        
        #region ChoiceExecution
        protected bool StandardChoose(string chooseDetail)
        {
            // Note:  chooseDetail ignored in standard implementation - employed in DialogueBox override
            if (!IsChoiceAlive(highlightedChoiceOption)) { return false; }
            highlightedChoiceOption.UseChoice();
            return true;
        }
       
        protected void StandardChoiceExecution(Action action, bool destroyOnChoose = true)
        {
            if (destroyOnChoose && clearDisableCallbacksOnChoose) { SimpleTriggerUIBoxModified(ReceiverModifiedType.ClearDisableCallbacks); }
            
            TriggerChoiceModified(ReceiverModifiedType.ItemSelected);
            action?.Invoke();
            
            if (destroyOnChoose) { Destroy(gameObject); }
        }
        #endregion

        #region InputHandling
        public bool TrySetController(BaseController setController)
        {
            if (setController == null) { return false; }

            handleGlobalInput = true;
            controller = setController;
            return true;
        }
        
        protected bool StandardMoveCursor(ControllerInputType controllerInputType, CursorMovementStyle cursorMovementStyle)
        {
            if (!isChoiceAvailable || !IsChoiceAlive(highlightedChoiceOption)) { return false; }

            if (TryInterceptMove(controllerInputType)) { return true; }
            
            // Standard choice handling
            int choiceIndex = choiceOptions.IndexOf(highlightedChoiceOption);
            bool validInput = TryExecuteMove(controllerInputType, ref choiceIndex, choiceOptions.Count, cursorMovementStyle);
            if (!validInput) { return false; }

            SetHighlightedChoice(choiceOptions[choiceIndex]);
            return true;
        }

        protected bool MoveCursor2D(ControllerInputType controllerInputType)
        {
            // Standard implementation
            if (!isChoiceAvailable || !IsChoiceAlive(highlightedChoiceOption)) { return false; }

            if (TryInterceptMove(controllerInputType)) { return true; }
            
            // Standard choice handling
            int choiceIndex = choiceOptions.IndexOf(highlightedChoiceOption);
            bool validInput = TryExecuteMove2D(controllerInputType, ref choiceIndex, choiceOptions.Count);
            if (!validInput) { return false; }

            SetHighlightedChoice(choiceOptions[choiceIndex]);
            return true;
        }
        
        protected bool StandardMoveCursorSpatial(ControllerInputType controllerInputType)
        {
            if (!isChoiceAvailable || !IsChoiceAlive(highlightedChoiceOption)) { return false; }

            if (TryInterceptMove(controllerInputType)) { return true; }

            // Standard choice handling
            if (!BaseController.TryInputTypeToNavigationVector(controllerInputType, out Vector2 direction)) { return false; }
            if (!highlightedChoiceOption.TryGetNavigationRect(out Rect originRect)) { return false; }

            Vector2 origin = originRect.center;
            if (!TryFindClosestRayHit(origin, direction, choiceOptions, highlightedChoiceOption, out IUIChoice targetChoice))
            {
                TryFindBestAngleMatch(origin, direction, choiceOptions, highlightedChoiceOption, out targetChoice);
            }
            if (!IsChoiceAlive(targetChoice)) { return false; }

            SetHighlightedChoice(targetChoice);
            return true;
        }
        
        // Special objects that require specialty input (sliders, choice groups, etc.)
        private bool TryInterceptMove(ControllerInputType controllerInputType)
        {
            if (highlightedChoiceOption is not IUIMoveInterceptor uiMoveInterceptor || !uiMoveInterceptor.TryMove(controllerInputType, out bool isHighlightMove)) { return false; }
            if (isHighlightMove) { TriggerChoiceModified(ReceiverModifiedType.ItemHighlighted); }
            return true;
        }

        protected bool TryEarlyExit(ControllerInputType controllerInputType)
        {
            if (preventEscapeOptionExit) { return false; }
            if (controllerInputType is not (ControllerInputType.Cancel or ControllerInputType.Option or ControllerInputType.Escape)) { return false; }
            destroyQueued = true;
            return true;
        }
        #endregion

        #region PrivateStaticMethods
        internal static bool TryExecuteMove(ControllerInputType controllerInputType, ref int currentSelectionIndex, int optionsCount, CursorMovementStyle cursorMovementStyle)
        {
            bool validInput = false;
            switch (controllerInputType)
            {
                case ControllerInputType.NavigateRight when cursorMovementStyle is CursorMovementStyle.Combined or CursorMovementStyle.Horizontal:
                case ControllerInputType.NavigateDown when cursorMovementStyle is CursorMovementStyle.Combined or CursorMovementStyle.Vertical:
                {
                    if (currentSelectionIndex + 1 >= optionsCount) { currentSelectionIndex = 0; }
                    else { currentSelectionIndex++; }
                    validInput = true;
                    break;
                }
                case ControllerInputType.NavigateUp when cursorMovementStyle is CursorMovementStyle.Combined or CursorMovementStyle.Vertical:
                case ControllerInputType.NavigateLeft when cursorMovementStyle is CursorMovementStyle.Combined or CursorMovementStyle.Horizontal:
                {
                    if (currentSelectionIndex <= 0) { currentSelectionIndex = optionsCount - 1; }
                    else { currentSelectionIndex--; }
                    validInput = true;
                    break;
                }
            }
            return validInput;
        }
        
        internal static bool TryExecuteMove2D(ControllerInputType controllerInputType, ref int choiceIndex, int optionsCount)
        {
            bool validInput = false;
            if (optionsCount == 1)
            {
                choiceIndex = 0;
                validInput = true;
            }
            else switch (controllerInputType)
            {
                case ControllerInputType.NavigateRight:
                {
                    if (choiceIndex + 1 >= optionsCount) { choiceIndex = 0; }
                    else { choiceIndex++; }
                    validInput = true;
                    break;
                }
                case ControllerInputType.NavigateLeft:
                {
                    if (choiceIndex <= 0) { choiceIndex = optionsCount - 1; }
                    else { choiceIndex--; }
                    validInput = true;
                    break;
                }
                case ControllerInputType.NavigateDown:
                {
                    if (choiceIndex + 2 >= optionsCount) { choiceIndex = 0; }
                    else { choiceIndex++; choiceIndex++; }
                    validInput = true;
                    break;
                }
                case ControllerInputType.NavigateUp:
                {
                    if (choiceIndex <= 1) { choiceIndex = optionsCount - 1; }
                    else { choiceIndex--; choiceIndex--; }
                    validInput = true;
                    break;
                }
            }
            return validInput;
        }
        
        internal static bool TryFindClosestRayHit(Vector2 origin, Vector2 direction, IEnumerable<IUIChoice> choiceOptions, IUIChoice highlightedChoiceOption, out IUIChoice closestChoice)
        {
            closestChoice = null;

            float closestDistance = float.PositiveInfinity;
            foreach (IUIChoice candidate in choiceOptions.Where(candidate => IsChoiceAlive(candidate) && candidate != highlightedChoiceOption))
            {
                if (!candidate.TryGetNavigationRect(out Rect candidateRect)) { continue; }
                if (!TryRayIntersectsRect(origin, direction, candidateRect, out float distance)) { continue; }
                if (distance >= closestDistance) { continue; }

                closestDistance = distance;
                closestChoice = candidate;
            }
            return closestChoice != null;
        }
        
        internal static bool TryFindBestAngleMatch(Vector2 origin, Vector2 direction, IEnumerable<IUIChoice> choiceOptions, IUIChoice highlightedChoiceOption, out IUIChoice bestChoice)
        {
            bestChoice = null;
            
            float bestScore = float.NegativeInfinity;
            foreach (IUIChoice candidate in choiceOptions.Where(candidate => IsChoiceAlive(candidate) && candidate != highlightedChoiceOption))
            {
                if (!candidate.TryGetNavigationRect(out Rect candidateRect)) { continue; }

                Vector2 delta = candidateRect.center - origin;
                float sqrMagnitude = delta.sqrMagnitude;
                if (sqrMagnitude <= 0f) { continue; }

                float alignment = Vector2.Dot(direction, delta);
                float magnitude = Mathf.Sqrt(sqrMagnitude);
                if (alignment <= _spatialCosAngleCheck * magnitude) { continue; }

                float score = alignment / sqrMagnitude;
                if (score <= bestScore) { continue; }

                bestScore = score;
                bestChoice = candidate;
            }
            return bestChoice != null;
        }
        
        internal static bool TryRayIntersectsRect(Vector2 origin, Vector2 direction, Rect rect, out float distance)
        {
            // Ray parameterizes as:
            // point(t) = origin + t * direction
            
            distance = 0f;
            float tMin = float.NegativeInfinity; // point where ray first enters slab
            float tMax = float.PositiveInfinity; // point where ray first exits slab

            // X-Axis
            if (Mathf.Approximately(direction.x, 0f)) { if (origin.x < rect.xMin || origin.x > rect.xMax) { return false; } }
            else
            {
                float t1 = (rect.xMin - origin.x) / direction.x; // distance travelled to left edge
                float t2 = (rect.xMax - origin.x) / direction.x; // distance travelled to right edge
                if (t1 > t2) { (t1, t2) = (t2, t1); } // swap to ensure t1 is entry point, t2 is exit point
                tMin = Mathf.Max(tMin, t1);
                tMax = Mathf.Min(tMax, t2);
                if (tMin > tMax) { return false; } // no overlap, ray has missed the slab entirely
            }

            // Y-Axis
            if (Mathf.Approximately(direction.y, 0f)) { if (origin.y < rect.yMin || origin.y > rect.yMax) { return false; } }
            else
            {
                float t1 = (rect.yMin - origin.y) / direction.y; // distance travelled to bottom edge
                float t2 = (rect.yMax - origin.y) / direction.y; // distance travelled to top edge
                if (t1 > t2) { (t1, t2) = (t2, t1); } // swap to ensure t1 is entry point, t2 is exit point
                tMin = Mathf.Max(tMin, t1);
                tMax = Mathf.Min(tMax, t2);
                if (tMin > tMax) { return false; } // no overlap, ray has missed the slab entirely
            }

            if (tMax < 0f) { return false; } // rect is entirely behind the ray origin

            distance = tMin >= 0f ? tMin : tMax; // tMin negative implies origin was in candidate bounds to begin with
            return true;
        }
        #endregion
    }
}
