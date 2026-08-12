namespace LowDefMustard.Saving
{
    public static class SaveFileManagerProvider
    {
        public static ISaveFileManager current { get; set; } = new NullSaveFileManager();
    }
}