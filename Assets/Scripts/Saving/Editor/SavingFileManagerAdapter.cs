using System;
using System.Collections.Generic;
using LowDefMustard.Saving.Editor;

namespace Frankie.Saving.Editor
{
    public class SavingFileManagerAdapter : ISaveFileManagerAdapter
    {
        public event Action gameListUpdated
        {
            add => SaveFileManager.gameListUpdated += value;
            remove => SaveFileManager.gameListUpdated -= value;
        }
        public string GetCurrentSaveName() => SaveFileManager.GetCurrentSaveName();
        public string GetSaveNameForIndex(int index) => SaveFileManager.GetSaveNameForIndex(index);
        public bool HasSave(string saveName) => SaveFileManager.HasSave(saveName);
        public bool GetInfoFromSave(string saveName, out string characterName, out int level) => SaveFileManager.GetInfoFromSave(saveName, out characterName, out level);
        public IEnumerable<string> ListSaves(bool includeSession = true) => SaveFileManager.ListSaves(includeSession);
        public void SetCurrentSave(string saveName, bool announce = true) => SaveFileManager.SetCurrentSave(saveName, announce);
        public void CopySave(string newSave, bool announce = true) => SaveFileManager.CopySave(newSave, announce);
        public void CopySave(string existingSaveName, string newSaveName, bool announce = true) => SaveFileManager.CopySave(existingSaveName, newSaveName, announce);
        public void Delete(bool announce = true) => SaveFileManager.Delete(announce);
        public void Delete(string saveName, bool announce = true) => SaveFileManager.Delete(saveName, announce);
    }
}
