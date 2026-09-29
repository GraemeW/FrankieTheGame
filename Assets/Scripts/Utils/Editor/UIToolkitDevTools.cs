using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using LowDefMustard.Utils;
using Frankie.Core;
using Frankie.Speech;
using Object = UnityEngine.Object;

namespace Frankie.Utils.UI.Editor
{
    public static class UIToolkitDevTools
    {
        private const string _menuRoot = "Tools/UIDevTools/";
        private const string _captureDirectory = "Temp/UICaptures";
        private const string _testSimpleMessage = "Wow! Looks like we're testing a simple message.  This extra-long test sentence checks that dialogue text wraps nicely inside the box.";
        private const string _testOptionMessage = "Should we be testing this option message?";

        #region MenuItems
        [MenuItem(_menuRoot + "Capture Game View")]
        private static void CaptureGameView()
        {
            Directory.CreateDirectory(_captureDirectory);
            string capturePath = Path.Combine(_captureDirectory, $"{DateTime.Now:yyyyMMdd-HHmmss}.png");
            ScreenCapture.CaptureScreenshot(capturePath);
            Debug.Log($"[UIToolkitDevTools] Game view capture queued:  {Path.GetFullPath(capturePath)}");
        }

        [MenuItem(_menuRoot + "Spawn Simple Message")]
        private static void SpawnSimpleMessage()
        {
            if (!TryGetPlayerStateMachine(out PlayerStateMachine playerStateMachine)) { return; }
            playerStateMachine.EnterDialogue(_testSimpleMessage);
        }

        [MenuItem(_menuRoot + "Spawn Simple Option")]
        private static void SpawnSimpleOption()
        {
            if (!TryGetPlayerStateMachine(out PlayerStateMachine playerStateMachine)) { return; }
            var choiceActionPairs = new List<ChoiceActionPair>
            {
                new("Yah", () => Debug.Log("[UIToolkitDevTools] Yah chosen")),
                new("Nah", () => Debug.Log("[UIToolkitDevTools] Nah chosen"))
            };
            playerStateMachine.EnterDialogue(_testOptionMessage, choiceActionPairs);
        }

        [MenuItem(_menuRoot + "Spawn Selected Dialogue")]
        private static void SpawnSelectedDialogue()
        {
            if (Selection.activeObject is not Dialogue dialogue)
            {
                Debug.LogWarning("[UIToolkitDevTools] Select a Dialogue asset in the Project window first.");
                return;
            }
            if (!TryGetPlayerStateMachine(out PlayerStateMachine playerStateMachine)) { return; }
            
            var aiConversant = Object.FindAnyObjectByType<AIConversant>();
            if (aiConversant == null)
            {
                Debug.LogWarning("[UIToolkitDevTools] No AIConversant found in scene to host the dialogue.");
                return;
            }
            playerStateMachine.EnterDialogue(aiConversant, dialogue);
        }

        // Hover the Game view and press Cmd/Ctrl+Alt+U:  logs what UI Toolkit panels pick and the EventSystem's raycast priority order
        [MenuItem(_menuRoot + "Log Pointer Targets %&u")]
        private static void LogPointerTargets()
        {
            if (Mouse.current == null) { return; }
            Vector2 screenPosition = Mouse.current.position.ReadValue();
            var log = new StringBuilder($"[UIToolkitDevTools] Pointer targets at screen {screenPosition}\n");

            log.AppendLine("UI Toolkit picks:");
            foreach (PanelRenderer panelRenderer in Object.FindObjectsByType<PanelRenderer>())
            {
                VisualElement rootElement = GetRootElement(panelRenderer);
                IPanel panel = rootElement?.panel;
                if (panel == null) { continue; }
                
                Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));
                VisualElement picked = panel.Pick(panelPosition);
                string pickedDescription = picked == null ? "none" : $"{picked.GetType().Name} [{string.Join(" ", picked.GetClasses())}]";
                log.AppendLine($"  {panelRenderer.name} (panel pos {panelPosition}):  {pickedDescription}");
            }

            log.AppendLine("EventSystem raycast (highest priority first):");
            if (EventSystem.current != null)
            {
                var raycastResults = new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screenPosition }, raycastResults);
                foreach (RaycastResult raycastResult in raycastResults)
                {
                    log.AppendLine($"  {(raycastResult.gameObject != null ? raycastResult.gameObject.name : "null")} via {raycastResult.module?.GetType().Name} (sortingOrder {raycastResult.sortingOrder}, depth {raycastResult.depth})");
                }
            }
            else { log.AppendLine("  No EventSystem.current"); }
            
            Debug.Log(log.ToString());
        }

        [MenuItem(_menuRoot + "Log Pointer Targets %&u", true)]
        [MenuItem(_menuRoot + "Capture Game View", true)]
        [MenuItem(_menuRoot + "Spawn Simple Message", true)]
        [MenuItem(_menuRoot + "Spawn Simple Option", true)]
        [MenuItem(_menuRoot + "Spawn Selected Dialogue", true)]
        private static bool ValidatePlayMode() => Application.isPlaying;
        #endregion

        #region PrivateMethods
        private static VisualElement GetRootElement(PanelRenderer panelRenderer)
        {
            // Note:  PanelRenderer exposes its root only via reload callbacks, invoked immediately when the UI is already loaded
            VisualElement rootElement = null;
            PanelRenderer.VersionedUIReloadCallback captureRoot = (_, reloadedRootElement, _) => rootElement = reloadedRootElement;
            panelRenderer.RegisterUIReloadCallback(captureRoot);
            panelRenderer.UnregisterUIReloadCallback(captureRoot);
            return rootElement;
        }

        private static bool TryGetPlayerStateMachine(out PlayerStateMachine playerStateMachine)
        {
            playerStateMachine = Object.FindAnyObjectByType<PlayerStateMachine>();
            if (playerStateMachine != null) { return true; }
            
            Debug.LogWarning("[UIToolkitDevTools] No PlayerStateMachine found -- enter a world scene first.");
            return false;
        }
        #endregion
    }
}
