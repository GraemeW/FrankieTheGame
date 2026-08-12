using System;
using System.Collections.Generic;
using System.Linq;

namespace LowDefMustard.Saving.Editor
{
    public class NullSaveFileManagerAdapter : ISaveFileManagerAdapter
    {
        public event Action gameListUpdated { add { } remove { } }
        public string GetCurrentSaveName() => null;
        public string GetSaveNameForIndex(int index) => string.Empty;
        public bool HasSave(string saveName) => false;
        public bool GetInfoFromSave(string saveName, out string characterName, out int level)
        {
            characterName = null;
            level = 0;
            return false;
        }
        public IEnumerable<string> ListSaves(bool includeSession = true) => Enumerable.Empty<string>();
        public void SetCurrentSave(string saveName, bool announceGameListUpdate = true) { }
        public void CopySave(string newSave, bool announceGameListUpdate = true) { }
        public void CopySave(string existingSaveName, string newSaveName, bool announceGameListUpdate = true) { }
        public void Delete(bool announceGameListUpdate = true) { }
        public void Delete(string saveName, bool announceGameListUpdate = true) { }
    }
}
