using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using LowDefMustard.Utils.Editor;

namespace Frankie.Speech.Editor
{
    internal static class DialogueGraphPalette
    {
        // Node header colours
        [NoAutoStaticsCleanup] public static readonly Color player = StandardBrownTheme.text;
        [NoAutoStaticsCleanup] public static readonly Color narrator = StandardBrownTheme.dimText;
        [NoAutoStaticsCleanup] public static readonly Color condition = StandardBrownTheme.darkCardBorder;
        [NoAutoStaticsCleanup] public static readonly Color entry = StandardBrownTheme.inputBorderHover;

        // AI speakers cycle per character, in order of first appearance
        [NoAutoStaticsCleanup] public static readonly Color[] aiSpeakers =
        {
            StandardBrownTheme.darkAccent,
            StandardBrownTheme.accent,
            new Color32(0xB8, 0x86, 0x4B, 0xFF),
            new Color32(0xA0, 0x70, 0x3A, 0xFF)
        };
    }
}
