using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace Frankie.Utils.Editor
{
    //  Expected source filename pattern: [CharacterName] - [Action] - [Frame#].png
    //  Note:  For every recognized movement direction, an idle clip is also generated as [CharacterName]Idle[Direction].anim
    //      - using "Idle" frames if present
    //      - , otherwise using Down frames for Down Idle
    //      - , otherwise using single frame for other directions
    //  Note:  One StandStill clip is also generated per character, as
    //      - using Static frames if present
    //      - , otherwise using single frame from Down 
    public class SpriteAnimationGeneratorWindow : EditorWindow
    {
        // Fixed Const/Static Tunables
        private const string _filenamePattern = @"^(?<char>.+?)\s*-\s*(?<dir>.+?)\s*-\s*(?<frame>\d+)\.png$";
            // Note:  char, dir and frame are string references used in match below
        
        private static readonly Dictionary<string, string> _directionAliasMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Down", "Down" },
            { "Front", "Down" },
            { "Up", "Up" },
            { "Back", "Up" },
            { "Left", "Left" },
            { "Right", "Right" },
            { "DownLeft", "DownLeft" },
            { "FrontLeft", "DownLeft" },
            { "DownRight", "DownRight" },
            { "FrontRight", "DownRight" },
            { "UpLeft", "UpLeft" },
            { "BackLeft", "UpLeft" },
            { "UpRight", "UpRight" },
            { "BackRight", "UpRight" },
        };
        private static readonly HashSet<string> _canonicalDirections = new(_directionAliasMap.Values, StringComparer.OrdinalIgnoreCase);
        private static readonly string[] _idleTokens = { "Idle" };
        private const string _standStillToken = "Static";

        // UI State
        private ObjectField referenceClipField;
        private ObjectField sourceFolderField;
        private ObjectField outputFolderField;
        private ObjectField overrideControllerField;
        private TextField prefixField;
        private FloatField frameRateField;
        private FloatField idleFrameRateField;
        private Toggle overwriteToggle;
        private Button generateButton;
        private Label logLabel;
        
        #region UnityMethods
        [MenuItem("Tools/Sprite Animation Generator", false, 501)]
        private static void Open()
        {
            var window = GetWindow<SpriteAnimationGeneratorWindow>("Sprite Animation Generator");
            window.minSize = new Vector2(440, 320);
        }

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            InitializeRootElement(root);

            // Input Reference / Animation Tunables
            root.Add(CreateSectionLabel("Animation Generation Tunables"));

            referenceClipField = CreateStandardObjectField("Reference Clip", typeof(AnimationClip));
            sourceFolderField = CreateStandardObjectField("Source Art Folder", typeof(DefaultAsset));
            outputFolderField = CreateStandardObjectField("Output Folder", typeof(DefaultAsset));

            root.Add(referenceClipField);
            root.Add(sourceFolderField);
            root.Add(outputFolderField);

            prefixField = CreateStandardTextField("Prefix", string.Empty, "Optional. Prepended to every generated clip name");
            root.Add(prefixField);

            frameRateField = CreateStandardFloatField("Frame Rate", 4f);
            root.Add(frameRateField);

            idleFrameRateField = CreateStandardFloatField("Idle Frame Rate", 2f);
            root.Add(idleFrameRateField);

            referenceClipField.RegisterValueChangedCallback(evt => { if (evt.newValue is AnimationClip clip) { frameRateField.value = clip.frameRate; } });
            referenceClipField.RegisterValueChangedCallback(_ => RefreshButtonState());
            sourceFolderField.RegisterValueChangedCallback(_ => RefreshButtonState());
            outputFolderField.RegisterValueChangedCallback(_ => RefreshButtonState());
            
            // Override Controller Tunables
            root.Add(CreateSpacer());
            root.Add(CreateSectionLabel("Controller Link"));

            overrideControllerField = CreateStandardObjectField("Override Controller", typeof(AnimatorOverrideController));
            root.Add(overrideControllerField);

            overwriteToggle = new Toggle("Overwrite Existing Clips") { value = true };
            root.Add(overwriteToggle);

            // Execution
            root.Add(CreateSpacer());
            root.Add(CreateSectionLabel("Execution"));
            
            generateButton = CreateStandardButton("Generate Animations");
            generateButton.RegisterCallback<ClickEvent>(_ => Generate());
            generateButton.SetEnabled(false);
            root.Add(generateButton);

            root.Add(CreateSpacer());

            ScrollView logScroll = CreateStandardScrollView();
            logLabel = new Label(string.Empty) { style = { whiteSpace = WhiteSpace.Normal } };
            logScroll.Add(logLabel);
            root.Add(logScroll);
        }
        
        private void RefreshButtonState()
        {
            generateButton.SetEnabled(
                referenceClipField.value != null &&
                sourceFolderField.value != null &&
                outputFolderField.value != null);
        }
        #endregion
        
        #region GenerationAnimations
        private void Generate()
        {
            var referenceClip = referenceClipField.value as AnimationClip;
            var sourceFolder = sourceFolderField.value as DefaultAsset;
            var outputFolder = outputFolderField.value as DefaultAsset;
            var overrideController = overrideControllerField.value as AnimatorOverrideController;
            bool overwriteExisting = overwriteToggle.value;
            string prefix = prefixField.value?.Trim() ?? string.Empty;

            string sourcePath = AssetDatabase.GetAssetPath(sourceFolder);
            string outputPath = AssetDatabase.GetAssetPath(outputFolder);
            EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(referenceClip);

            if (!AreInputsValid(referenceClip, bindings, sourcePath, outputPath)) { return; }

            // Note:  Only valid for standard prefabs with a single sprite-bound renderer
            EditorCurveBinding spriteBinding = bindings[0];
            AnimationClipSettings refSettings = AnimationUtility.GetAnimationClipSettings(referenceClip);
            float frameRate = frameRateField.value;
            float idleFrameRate = idleFrameRateField.value;

            var regex = new Regex(_filenamePattern);
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { sourcePath });

            var frameEntries = new List<FrameEntry>();
            var passthroughActions = new HashSet<string>();

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileName(assetPath);
                Match match = regex.Match(fileName);
                if (!match.Success) { continue; }

                string rawAction = match.Groups["dir"].Value.Trim();
                ActionClassification actionClassification = ClassifyAction(rawAction);

                if (!actionClassification.isRecognized) { passthroughActions.Add(rawAction); }

                frameEntries.Add(new FrameEntry(
                    match.Groups["char"].Value.Trim(),
                    actionClassification.resolvedAction,
                    int.Parse(match.Groups["frame"].Value),
                    assetPath,
                    actionClassification.isIdleSource,
                    actionClassification.isStandStillSource));
            }

            if (!HasFrameEntries(frameEntries)) { return; }

            // Dedicated Idle Art
            var idleSourceSprites = frameEntries
                .Where(e => e.isIdleSource)
                .GroupBy(e => (e.character, e.action))
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(e => e.frame)
                        .Select(e => AssetDatabase.LoadAssetAtPath<Sprite>(e.assetPath))
                        .Where(s => s != null)
                        .ToArray());

            // Dedicated StandStill Art
            var standStillSourceSprites = frameEntries
                .Where(e => e.isStandStillSource)
                .GroupBy(e => e.character)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(e => e.frame)
                        .Select(e => AssetDatabase.LoadAssetAtPath<Sprite>(e.assetPath))
                        .FirstOrDefault(s => s != null));

            // Primary Art
            var mainGroups = frameEntries
                .Where(e => !e.isIdleSource && !e.isStandStillSource)
                .GroupBy(e => (e.character, e.action))
                .OrderBy(g => g.Key.character)
                .ThenBy(g => g.Key.action);

            var log = new AnimationBuildLog();
            var downFirstFrameByCharacter = new Dictionary<string, Sprite>();

            AnimationConfig animationConfig;
            foreach (var group in mainGroups)
            {
                string characterName = group.Key.character;
                string action = group.Key.action;
                string clipName = $"{prefix}{characterName}{action}";
                string clipAssetPath = $"{outputPath}/{clipName}.anim";

                Sprite[] orderedSprites = group
                    .OrderBy(e => e.frame)
                    .Select(e => AssetDatabase.LoadAssetAtPath<Sprite>(e.assetPath))
                    .Where(s => s != null)
                    .ToArray();

                var animationData = new StandardAnimationData(clipAssetPath, clipName, orderedSprites);
                animationConfig = new AnimationConfig(frameRate, spriteBinding, refSettings);
                WriteAnimationClip(animationData, animationConfig, overwriteExisting, overrideController, log);

                if (orderedSprites.Length > 0 && string.Equals(action, "Down", StringComparison.OrdinalIgnoreCase))
                {
                    downFirstFrameByCharacter[characterName] = orderedSprites[0];
                }
                
                if (orderedSprites.Length > 0 && _canonicalDirections.Contains(action))
                {
                    animationConfig = new AnimationConfig(idleFrameRate, spriteBinding, refSettings);
                    IdleAnimationData idleAnimationData = new IdleAnimationData(prefix, characterName, action, orderedSprites, idleSourceSprites, outputPath);
                    GenerateIdleClip(idleAnimationData, animationConfig, overwriteExisting, overrideController, log);
                }
            }
            
            foreach (string characterName in frameEntries.Select(e => e.character).Distinct())
            {
                animationConfig = new AnimationConfig(idleFrameRate, spriteBinding, refSettings);
                downFirstFrameByCharacter.TryGetValue(characterName, out Sprite downFirstFrame);
                var inputAnimationData = new StandStillAnimationData(prefix, characterName, standStillSourceSprites, downFirstFrame, outputPath);
                GenerateStandStillClip(inputAnimationData, animationConfig, overwriteExisting, overrideController, log);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            log.AnnotatePassthroughActions(passthroughActions);
            log.SummarizeGeneration();
            logLabel.text = log.log.ToString();
        }
        
        private bool AreInputsValid(AnimationClip referenceClip, EditorCurveBinding[] bindings, string sourcePath, string outputPath)
        {
            if (!AssetDatabase.IsValidFolder(sourcePath) || !AssetDatabase.IsValidFolder(outputPath))
            {
                logLabel.text = "Both source and output must be valid project folders.";
                return false;
            }
            
            if (bindings == null || bindings.Length == 0)
            {
                if (referenceClip != null) logLabel.text = $"Reference clip '{referenceClip.name}' has no object reference (sprite) curves.";
                return false;
            }

            if (frameRateField.value <= 0f)
            {
                logLabel.text = "Frame Rate must be greater than 0.";
                return false;
            }

            if (idleFrameRateField.value <= 0f)
            {
                logLabel.text = "Idle Frame Rate must be greater than 0.";
                return false;
            }
            return true;
        }

        private bool HasFrameEntries(List<FrameEntry> frameEntries)
        {
            if (frameEntries == null || frameEntries.Count == 0)
            {
                logLabel.text = "No files matched pattern: [CharacterName] - [Action] - [Frame#].png";
                return false;
            }
            return true;
        }
        #endregion
        
        #region StaticHelpers
        private static ActionClassification ClassifyAction(string rawAction)
        {
            if (rawAction.IndexOf(_standStillToken, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new ActionClassification(rawAction, false, true, true);
            }

            foreach (string token in _idleTokens)
            {
                int idx = rawAction.IndexOf(token, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) continue;

                string remainder = rawAction.Remove(idx, token.Length).Trim();
                if (_directionAliasMap.TryGetValue(remainder, out string canonicalFromIdle))
                {
                    return new ActionClassification(canonicalFromIdle, true, false, true);
                }
            }

            return _directionAliasMap.TryGetValue(rawAction, out string canonical) ? 
                new ActionClassification(canonical, false, false, true) : 
                new ActionClassification(rawAction, false, false, false);
        }
        
        private static void GenerateStandStillClip(StandStillAnimationData inputAnimationData, AnimationConfig animationConfig, bool overwriteExisting, AnimatorOverrideController overrideController, AnimationBuildLog log)
        {
            string clipName = inputAnimationData.GetClipName();
            string clipAssetPath = inputAnimationData.GetClipAssetPath();
            Sprite[] standStillSprites = inputAnimationData.GetStandStillSprites();
            
            var parsedAnimationData = new StandardAnimationData(clipAssetPath, clipName, standStillSprites);
            WriteAnimationClip(parsedAnimationData, animationConfig, overwriteExisting, overrideController, log);
        }
        
        private static void GenerateIdleClip(IdleAnimationData inputAnimationData, AnimationConfig animationConfig, bool overwriteExisting, AnimatorOverrideController overrideController, AnimationBuildLog log)
        {
            string clipName = inputAnimationData.GetClipName();
            string clipAssetPath = inputAnimationData.GetClipAssetPath();
            Sprite[] idleSprites = inputAnimationData.GetIdleSprites();
            
            var parsedAnimationData = new StandardAnimationData(clipAssetPath, clipName, idleSprites);
            WriteAnimationClip(parsedAnimationData, animationConfig, overwriteExisting, overrideController, log);
        }
        
        private static void WriteAnimationClip(StandardAnimationData animationData, AnimationConfig animationConfig, bool overwriteExisting, AnimatorOverrideController overrideController, AnimationBuildLog log)
        {
            if (animationData.sprites == null || animationData.sprites.Length == 0) { log.SkipNoSprite(animationData.clipName); return; }
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(animationData.clipAssetPath);
            if (existing != null && !overwriteExisting) { log.SkipAlreadyExists(animationData.clipName); return; }

            AnimationClip clip = existing != null ? existing : new AnimationClip();
            clip.frameRate = animationConfig.frameRate;
            AnimationUtility.SetAnimationClipSettings(clip, animationConfig.refSettings);

            var keyframes = new ObjectReferenceKeyframe[animationData.sprites.Length];
            for (int i = 0; i < animationData.sprites.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe { time = i / animationConfig.frameRate, value = animationData.sprites[i] };
            }
            AnimationUtility.SetObjectReferenceCurve(clip, animationConfig.spriteBinding, keyframes);

            if (existing == null) { AssetDatabase.CreateAsset(clip, animationData.clipAssetPath); }
            else { EditorUtility.SetDirty(clip); }

            log.AppendLine($"{(existing == null ? "Created" : "Updated")} {animationData.clipName} ({animationData.sprites.Length} frames)");
            log.createdCount++;

            if (overrideController != null) { ApplyOverride(overrideController, animationData.clipName, clip); }
        }
        
        private static void ApplyOverride(AnimatorOverrideController controller, string clipName, AnimationClip newClip)
        {
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            controller.GetOverrides(overrides);

            bool matched = false;
            for (int i = 0; i < overrides.Count; i++)
            {
                if (overrides[i].Key == null || !string.Equals(overrides[i].Key.name, clipName, StringComparison.OrdinalIgnoreCase)) { continue; }
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, newClip);
                matched = true;
                break;
            }

            if (!matched) { return; }
            controller.ApplyOverrides(overrides);
            EditorUtility.SetDirty(controller);
        }
        #endregion
        
        #region StaticUIBuilders
        private static void InitializeRootElement(VisualElement root)
        {
            root.style.paddingLeft = 8;
            root.style.paddingRight = 8;
            root.style.paddingTop = 8;
            root.style.paddingBottom = 8;
        }
        
        private static Label CreateSectionLabel(string text) => new(text) { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 4, marginBottom = 2 } };
        
        private static VisualElement CreateSpacer() => new() { style = { height = 8 } };
        
        private static FloatField CreateStandardFloatField(string label, float defaultValue)
        {
            return new FloatField(label)
            {
                value = defaultValue,
                style = { marginBottom = 2 }
            };
        }

        private static TextField CreateStandardTextField(string label, string defaultText, string tooltip = "")
        {
            return new TextField(label)
            {
                value = defaultText,
                style = { marginBottom = 2 },
                tooltip = tooltip
            };
        }
        
        private static ObjectField CreateStandardObjectField(string label, Type objectType)
        {
            var field = new ObjectField(label)
            {
                objectType = objectType,
                allowSceneObjects = false,
                style = { marginBottom = 2 }
            };
            return field;
        }

        private static Button CreateStandardButton(string label)
        {
            return new Button
            {
                text = label,
                style = { height = 30 }
            };
        }

        private static ScrollView CreateStandardScrollView()
        {
            return new ScrollView(ScrollViewMode.Vertical)
            {
                style = 
                { 
                    flexGrow = 1, 
                    minHeight = 140, 
                    borderTopWidth = 1, 
                    borderBottomWidth = 1,
                    borderLeftWidth = 1, 
                    borderRightWidth = 1 
                }
            };
        }
        #endregion
    }
}
