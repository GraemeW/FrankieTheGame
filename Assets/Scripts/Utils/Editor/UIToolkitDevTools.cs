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
        private const string _copyUSSAssetURLMenuItem = "Assets/Copy USS Asset URL";
        private const string _captureDirectory = "Temp/UICaptures";
        private const string _testSimpleMessage = "Wow! Looks like we're testing a simple message.  This extra-long test sentence checks that dialogue text wraps nicely inside the box.";
        private const string _testOptionMessage = "Should we be testing this option message?";

        #region MenuItems
        [MenuItem(_menuRoot + "Capture Game View", false, 20)]
        private static void CaptureGameView()
        {
            Directory.CreateDirectory(_captureDirectory);
            string capturePath = Path.Combine(_captureDirectory, $"{DateTime.Now:yyyyMMdd-HHmmss}.png");
            ScreenCapture.CaptureScreenshot(capturePath);
            Debug.Log($"[UIToolkitDevTools] Game view capture queued:  {Path.GetFullPath(capturePath)}");
        }
        
        // Hover the Game view and press Cmd/Ctrl+Alt+U:  logs what UI Toolkit panels pick and the EventSystem's raycast priority order
        [MenuItem(_menuRoot + "Log Pointer Targets %&u", false, 21)]
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

            // World-space panels are picked via 3D physics rays against a collider on the panel's GameObject
            log.AppendLine("World-space panels:");
            foreach (PanelRenderer panelRenderer in Object.FindObjectsByType<PanelRenderer>())
            {
                if (panelRenderer.panelSettings == null || panelRenderer.panelSettings.renderMode != PanelRenderMode.WorldSpace) { continue; }

                var panelDescription = new StringBuilder($"  {panelRenderer.name} (layer {LayerMask.LayerToName(panelRenderer.gameObject.layer)}, position {panelRenderer.transform.position}, scale {panelRenderer.transform.lossyScale}, bounds {panelRenderer.bounds}):");
                Collider[] panelColliders = panelRenderer.GetComponentsInChildren<Collider>(true);
                if (panelColliders.Length == 0) { panelDescription.Append("  no collider"); }
                foreach (Collider panelCollider in panelColliders)
                {
                    panelDescription.Append($"  {panelCollider.GetType().Name} on {panelCollider.name} (enabled {panelCollider.enabled}, trigger {panelCollider.isTrigger}, bounds {panelCollider.bounds})");
                }
                log.AppendLine(panelDescription.ToString());
            }

            PanelInputConfiguration panelInputConfiguration = Object.FindAnyObjectByType<PanelInputConfiguration>();
            log.AppendLine(panelInputConfiguration == null
                ? "PanelInputConfiguration:  none (defaults)"
                : $"PanelInputConfiguration:  processWorldSpaceInput {panelInputConfiguration.processWorldSpaceInput}, interactionLayers {panelInputConfiguration.interactionLayers.value}, maxInteractionDistance {panelInputConfiguration.maxInteractionDistance}, redirection {panelInputConfiguration.panelInputRedirection}");

            log.AppendLine("Physics ray from the main camera (nearest first, triggers included):");
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Ray pointerRay = mainCamera.ScreenPointToRay(screenPosition);
                // ReSharper disable once Unity.PreferNonAllocApi
                RaycastHit[] raycastHits = Physics.RaycastAll(pointerRay, Mathf.Infinity, Physics.AllLayers, QueryTriggerInteraction.Collide);
                Array.Sort(raycastHits, (first, second) => first.distance.CompareTo(second.distance));
                if (raycastHits.Length == 0) { log.AppendLine($"  No hits (ray origin {pointerRay.origin}, direction {pointerRay.direction})"); }
                foreach (RaycastHit raycastHit in raycastHits)
                {
                    log.AppendLine($"  {raycastHit.collider.name} (layer {LayerMask.LayerToName(raycastHit.collider.gameObject.layer)}, distance {raycastHit.distance})");
                }
            }
            else { log.AppendLine("  No main camera"); }
            
            Debug.Log(log.ToString());
        }

        [MenuItem(_menuRoot + "Spawn Simple Message", false, 31)]
        private static void SpawnSimpleMessage()
        {
            if (!TryGetPlayerStateMachine(out PlayerStateMachine playerStateMachine)) { return; }
            playerStateMachine.EnterDialogue(_testSimpleMessage);
        }

        [MenuItem(_menuRoot + "Spawn Simple Option", false, 32)]
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

        [MenuItem(_menuRoot + "Spawn Selected Dialogue", false, 33)]
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
        
        [MenuItem(_menuRoot + "Capture Game View", true)]
        [MenuItem(_menuRoot + "Log Pointer Targets %&u", true)]
        [MenuItem(_menuRoot + "Spawn Simple Message", true)]
        [MenuItem(_menuRoot + "Spawn Simple Option", true)]
        [MenuItem(_menuRoot + "Spawn Selected Dialogue", true)]
        private static bool ValidatePlayMode() => Application.isPlaying;
        #endregion

        #region AssetMenuItems
        [MenuItem(_copyUSSAssetURLMenuItem, false, 19)]
        private static void CopyUSSAssetURL()
        {
            Object asset = Selection.activeObject;
            string assetPath = AssetDatabase.GetAssetPath(asset);
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localFileID)) { return; }

            int fileType = AssetDatabase.IsNativeAsset(asset) ? 2 : 3; // 2: serialized by Unity (e.g. .asset), 3: imported (e.g. .png, .ttf)
            string ussAssetURL = $"url(\"project://database/{Uri.EscapeUriString(assetPath)}?fileID={localFileID}&guid={guid}&type={fileType}#{asset.name}\")";
            EditorGUIUtility.systemCopyBuffer = ussAssetURL;
            Debug.Log($"[UIToolkitDevTools] Copied USS asset url:  {ussAssetURL}", asset);
        }

        [MenuItem(_copyUSSAssetURLMenuItem, true)]
        private static bool ValidateCopyUSSAssetURL() => Selection.activeObject != null && AssetDatabase.Contains(Selection.activeObject);
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
