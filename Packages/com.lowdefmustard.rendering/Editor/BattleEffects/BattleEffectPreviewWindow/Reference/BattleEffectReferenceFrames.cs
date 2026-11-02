#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LowDefMustard.Rendering.Editor
{
    public sealed class BattleEffectReferenceFrames : IDisposable
    {
        // State
        private readonly List<Texture2D> frames = new();

        // Public Methods/Attributes
        public int count => frames.Count;

        public BattleEffectReferenceFrames(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) { return; }

            foreach (string file in Directory.GetFiles(folder, "*.png").OrderBy(file => file, StringComparer.Ordinal))
            {
                var frame = new Texture2D(2, 2) { filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
                if (frame.LoadImage(File.ReadAllBytes(file))) { frames.Add(frame); }
                else { Object.DestroyImmediate(frame); }
            }
        }

        public bool TryGetSize(out Vector2 size)
        {
            size = frames.Count > 0 ? new Vector2(frames[0].width, frames[0].height) : Vector2.zero;
            return frames.Count > 0;
        }

        public bool TryGetFrame(float time, float frameRate, out Texture2D frame, out int frameIndex)
        {
            frame = null;
            frameIndex = -1;
            if (frames.Count == 0 || frameRate <= 0f) { return false; }

            frameIndex = Mathf.Clamp(Mathf.FloorToInt(time * frameRate), 0, frames.Count - 1);
            frame = frames[frameIndex];
            return true;
        }

        public void Dispose()
        {
            foreach (Texture2D frame in frames) { Object.DestroyImmediate(frame); }
            frames.Clear();
        }
    }
}
#endif
