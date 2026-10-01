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
        
        // Tunables
        [Header("UI Box Hookups")]
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected Transform optionParent;
        [SerializeField] protected Transform backExitParent;
        
        [Header("UI Box Prefabs")] 
        [SerializeField] protected UIBackExit backExitPrefab;
        [SerializeField] protected GameObject optionButtonPrefab;
        [SerializeField] protected GameObject optionSliderPrefab;
        
        // Const/Static Fixed
        private const float _spatialAngleCheck = 85f;
        private static readonly float _spatialCosAngleCheck = Mathf.Cos(_spatialAngleCheck * Mathf.Deg2Rad);
        
        // Key State Parameters
        protected bool handleGlobalInput { get; set; } = true;
        protected bool clearVolatileOptionsOnEnable { get; set; } = true;
        protected bool preventEscapeOptionExit { get; set; } = false;
        
        // State -- Standard
        protected BaseController controller;
        public bool destroyQueued { get; set; } = false;

        // State -- Choices
        private bool isChoiceAvailable = false;
        private bool clearDisableCallbacksOnChoose = false;
        protected readonly List<IUIChoice> choiceOptions = new();
        protected IUIChoice highlightedChoiceOption;

        // Cached References
        private Camera renderCamera; // Only relevant if canvas != overlay
        private IUIBoxView boxView; // when null -> uses legacy uGUI hookups
        private bool isBoxViewResolved = false;

        // Event Handles
        private event Action<ReceiverModifiedType, ReceiverModifiedData> receiverModified;
        
        #region UnityMethods

        protected virtual void Start()
        {
            var canvas = GetComponentInParent<Canvas>();
            renderCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        }
        #endregion
        
        #region UtilityMethods
        public void SetHandleGlobalInput(bool enable) => handleGlobalInput = enable; // Warning:  Only use if a known receiver exists to handle input
        public void SubscribeToReceiverUpdates(bool enable, Action<ReceiverModifiedType, ReceiverModifiedData> action)
        {
            receiverModified -= action;
            if (enable) { receiverModified += action; }
        }
        
        protected void TriggerUIBoxModified(ReceiverModifiedType dialogueBoxModifiedType, ReceiverModifiedData uiBoxModifiedData) => receiverModified?.Invoke(dialogueBoxModifiedType, uiBoxModifiedData);
        protected abstract void SimpleTriggerUIBoxModified(ReceiverModifiedType dialogueBoxModifiedType); // Implemented via UIBox
        protected void SetVisible(bool enable)
        {
            if (TryGetBoxView(out IUIBoxView view)) { view.SetVisible(enable); return; }
            canvasGroup.alpha = enable ? 1.0f : 0.0f;
        }

        protected bool TryGetBoxView(out IUIBoxView view)
        {
            if (!isBoxViewResolved)
            {
                TryGetComponent(out boxView);
                isBoxViewResolved = true;
            }
            view = boxView;
            return view != null;
        }
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

        // Transitional:  legacy uGUI choices play their own highlight/select sounds (per-choice soundboxes)
        // Remove the view gate alongside the legacy uGUI path
        protected void TriggerChoiceModified(ReceiverModifiedType receiverModifiedType)
        {
            if (!TryGetBoxView(out IUIBoxView _)) { return; }
            SimpleTriggerUIBoxModified(receiverModifiedType);
        }

        protected IUIChoice AddChoiceOption(string choiceText, Action action) => CreateChoiceOption(choiceText, () => StandardChoiceExecution(action, true));

        protected IUIChoice AddNonDestroyChoiceOption(string choiceText, Action action) => CreateChoiceOption(choiceText, () => StandardChoiceExecution(action, false));
        
        protected void AddSeparator(ChoiceSeparatorType separatorType = ChoiceSeparatorType.Major) { if (TryGetBoxView(out IUIBoxView view)) { view.CreateChoiceSeparator(separatorType); } }

        private IUIChoice CreateChoiceOption(string choiceText, Action onChoose)
        {
            int choiceOrder = choiceOptions.Count + 1;
            if (TryGetBoxView(out IUIBoxView view))
            {
                IUIChoice viewChoice = view.CreateChoiceOption(choiceText, choiceOrder, onChoose);
                choiceOptions.Add(viewChoice);
                return viewChoice;
            }

            // Legacy uGUI approach
            GameObject uiChoiceOptionObject = Instantiate(optionButtonPrefab, optionParent);
            var uiChoiceOption = uiChoiceOptionObject.GetComponent<UIChoiceButton>();
            uiChoiceOption.SetChoiceOrder(choiceOrder);
            uiChoiceOption.SetText(choiceText);
            uiChoiceOption.AddOnClickListener(delegate { onChoose(); });
            choiceOptions.Add(uiChoiceOption);
            return uiChoiceOption;
        }

        protected void ClearChoiceSelections()
        {
            highlightedChoiceOption = null;
            foreach (IUIChoice choiceOption in choiceOptions.Where(IsChoiceAlive))
            {
                choiceOption.Highlight(false);
            }
        }
        
        protected static List<UIChoice> FilterOutSubOptions(List<UIChoice> uiChoices)
        {
            List<UIChoice> filteredUIChoices = uiChoices.ToList();
            var subOptions = new List<UIChoice>();
            foreach (UIChoice choice in filteredUIChoices)
            {
                if (choice is UIChoiceContainer uiChoiceContainer)
                {
                    subOptions.AddRange(uiChoiceContainer.GetSubOptions());
                }
            }
            foreach (UIChoice choice in subOptions) { filteredUIChoices.Remove(choice); }
            return filteredUIChoices;
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
            if (!highlightedChoiceOption.TryGetScreenRect(renderCamera, out Rect originRect)) { return false; }

            Vector2 origin = originRect.center;
            if (!TryFindClosestRayHit(origin, direction, renderCamera, choiceOptions, highlightedChoiceOption, out IUIChoice targetChoice))
            {
                TryFindBestAngleMatch(origin, direction, renderCamera, choiceOptions, highlightedChoiceOption, out targetChoice);
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
        
        #endregion

        #region PrivateStaticMethods
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
        
        internal static bool TryFindClosestRayHit(Vector2 origin, Vector2 direction, Camera renderCamera, IEnumerable<IUIChoice> choiceOptions, IUIChoice highlightedChoiceOption, out IUIChoice closestChoice)
        {
            closestChoice = null;

            float closestDistance = float.PositiveInfinity;
            foreach (IUIChoice candidate in choiceOptions.Where(candidate => IsChoiceAlive(candidate) && candidate != highlightedChoiceOption))
            {
                if (!candidate.TryGetScreenRect(renderCamera, out Rect candidateRect)) { continue; }
                if (!TryRayIntersectsRect(origin, direction, candidateRect, out float distance)) { continue; }
                if (distance >= closestDistance) { continue; }

                closestDistance = distance;
                closestChoice = candidate;
            }
            return closestChoice != null;
        }
        
        internal static bool TryFindBestAngleMatch(Vector2 origin, Vector2 direction, Camera renderCamera, IEnumerable<IUIChoice> choiceOptions, IUIChoice highlightedChoiceOption, out IUIChoice bestChoice)
        {
            bestChoice = null;
            
            float bestScore = float.NegativeInfinity;
            foreach (IUIChoice candidate in choiceOptions.Where(candidate => IsChoiceAlive(candidate) && candidate != highlightedChoiceOption))
            {
                if (!candidate.TryGetScreenRect(renderCamera, out Rect candidateRect)) { continue; }

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
        
        internal static bool TryGetScreenRect(Camera renderCamera, RectTransform rectTransform, out Rect screenRect)
        {
            screenRect = default;
            if (rectTransform == null) { return false; }

            var worldCorners = new Vector3[4];
            rectTransform.GetWorldCorners(worldCorners);

            Vector2 min = RectTransformUtility.WorldToScreenPoint(renderCamera, worldCorners[0]);
            Vector2 max = min;
            for (int i = 1; i < 4; i++)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(renderCamera, worldCorners[i]);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            screenRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return true;
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
