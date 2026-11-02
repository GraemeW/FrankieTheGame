using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace LowDefMustard.Rendering
{
    public sealed class GraphicsWarmup : MonoBehaviour
    {
        // Warms up the shaders + pipeline states (PSOs) of the listed materials on start
        //  - via 2 options:
        //      - A. PreDraw:  Draws each entry once invisibly through its real draw path (blocks the frames it runs in)
        //      - B. StateCollection:  Builds a GraphicsStateCollection from the entries at runtime + warms it up in the background
        //  - Option [traceInDevelopmentBuilds]:
        //      - traces every PSO drawn this session (+ in StateCollection mode, the PSOs the warm-up missed)
        //      - saved on quit to persistentDataPath + sent to the editor (Autoconnect Profiler enabled)

        // Tunables
        [SerializeField] private GraphicsWarmupMode mode = GraphicsWarmupMode.StateCollection;
        [SerializeField] private List<GraphicsWarmupEntry> entries = new();
        [SerializeField][Min(1)] private int preDrawFrames = 2;
        [SerializeField][Tooltip("0 = all at once (background job)")][Min(0)] private int progressiveCountPerFrame = 0;
        [SerializeField] private bool traceInDevelopmentBuilds = false;
        [SerializeField] private string traceFileName = "GraphicsWarmupTrace";

        // Const Tunables
        private const int _maxPanelWaitFrames = 30;
        private const string _cacheMissSuffix = "-CacheMisses";
        private const string _fileExtension = ".graphicsstate";

        // State
        public bool isComplete { get; private set; }
        private Coroutine preWarmCoroutine;
        private GraphicsWarmupPreDraw preDraw;
        private JobHandle warmupHandle;
        private GraphicsStateCollection warmupCollection;
        private GraphicsStateCollection sessionTrace;

        // Events
        public event Action completed;

        #region UnityMethods
        private void Awake()
        {
            if (!IsTracing()) { return; }
            sessionTrace = new GraphicsStateCollection();
            sessionTrace.BeginTrace();
        }

        private void Start()
        {
            if (preWarmCoroutine != null) { StopCoroutine(preWarmCoroutine); }
            preWarmCoroutine = StartCoroutine(mode == GraphicsWarmupMode.PreDraw ? PreDraw() : WarmUpCollection());
        }

        private void OnApplicationQuit()
        {
            if (!IsTracing()) { return; }
            if (sessionTrace != null)
            {
                sessionTrace.EndTrace();
                SaveTrace(sessionTrace, traceFileName);
            }
            warmupHandle.Complete();
            if (warmupCollection != null && warmupCollection.cacheMissCollection != null) { SaveTrace(warmupCollection.cacheMissCollection, traceFileName + _cacheMissSuffix); }
        }

        private void OnDestroy()
        {
            if (preWarmCoroutine != null) { StopCoroutine(preWarmCoroutine); }
            preDraw?.Dispose(); // Stopping the coroutine skips its own cleanup
            preDraw = null;
            
            warmupHandle.Complete();
            if (warmupCollection != null) { Destroy(warmupCollection); }
            if (sessionTrace != null) { Destroy(sessionTrace); }
        }
        #endregion

        #region PublicMethods
        public IReadOnlyList<GraphicsWarmupEntry> GetEntries() => entries;
        #endregion

        #region PrivateMethods
        private bool IsTracing() => traceInDevelopmentBuilds && Debug.isDebugBuild && !Application.isEditor;

        private IEnumerator PreDraw()
        {
            float startTime = Time.realtimeSinceStartup;
            preDraw = new GraphicsWarmupPreDraw(transform, entries);
            for (int frame = 0; frame < _maxPanelWaitFrames && !preDraw.IsReady(); frame++) { yield return null; }
            for (int frame = 0; frame < preDrawFrames; frame++) { yield return new WaitForEndOfFrame(); }
            preDraw.Dispose();
            preDraw = null;
            Complete($"pre-drew {entries.Count} entries", startTime);
        }

        private IEnumerator WarmUpCollection()
        {
            float startTime = Time.realtimeSinceStartup;
            warmupCollection = GraphicsWarmupStates.Build(entries);
            bool traceCacheMisses = IsTracing();
            if (progressiveCountPerFrame <= 0)
            {
                warmupHandle = warmupCollection.WarmUp(default, traceCacheMisses);
                while (!warmupHandle.IsCompleted) { yield return null; } // Polling:  a job handle has no completion callback
            }
            else
            {
                for (int call = 0; call <= warmupCollection.totalGraphicsStateCount && !warmupCollection.isWarmedUp; call++)
                {
                    warmupHandle = warmupCollection.WarmUpProgressively(progressiveCountPerFrame, warmupHandle, traceCacheMisses);
                    yield return null;
                }
            }

            warmupHandle.Complete();
            Complete($"warmed {warmupCollection.variantCount} variants / {warmupCollection.totalGraphicsStateCount} states", startTime);
        }

        private void Complete(string summary, float startTime)
        {
            isComplete = true;
            if (Debug.isDebugBuild) { Debug.Log($"[GraphicsWarmup] {mode} ({SystemInfo.graphicsDeviceType}):  {summary} in {(Time.realtimeSinceStartup - startTime) * 1000f:F0} ms"); }
            completed?.Invoke();
        }

        private static void SaveTrace(GraphicsStateCollection collection, string fileName)
        {
            string deviceFileName = $"{fileName}-{SystemInfo.graphicsDeviceType}";
            string path = Path.Combine(Application.persistentDataPath, deviceFileName + _fileExtension);
            bool isSaved = collection.SaveToFile(path);
            bool isSent = collection.SendToEditor(deviceFileName);
            Debug.Log($"[GraphicsWarmup] Trace {deviceFileName}:  {collection.variantCount} variants / {collection.totalGraphicsStateCount} states (saved:  {isSaved} {path}, sent to editor:  {isSent})");
        }
        #endregion
    }
}
