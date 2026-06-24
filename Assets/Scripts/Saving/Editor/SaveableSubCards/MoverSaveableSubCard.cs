using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Frankie.Control;
using Frankie.Utils;

namespace Frankie.Saving.Editor
{
    public class MoverSaveableSubCard : SaveableSubCardData
    {
        // Const
        private const string _moveEntityText = "Move Entity to Saved Position";
        private const string _pickButtonIdleText = "Pick Position in Scene";
        private const string _pickButtonActiveText = "Click in Scene View";
        private static readonly Color _pickButtonActiveColor = Color.forestGreen;
        
        // State
        private bool isPicking = false;
        private Action<SceneView> activeSceneGUIHandler;
        private Action activeFocusChangedHandler;

        public MoverSaveableSubCard(ISaveableBase saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }
        
        public bool IsPlayerMoverSubCard() => saveable is PlayerMover;

        public override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            CancelPickingIfActive();
            if (saveable is not Mover mover) { return; }
            
            SerializableVector2 savedPosition = mover.ManualGetDataFromState(saveState);
            if (savedPosition == null)
            {
                subCardView.Add(new Label("No position currently saved"));
                return;
            }
            
            float xPosition = savedPosition.x;
            float yPosition = savedPosition.y;

            var xRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(xRow);
            xRow.Add(new Label("X:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });
            var xField = new FloatField { value = xPosition, isDelayed = true, style = { flexGrow = 1 } };
            xRow.Add(xField);

            var yRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(yRow);
            yRow.Add(new Label("Y:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });
            var yField = new FloatField { value = yPosition, isDelayed = true, style = { flexGrow = 1 } };
            yRow.Add(yField);

            var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(buttonRow);
            
            buttonRow.Add(new Label(string.Empty) { style = { width = 120 } });
            var moveButton = new Button { text = _moveEntityText, style = { flexGrow = 1 } }; 
            buttonRow.Add(moveButton);
            buttonRow.Add(new Label(string.Empty) { style = { width = 10 } });
            var pickButton = new Button { text = _pickButtonIdleText, style = { flexGrow = 1 } };
            buttonRow.Add(pickButton);

            xField.RegisterValueChangedCallback(changeEvent =>
            {
                xPosition = (float)Math.Round(changeEvent.newValue, 2, MidpointRounding.AwayFromZero);
                xField.SetValueWithoutNotify(xPosition);
                PushSaveState();
            });

            yField.RegisterValueChangedCallback(changeEvent =>
            {
                yPosition = (float)Math.Round(changeEvent.newValue, 2, MidpointRounding.AwayFromZero);
                yField.SetValueWithoutNotify(yPosition);
                PushSaveState();
            });

            moveButton.RegisterCallback<ClickEvent>(MoveToPosition);

            pickButton.RegisterCallback<ClickEvent>(_ =>
            {
                if (isPicking)
                {
                    StopPicking(pickButton);
                    return;
                }
                StartPicking(pickButton, OnScenePositionPicked);
            });
            return;

            
            // Local Functions
            void MoveToPosition(ClickEvent clickEvent)
            {
                if (saveable is not Component component) { return; }
                component.gameObject.transform.position = new Vector2(xPosition, yPosition);
            }
            
            void OnScenePositionPicked(Vector2 pickedPosition)
            {
                xPosition = (float)Math.Round(pickedPosition.x, 2, MidpointRounding.AwayFromZero);
                yPosition = (float)Math.Round(pickedPosition.y, 2, MidpointRounding.AwayFromZero);
                xField.SetValueWithoutNotify(xPosition);
                yField.SetValueWithoutNotify(yPosition);
                PushSaveState();
            }
            
            void PushSaveState()
            {
                Vector3 updatedPosition = new(xPosition, yPosition, 0f);
                var serializablePosition = new SerializableVector2(updatedPosition);
                saveState = mover.ManualGetStateFromData(serializablePosition);
                RaiseSaveStateChanged();
            }
        }

        #region ScenePositionPicking
        private void StartPicking(Button pickButton, Action<Vector2> onPicked)
        {
            isPicking = true;
            pickButton.text = _pickButtonActiveText;
            pickButton.style.backgroundColor = _pickButtonActiveColor;

            if (activeSceneGUIHandler != null) { SceneView.duringSceneGui -= activeSceneGUIHandler; }
            SceneView.duringSceneGui += OnSceneGUI;
            activeSceneGUIHandler = OnSceneGUI;
            
            if (activeFocusChangedHandler != null) { EditorWindow.windowFocusChanged -= OnFocusedWindowChanged;  }
            EditorWindow.windowFocusChanged += OnFocusedWindowChanged;
            activeFocusChangedHandler = OnFocusedWindowChanged;
            
            return;
            
            
            // Local Functions
            void OnFocusedWindowChanged()
            {
                if (EditorWindow.focusedWindow is SceneView) { return; }
                StopPicking(pickButton);
            }
            
            void OnSceneGUI(SceneView sceneView)
            {
                Event current = Event.current;
                if (current == null) { return; }

                switch (current.type)
                {
                    case EventType.KeyDown when current.keyCode == KeyCode.Escape:
                    case EventType.MouseDown when current.button == 1:
                        current.Use();
                        StopPicking(pickButton);
                        return;
                    case EventType.MouseDown when current.button == 0:
                    {
                        Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
                        var zPlane = new Plane(Vector3.forward, Vector3.zero);
                        if (zPlane.Raycast(ray, out float distance))
                        {
                            Vector3 worldPoint = ray.GetPoint(distance);
                            onPicked?.Invoke(new Vector2(worldPoint.x, worldPoint.y));
                        }

                        current.Use();
                        StopPicking(pickButton);
                        break;
                    }
                }
            }
        }
        
        private void CancelPickingIfActive()
        {
            if (!isPicking) { return; }
            isPicking = false;
            
            if (activeSceneGUIHandler != null)
            {
                SceneView.duringSceneGui -= activeSceneGUIHandler;
                activeSceneGUIHandler = null;
            }
            if (activeFocusChangedHandler != null)
            {
                EditorWindow.windowFocusChanged -= activeFocusChangedHandler;
                activeFocusChangedHandler = null;
            }
        }
        
        private void StopPicking(Button pickButton)
        {
            isPicking = false;
            pickButton.text = _pickButtonIdleText;
            pickButton.style.backgroundColor = StyleKeyword.Null;

            if (activeSceneGUIHandler != null)
            {
                SceneView.duringSceneGui -= activeSceneGUIHandler;
                activeSceneGUIHandler = null;
            }
            if (activeFocusChangedHandler != null)
            {
                EditorWindow.windowFocusChanged -= activeFocusChangedHandler;
                activeFocusChangedHandler = null;
            }
        }
        #endregion
    }
}
