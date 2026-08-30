using System;
using System.Collections.Generic;
using System.Linq;
using LowDefMustard.Saving.Editor;

namespace LowDefMustard.Saving.Tests.Editor
{
    // Controllable in-memory ISaveFileManagerAdapter
    // - Allows SaveEditor tests script save lists, current-save state, and character info without a real game's SaveFileManager
    public class TestSaveFileManagerAdapter : ISaveFileManagerAdapter
    {
        public event Action gameListUpdated;

        public string currentSaveName;
        public readonly Dictionary<string, (string characterName, int level)> saves = new();

        public string GetCurrentSaveName() => currentSaveName;
        public string GetSaveNameForIndex(int index) => $"Save{index}";
        public bool HasSave(string saveName) => saves.ContainsKey(saveName);

        public bool GetInfoFromSave(string saveName, out string characterName, out int level)
        {
            if (saves.TryGetValue(saveName, out (string characterName, int level) info))
            {
                characterName = info.characterName;
                level = info.level;
                return true;
            }
            characterName = null;
            level = 0;
            return false;
        }

        public IEnumerable<string> ListSaves(bool includeSession = true) => saves.Keys.ToList();

        public void SetCurrentSave(string saveName, bool announceGameListUpdate = true)
        {
            currentSaveName = saveName;
            if (announceGameListUpdate) { gameListUpdated?.Invoke(); }
        }

        public void CopySave(string newSave, bool announceGameListUpdate = true)
        {
            if (currentSaveName != null && saves.TryGetValue(currentSaveName, out (string characterName, int level) info)) { saves[newSave] = info; }
            if (announceGameListUpdate) { gameListUpdated?.Invoke(); }
        }

        public void CopySave(string existingSaveName, string newSaveName, bool announceGameListUpdate = true)
        {
            if (saves.TryGetValue(existingSaveName, out (string characterName, int level) info)) { saves[newSaveName] = info; }
            if (announceGameListUpdate) { gameListUpdated?.Invoke(); }
        }

        public void Delete(bool announceGameListUpdate = true)
        {
            if (currentSaveName != null) { saves.Remove(currentSaveName); }
            if (announceGameListUpdate) { gameListUpdated?.Invoke(); }
        }

        public void Delete(string saveName, bool announceGameListUpdate = true)
        {
            saves.Remove(saveName);
            if (announceGameListUpdate) { gameListUpdated?.Invoke(); }
        }
    }
}
