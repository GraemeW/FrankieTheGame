using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering
{
    [CreateAssetMenu(fileName = "New Battle Effect Recipe", menuName = "BattleAction/Effects/Battle Effect Recipe", order = 16)]
    public sealed class BattleEffectRecipe : ScriptableObject
    {
        // Notes:
        //  - Drawn by the shared BattleEffect shader graph
        //      - items are baked into a data texture (one row per item ~ see BattleEffectDataLayout)
        //  - The texture is cached + updated in place on edit, so live effects / previews pick up changes
        //  - Internal methods/properties are for test visibility

        [SerializeField][Tooltip("Steps the animation at this many frames per second (0 = smooth)")][Min(0f)] private float frameRate = 0f;
        [SerializeField][Tooltip("Grows / shrinks the whole effect about its centre (positions, sizes, outlines, jitter + glow)")][Min(0.01f)] private float scaleRatio = 1f;
        [SerializeField][Tooltip("Anti-aliasing (same crispness at any shape size)")][Min(0f)] private float edgeSoftnessPixels = 1f;
        [SerializeField][Tooltip("Extra soft edge (see BattleEffectItem)")][Min(0f)] private float glowWidthEU = 0f;
        [SerializeField][Tooltip("Drawn in order (later items on top)")] private List<BattleEffectItem> items = new();

        // State
        [NonSerialized] private Texture2D dataTexture;
        [NonSerialized] private int itemCount;
        [NonSerialized] private bool isDirty = true;
        [NonSerialized] private int revision;
        [NonSerialized] private BattleEffectFloatRange[] parameterBuffer;

        #region PublicMethods
        public float GetFrameRate() => frameRate;
        public int GetRevision() => revision;
        public IReadOnlyList<BattleEffectItem> GetItems() => items;

        public float GetEndTime()
        {
            float endTime = 0f;
            foreach (BattleEffectItem item in items)
            {
                if (item != null) { endTime = Mathf.Max(endTime, item.GetEndTime()); }
            }
            return endTime;
        }

        public void ApplyTo(ref MaterialDefinition materialDefinition, BattleEffectFit fit = BattleEffectFit.AspectFit)
        {
            RefreshDataTexture();
            BattleEffectPropertyRefs.SetRecipe(ref materialDefinition, dataTexture, itemCount, frameRate, edgeSoftnessPixels, glowWidthEU * scaleRatio);
            BattleEffectPropertyRefs.SetFit(ref materialDefinition, fit);
        }
        #endregion

        #region InternalMethods
        internal Texture2D BakeDataTexture(out int bakedItemCount)
        {
            RefreshDataTexture();
            bakedItemCount = itemCount;
            return dataTexture;
        }

        internal void SetItems(IEnumerable<BattleEffectItem> setItems)
        {
            items = new List<BattleEffectItem>(setItems);
            isDirty = true;
            revision++;
        }
        #endregion

        #region UnityMethods
        private void OnValidate()
        {
            isDirty = true;
            revision++;
#if UNITY_EDITOR
            ValidateShapes();
#endif
        }

        private void OnDisable()
        {
            if (dataTexture == null) { return; }
            if (Application.isPlaying) { Destroy(dataTexture); }
            else { DestroyImmediate(dataTexture); }
            dataTexture = null;
            isDirty = true;
        }
        #endregion

        #region PrivateMethods
        private void RefreshDataTexture()
        {
            if (dataTexture != null && !isDirty) { return; }

            int rowCount = Mathf.Max(items.Count, 1);
            if (dataTexture == null)
            {
                dataTexture = new Texture2D(BattleEffectDataLayout.texelsPerItem, rowCount, TextureFormat.RGBAFloat, false, true)
                {
                    name = $"{name} (Battle Effect Data)",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
            else if (dataTexture.height != rowCount) { dataTexture.Reinitialize(BattleEffectDataLayout.texelsPerItem, rowCount); }

            // Note:  items w/o a shape (or w/o a shape ID) are skipped, so rows are packed
            parameterBuffer ??= new BattleEffectFloatRange[BattleEffectDataLayout.shapeParameterCapacity];
            var texels = new Color[BattleEffectDataLayout.texelsPerItem * rowCount];
            itemCount = 0;
            foreach (BattleEffectItem item in items)
            {
                if (item != null && item.TryWriteData(texels, itemCount * BattleEffectDataLayout.texelsPerItem, parameterBuffer, scaleRatio)) { itemCount++; }
            }

            dataTexture.SetPixels(texels);
            dataTexture.Apply(false);
            isDirty = false;
        }

#if UNITY_EDITOR
        internal void ValidateShapes()
        {
            // Note:  Critical to clean up here since inspector list edits can
            //  - leave items zero-filled (the first entry added to an empty list skips field initializers)
            //  - w/o a shape (new entries skip [SerializeReference] initializers)
            //  - or sharing one (duplicated entries)
            
            var seenShapes = new HashSet<BattleEffectShape>();
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null || items[i].IsUnset()) { items[i] = new BattleEffectItem(); }
                BattleEffectItem item = items[i];
                BattleEffectShape shape = item.GetShape();
                if (shape == null) { item.SetShape(new BattleEffectCircle()); }
                else if (!seenShapes.Add(shape)) { item.SetShape(JsonUtility.FromJson(JsonUtility.ToJson(shape), shape.GetType()) as BattleEffectShape); }
            }
        }
#endif
        #endregion
    }
}
