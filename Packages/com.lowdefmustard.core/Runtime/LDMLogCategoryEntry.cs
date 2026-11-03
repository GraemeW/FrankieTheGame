using System;
using UnityEngine;

namespace LowDefMustard.Core
{
    [Serializable]
    internal sealed class LDMLogCategoryEntry
    {
        public LDMLogCategory category;
        public bool enabled = true;
        public Color color = Color.white;

        // Editor Field References
        public const string categoryField = nameof(category);
        public const string enabledField = nameof(enabled);
        public const string colorField = nameof(color);
    }
}
