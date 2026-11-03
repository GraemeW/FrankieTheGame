using System;

namespace LowDefMustard.Core
{
    [Flags]
    public enum LDMLogCategory
    {
        // Flags -> one call can carry several tags ~ a message shows if ANY of its flags is enabled
        // Single-bit values only (aggregates like All are skipped by LDMLogSettings)

        None = 0,
        Default = 1 << 0,
        Core = 1 << 1,
        Control = 1 << 2,
        Combat = 1 << 3,
        Inventory = 1 << 4,
        Quests = 1 << 5,
        Stats = 1 << 6,
        Speech = 1 << 7,
        Sound = 1 << 8,
        World = 1 << 9,
        Zones = 1 << 10,
        UI = 1 << 11,
        Rendering = 1 << 12,
        Saving = 1 << 13,
        Localization = 1 << 14,
        Utilities = 1 << 15,
        All = ~0
    }
}
