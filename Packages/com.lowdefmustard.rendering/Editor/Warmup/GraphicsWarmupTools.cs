using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    public static class GraphicsWarmupTools
    {
        // Const Tunables
        private const string _menuRoot = "Tools/GraphicsTools/";
        
        private const string _materialExtension = ".mat";
        private const string _entriesPropertyName = "entries";
        
        private const string _fileExtension = "graphicsstate";
        private const string _reportSuffix = ".report.txt";
        private const string _compareSuffix = ".compare.txt";

        #region MenuItems
        [MenuItem(_menuRoot + "WarmUp: Reconcile shader collection", false, 16)]
        private static void ReconcileShaderCollection()
        {
            string[] materialFolders = GraphicsWarmupFolders.GetMaterialFolders().Where(AssetDatabase.IsValidFolder).ToArray();
            if (materialFolders.Length == 0) { Debug.LogWarning($"[GraphicsWarmup] No valid material folders registered ~ register them via {nameof(GraphicsWarmupFolders)} from an [InitializeOnLoad] class"); return; }
            if (!TryLoadWarmupPrefab(out string prefabPath, out GraphicsWarmup warmup)) { return; }
            
            var serializedWarmup = new SerializedObject(warmup);
            SerializedProperty entriesProperty = serializedWarmup.FindProperty(_entriesPropertyName);

            var boundMaterials = new List<string>();
            var unboundMaterials = new List<string>();
            HashSet<Material> coveredMaterials = GetCoveredMaterials(warmup.GetEntries());
            foreach (string materialPath in AssetDatabase.FindAssets("t:Material", materialFolders).Select(AssetDatabase.GUIDToAssetPath).Where(path => path.EndsWith(_materialExtension)).OrderBy(path => path)) // .mat only ~ shader graphs hold a material sub-asset too
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null || coveredMaterials.Contains(material)) { continue; }

                string uiToolkitFolder = GraphicsWarmupFolders.GetUIToolkitMaterialFolders().Select(folder => folder + "/").FirstOrDefault(materialPath.StartsWith);
                PanelSettings panelSettings = uiToolkitFolder != null ? GetFolderPanelSettings(warmup.GetEntries(), uiToolkitFolder) : null;
                if (panelSettings == null) { unboundMaterials.Add(materialPath); continue; }

                AddUIToolkitMaterialEntry(entriesProperty, material, panelSettings);
                boundMaterials.Add($"{materialPath} (on {panelSettings.name})");
            }

            if (boundMaterials.Count > 0)
            {
                serializedWarmup.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SavePrefabAsset(warmup.gameObject);
                Debug.Log($"[GraphicsWarmup] Auto-bound {boundMaterials.Count} new material(s) to {prefabPath}:\n    {string.Join("\n    ", boundMaterials)}");
            }
            if (unboundMaterials.Count > 0) { Debug.LogWarning($"[GraphicsWarmup] {unboundMaterials.Count} material(s) found but not bound to {prefabPath} ~ these will not be warmed (add their entries by hand):\n    {string.Join("\n    ", unboundMaterials)}"); }
            if (boundMaterials.Count == 0 && unboundMaterials.Count == 0) { Debug.Log($"[GraphicsWarmup] Every material in {string.Join(", ", materialFolders)} already has an entry in {prefabPath}"); }
        }
        
        [MenuItem(_menuRoot + "WarmUp: Report Trace File", false, 15)]
        private static void ReportTraceFile()
        {
            // Reports every variant + its states (vertex layout, target, render state)
            
            if (!TryLoadTrace(out string path, out GraphicsStateCollection trace)) { return; }
            try
            {
                var report = new StringBuilder();
                AppendHeader(report, "Trace", path, trace);
                foreach (KeyValuePair<string, List<string>> variant in GetStatesByVariant(trace)) { AppendVariant(report, variant.Key, variant.Value); }
                WriteReport(path + _reportSuffix, report);
            }
            finally { Object.DestroyImmediate(trace); }
        }

        [MenuItem(_menuRoot + "WarmUp: Compare Trace With Selected Warmup", false, 15)]
        private static void CompareTraceWithSelectedWarmup()
        {
            // Compares the trace vs the selected GraphicsWarmup's procedural collection
            
            GraphicsWarmup warmup = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInChildren<GraphicsWarmup>(true) : null;
            if (warmup == null) { EditorUtility.DisplayDialog("Graphics Warmup", "Select the GameObject (or prefab) holding the GraphicsWarmup first.", "OK"); return; }
            if (!TryLoadTrace(out string path, out GraphicsStateCollection trace)) { return; }

            GraphicsStateCollection procedural = GraphicsWarmupStates.Build(warmup.GetEntries());
            try
            {
                Dictionary<string, List<string>> tracedStates = GetStatesByVariant(trace);
                Dictionary<string, List<string>> proceduralStates = GetStatesByVariant(procedural);
                bool isSameDevice = trace.graphicsDeviceType == SystemInfo.graphicsDeviceType || trace.graphicsDeviceType == GraphicsDeviceType.Null; // Null:  not stamped in the file

                var report = new StringBuilder();
                AppendHeader(report, "Trace", path, trace);
                AppendHeader(report, "Procedural", warmup.name, procedural);
                if (!isSameDevice) { report.AppendLine($"Note:  trace is {trace.graphicsDeviceType}, editor is {SystemInfo.graphicsDeviceType} ~ variants compare, states are listed side by side only").AppendLine(); }

                int matchedStates = 0;
                int missedStates = 0;
                foreach (KeyValuePair<string, List<string>> traced in tracedStates)
                {
                    if (!proceduralStates.TryGetValue(traced.Key, out List<string> generated))
                    {
                        if (IsProjectVariant(traced.Key, proceduralStates)) { AppendVariant(report, "MISSED VARIANT  " + traced.Key, traced.Value); }
                        continue;
                    }
                    foreach (string state in traced.Value)
                    {
                        bool isMatched = isSameDevice && generated.Contains(state);
                        if (isMatched) { matchedStates++; } else { missedStates++; }
                        report.AppendLine($"{(isMatched ? "MATCHED" : "MISSED ")}  {traced.Key}").AppendLine($"    traced      {state}");
                        if (!isMatched) { foreach (string generatedState in generated) { report.AppendLine($"    procedural  {generatedState}"); } }
                    }
                }
                foreach (KeyValuePair<string, List<string>> generated in proceduralStates.Where(generated => !tracedStates.ContainsKey(generated.Key))) { AppendVariant(report, "UNUSED (not in trace)  " + generated.Key, generated.Value); }

                report.Insert(0, $"Summary:  {matchedStates} traced states matched, {missedStates} missed (for the warm-up's shaders)\n\n");
                WriteReport(path + _compareSuffix, report);
            }
            finally
            {
                Object.DestroyImmediate(trace);
                Object.DestroyImmediate(procedural);
            }
        }
        #endregion

        #region PrivateMethods
        private static bool TryLoadWarmupPrefab(out string prefabPath, out GraphicsWarmup warmup)
        {
            warmup = null;
            prefabPath = null;
            foreach (string path in AssetDatabase.FindAssets($"t:Prefab {nameof(GraphicsWarmup)}").Select(AssetDatabase.GUIDToAssetPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || !prefab.TryGetComponent(out GraphicsWarmup prefabWarmup)) { continue; }
                if (warmup != null) { Debug.LogError($"[GraphicsWarmup] More than one GraphicsWarmup prefab ({prefabPath}, {path}) ~ unsure which to reconcile"); return false; }
                warmup = prefabWarmup;
                prefabPath = path;
            }
            if (warmup == null) { Debug.LogError("[GraphicsWarmup] No prefab w/ a GraphicsWarmup on its root found"); }
            return warmup != null;
        }

        private static HashSet<Material> GetCoveredMaterials(IEnumerable<GraphicsWarmupEntry> entries)
        {
            var coveredMaterials = new HashSet<Material>();
            foreach (GraphicsWarmupEntry entry in entries.Where(entry => entry != null))
            {
                if (entry.GetMaterial() != null) { coveredMaterials.Add(entry.GetMaterial()); }
                if (entry.GetFilter() == null || entry.GetFilter().passes == null) { continue; }
                foreach (PostProcessingPass pass in entry.GetFilter().passes) { if (pass.material != null) { coveredMaterials.Add(pass.material); } }
            }
            return coveredMaterials;
        }

        private static PanelSettings GetFolderPanelSettings(IEnumerable<GraphicsWarmupEntry> entries, string folder)
        {
            // i.e. the standard binding:  the panel settings the folder's existing UI Toolkit material entries are drawn on
            return entries
                .Where(entry => entry != null && entry.GetPath() == GraphicsWarmupPath.UIToolkitMaterial && entry.GetMaterial() != null && entry.GetPanelSettings() != null)
                .Where(entry => AssetDatabase.GetAssetPath(entry.GetMaterial()).StartsWith(folder))
                .Select(entry => entry.GetPanelSettings())
                .FirstOrDefault();
        }

        private static void AddUIToolkitMaterialEntry(SerializedProperty entriesProperty, Material material, PanelSettings panelSettings)
        {
            int index = entriesProperty.arraySize;
            entriesProperty.InsertArrayElementAtIndex(index);
            SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(index);
            entryProperty.FindPropertyRelative("path").enumValueIndex = (int)GraphicsWarmupPath.UIToolkitMaterial;
            entryProperty.FindPropertyRelative("material").objectReferenceValue = material;
            entryProperty.FindPropertyRelative("filter").objectReferenceValue = null;
            entryProperty.FindPropertyRelative("panelSettings").objectReferenceValue = panelSettings;
            entryProperty.FindPropertyRelative("targetTexture").objectReferenceValue = null;
        }

        private static bool TryLoadTrace(out string path, out GraphicsStateCollection trace)
        {
            trace = null;
            path = EditorUtility.OpenFilePanel("Graphics state collection", Application.persistentDataPath, _fileExtension);
            if (string.IsNullOrEmpty(path)) { return false; }

            trace = new GraphicsStateCollection();
            if (trace.LoadFromFile(path)) { return true; }
            Debug.LogError($"[GraphicsWarmup] Couldn't load {path}");
            Object.DestroyImmediate(trace);
            trace = null;
            return false;
        }

        private static bool IsProjectVariant(string variantKey, Dictionary<string, List<string>> proceduralStates)
        {
            // i.e. a variant of a shader the warm-up lists (other shaders in the trace are out of its scope)
            string shaderName = variantKey.Split('|')[0];
            return proceduralStates.Keys.Any(key => key.Split('|')[0] == shaderName);
        }

        private static Dictionary<string, List<string>> GetStatesByVariant(GraphicsStateCollection collection)
        {
            var statesByVariant = new Dictionary<string, List<string>>();
            var variants = new List<GraphicsStateCollection.ShaderVariant>();
            var states = new List<GraphicsStateCollection.GraphicsState>();
            collection.GetVariants(variants);
            foreach (GraphicsStateCollection.ShaderVariant variant in variants)
            {
                collection.GetGraphicsStatesForVariant(variant, states);
                string key = FormatVariant(variant);
                if (!statesByVariant.TryGetValue(key, out List<string> formattedStates)) { formattedStates = new List<string>(); statesByVariant.Add(key, formattedStates); }
                formattedStates.AddRange(states.Select(FormatState));
            }
            return statesByVariant;
        }

        private static string FormatVariant(GraphicsStateCollection.ShaderVariant variant)
        {
            string shaderName = variant.shader != null ? variant.shader.name : "(missing shader)";
            string keywords = variant.keywords == null ? "" : string.Join(" ", variant.keywords.Select(keyword => keyword.name).OrderBy(name => name));
            return $"{shaderName}|pass {variant.passId.SubshaderIndex}.{variant.passId.PassIndex}|[{keywords}]";
        }

        private static string FormatState(GraphicsStateCollection.GraphicsState state)
        {
            string vertexLayout = state.vertexAttributes == null ? "" : string.Join(" ", state.vertexAttributes.Select(attribute => $"{attribute.attribute}:{attribute.format}x{attribute.dimension}@{attribute.stream}"));
            // Note:  load / store actions are left out ~ they vary w/ the frame, but aren't part of an API-level pipeline state
            string attachments = state.attachments == null ? "" : string.Join(" ", state.attachments.Select(attachment => attachment.graphicsFormat));
            RenderStateBlock renderState = state.renderState;
            RenderTargetBlendState blend = renderState.blendState.blendState0;
            DepthState depth = renderState.depthState;
            StencilState stencil = renderState.stencilState;
            return $"vertex [{vertexLayout}] | target [{attachments}] samples {state.sampleCount} depthIndex {state.depthAttachmentIndex} subPass {state.subPassIndex}"
                + $" | blend {blend.sourceColorBlendMode}/{blend.destinationColorBlendMode} {blend.sourceAlphaBlendMode}/{blend.destinationAlphaBlendMode} {blend.colorBlendOperation}/{blend.alphaBlendOperation} mask {blend.writeMask}"
                + $" | depth write {depth.writeEnabled} {depth.compareFunction}"
                + $" | stencil {stencil.enabled} {stencil.readMask}/{stencil.writeMask} front {stencil.compareFunctionFront}/{stencil.passOperationFront}/{stencil.failOperationFront}/{stencil.zFailOperationFront} back {stencil.compareFunctionBack}/{stencil.passOperationBack}/{stencil.failOperationBack}/{stencil.zFailOperationBack} ref {renderState.stencilReference}"
                + $" | cull {renderState.rasterState.cullingMode} force {state.forceCullMode} | {state.topology}";
        }

        private static void AppendHeader(StringBuilder report, string label, string source, GraphicsStateCollection collection)
        {
            report.AppendLine($"{label}:  {source}");
            report.AppendLine($"    {collection.graphicsDeviceType} / {collection.runtimePlatform} / quality {collection.qualityLevelName}:  {collection.variantCount} variants, {collection.totalGraphicsStateCount} states").AppendLine();
        }

        private static void AppendVariant(StringBuilder report, string label, List<string> states)
        {
            report.AppendLine(label);
            foreach (string state in states) { report.AppendLine("    " + state); }
        }

        private static void WriteReport(string reportPath, StringBuilder report)
        {
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log($"[GraphicsWarmup] Wrote {reportPath}");
            EditorUtility.RevealInFinder(reportPath);
        }
        #endregion
    }
}
