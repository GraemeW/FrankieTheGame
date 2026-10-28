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
        //  - Instances flatten other recipes' items in after this recipe's own (re-timed, mirrored && recoloured per their BattleEffectInstance properties)
        //      - an edit to an instanced recipe rebakes this one (its revision is part of the cache check)
        //  - Internal methods/properties are for test visibility

        [Header("Global Recipe Parameters")]
        [SerializeField][Tooltip("Steps the animation at this many frames per second (0 = smooth)")][Min(0f)] private float frameRate = 0f;
        [SerializeField][Tooltip("Grows / shrinks the whole effect about its centre (positions, sizes, outlines, jitter + glow)")][Min(0.01f)] private float scaleRatio = 1f;
        [SerializeField][Tooltip("Anti-aliasing (same crispness at any shape size)")][Min(0f)] private float edgeSoftnessPixels = 1f;
        [SerializeField][Tooltip("Extra soft edge (see BattleEffectItem)")][Min(0f)] private float glowWidthEU = 0f;
        [Header("Items and Instances")]
        [SerializeField][Tooltip("Drawn in order (later items on top)")] private List<BattleEffectItem> items = new();
        [SerializeField][Tooltip("Other recipes drawn after the items (in order), re-timed, mirrored + recoloured")] private List<BattleEffectInstance> instances = new();

        // Editor Field References
        public const string itemsField = nameof(items);
        public const string instancesField = nameof(instances);

        // Const Tunables
        public const int maxInstanceDepth = 4; // also stops a recipe perpetually instancing itself

        // State
        [NonSerialized] private Texture2D dataTexture;
        [NonSerialized] private int itemCount;
        [NonSerialized] private bool isDirty = true;
        [NonSerialized] private int revision;
        [NonSerialized] private int bakedInstanceRevision = -1;
        [NonSerialized] private BattleEffectFloatRange[] parameterBuffer;

        #region PublicMethods
        public float GetFrameRate() => frameRate;
        public int GetRevision() => revision;
        public IReadOnlyList<BattleEffectItem> GetItems() => items;

        public IReadOnlyList<BattleEffectInstance> GetInstances() => instances;
        public float GetTotalStartTime() => GetStartTime();
        public float GetTotalEndTime() => GetEndTime();

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

        internal int AddItem(BattleEffectItem item)
        {
            items.Add(item);
            isDirty = true;
            revision++;
            return items.Count - 1;
        }

        internal int AddInstance(BattleEffectInstance instance)
        {
            instances.Add(instance);
            isDirty = true;
            revision++;
            return instances.Count - 1;
        }

        internal void SetInstances(IEnumerable<BattleEffectInstance> setInstances)
        {
            instances = new List<BattleEffectInstance>(setInstances);
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
            ValidateInstances();
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
            int instanceRevision = GetInstanceRevision();
            if (dataTexture != null && !isDirty && instanceRevision == bakedInstanceRevision) { return; }

            int rowCount = Mathf.Max(CountRows(), 1);
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
            var texels = new Color[BattleEffectDataLayout.texelsPerItem * rowCount];
            itemCount = WriteRows(texels, 0, scaleRatio);

            dataTexture.SetPixels(texels);
            dataTexture.Apply(false);
            isDirty = false;
            bakedInstanceRevision = instanceRevision;
        }

        private int WriteRows(Color[] texels, int firstRow, float effectScale, int depth = 0)
        {
            // This recipe's items, then each instance's - stacked into the 1D texels array (i.e. texelsPerItem entries per item 0; texelsPerItem entries per item1; etc.)
            //  - each instance's items written by its recipe, then re-timed / mirrored / recoloured
            // Starting at depth = 0, traverses through instances (up to maxInstanceDepth) to write total set inclusive
            
            parameterBuffer ??= new BattleEffectFloatRange[BattleEffectDataLayout.shapeParameterCapacity];
            int row = firstRow;
            foreach (BattleEffectItem item in items)
            {
                if (item != null && item.TryWriteData(texels, row * BattleEffectDataLayout.texelsPerItem, parameterBuffer, effectScale)) { row++; }
            }
            if (depth >= maxInstanceDepth) { return row - firstRow; }

            foreach (BattleEffectInstance instance in instances)
            {
                BattleEffectRecipe instancedRecipe = instance?.GetRecipe();
                if (instancedRecipe == null) { continue; }
                int instanceFirstRow = row;
                row += instancedRecipe.WriteRows(texels, row, effectScale * instancedRecipe.scaleRatio, depth + 1); // Write the instance recipe's data as-is
                for (int instanceRow = instanceFirstRow; instanceRow < row; instanceRow++) { instance.ApplyTo(texels, instanceRow * BattleEffectDataLayout.texelsPerItem); } // Then re-time/mirror/re-colour
            }
            return row - firstRow;
        }

        private int CountRows(int depth = 0)
        {
            // Upper bound (items w/o a shape are counted but not written)
            // Starting at depth = 0, traverses through instances (up to maxInstanceDepth) to find total row count inclusive
            int rows = 0;
            foreach (BattleEffectItem item in items) { if (item != null) { rows++; } }
            if (depth >= maxInstanceDepth) { return rows; }
            foreach (BattleEffectInstance instance in instances)
            {
                if (instance?.GetRecipe() != null) { rows += instance.GetRecipe().CountRows(depth + 1); }
            }
            return rows;
        }

        internal int GetInstanceRevision(int depth = 0)
        {
            // Sum of instanced recipes' revisions (any edit below changes it)
            // Starting at depth = 0, traverses through instances (up to maxInstanceDepth) to find total revision inclusive
            int instanceRevision = 0;
            if (depth >= maxInstanceDepth) { return instanceRevision; }
            foreach (BattleEffectInstance instance in instances)
            {
                BattleEffectRecipe instanced = instance?.GetRecipe();
                if (instanced != null) { instanceRevision += instanced.revision + 1 + instanced.GetInstanceRevision(depth + 1); }
            }
            return instanceRevision;
        }

        private float GetStartTime(int depth = 0)
        {
            // Starting at depth = 0, traverses through instances (up to maxInstanceDepth) to find total start time inclusive
            float startTime = float.MaxValue;
            foreach (BattleEffectItem item in items)
            {
                if (item != null) { startTime = Mathf.Min(startTime, item.GetStartTime()); }
            }
            if (depth < maxInstanceDepth)
            {
                foreach (BattleEffectInstance instance in instances)
                {
                    BattleEffectRecipe instanced = instance?.GetRecipe();
                    if (instanced != null) { startTime = Mathf.Min(startTime, instance.GetStartOffset() + instanced.GetStartTime(depth + 1)); }
                }
            }
            return Mathf.Approximately(startTime, float.MaxValue) ? 0f : startTime;
        }

        private float GetEndTime(int depth = 0)
        {
            // Starting at depth = 0, traverses through instances (up to maxInstanceDepth) to find total end time inclusive
            float endTime = 0f;
            foreach (BattleEffectItem item in items)
            {
                if (item != null) { endTime = Mathf.Max(endTime, item.GetEndTime()); }
            }
            if (depth >= maxInstanceDepth) { return endTime; }
            foreach (BattleEffectInstance instance in instances)
            {
                BattleEffectRecipe instanced = instance?.GetRecipe();
                if (instanced != null) { endTime = Mathf.Max(endTime, instance.GetStartOffset() + instanced.GetEndTime(depth + 1)); }
            }
            return endTime;
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

        internal void ValidateInstances()
        {
            // The first entry added to an empty list skips field initializers ~ a zero scale ratio would collapse the instance
            foreach (BattleEffectInstance instance in instances)
            {
                if (instance != null && instance.IsScaleUnset()) { instance.ResetScale(); }
            }
        }
#endif
        #endregion
    }
}
