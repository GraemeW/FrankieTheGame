using System.Globalization;
using UnityEngine;

namespace Frankie.Saving
{
    public class PrefsEntryData
    {
        public readonly string key;
        public readonly PrefsValueType type;
        public string value { get; private set; }
        
        public PrefsEntryData(string key, PrefsValueType type, string value)
        {
            this.key = key;
            this.type = type;
            this.value = value;
        }
        public void SetValue(string newValue) => value = newValue;
        
        // Getters
        public bool TryGetValue(out int intValue)
        {
            intValue = int.TryParse(value, out int i) ? i : 0;
            return type == PrefsValueType.Int;
        }

        public bool TryGetValue(out float floatValue)
        {
            floatValue = float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ? f : 0f;
            return type == PrefsValueType.Float;
        }
        
        public bool TryGetValue(out string stringValue)
        {
            stringValue = value ?? string.Empty;
            return type == PrefsValueType.String;
        }
    }
}
