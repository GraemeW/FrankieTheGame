using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace Frankie.Tests.Editor
{
    public class AddressablesEntryCoverageTests
    {
        // Tested:
        //  - Every authored asset (Game + z_Artwork) is addressable, explicitly or through a folder entry
        //  - Per-file folders (music):  every file has its own entry (one bundle per track)
        //  - Split folders (per character / family / tileset / object family):  every subfolder + loose file has its own entry
        // Note:  Validates project data ~ a failure lists what needs an entry, not a code bug
        // Note:  Missed files are still addressable through their parent's catch-all folder entry (no duplicate), just bundled together (these flag a coarser bundle, not a broken build)

        // Const (note that paths/names are necessarily string ref and brittle, must be updated w/ project layout/re-configuration)
        private static readonly string[] _authoredRoots = { "Assets/Game", "Assets/z_Artwork" };
        private static readonly string[] _unaddressableExtensions = { ".cs", ".md", ".asmref", ".asmdef" };
        private const string _perFileRoot = "Assets/z_Artwork/Music";
        private static readonly string[] _splitFolders =
        {
            "Assets/z_Artwork/PixelArt/PCs",
            "Assets/z_Artwork/PixelArt/NPCs",
            "Assets/z_Artwork/PixelArt/CombatCharacters",
            "Assets/z_Artwork/PixelArt/WorldArtwork/Buildings",
            "Assets/z_Artwork/PixelArt/WorldArtwork/Tilemaps",
            "Assets/z_Artwork/PixelArt/WorldArtwork/WorldObjects",
            "Assets/Game/CharacterObjects/PCs",
            "Assets/Game/CharacterObjects/NPCs",
            "Assets/Game/Tilemaps",
            "Assets/Game/WorldObjects"
        };
        private static readonly string[] _splitFamilyFolders = { "Assets/z_Artwork/PixelArt/NPCs", "Assets/Game/CharacterObjects/NPCs" }; // Each child folder is a family, itself split per character

        #region Helpers
        private static AddressableAssetSettings GetSettings()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            Assert.IsNotNull(settings, "No Addressables settings found");
            return settings;
        }

        private static bool HasOwnEntry(AddressableAssetSettings settings, string path) => settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path)) != null;

        private static IEnumerable<string> GetLooseFiles(string folder) => Directory.GetFiles(folder).Where(file => !file.EndsWith(".meta") && !Path.GetFileName(file).StartsWith(".")).Select(file => file.Replace('\\', '/'));

        private static IEnumerable<string> GetSubfolders(string folder) => Directory.GetDirectories(folder).Select(directory => directory.Replace('\\', '/'));

        private static void AssertNoMisses(List<string> misses, string description)
        {
            Assert.IsEmpty(misses, $"{description}:\n{string.Join("\n", misses)}");
        }
        #endregion

        #region Tests
        [Test]
        public void AuthoredAssets_AreAllAddressable()
        {
            AddressableAssetSettings settings = GetSettings();
            List<string> misses = AssetDatabase.FindAssets(string.Empty, _authoredRoots)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .Where(path => !AssetDatabase.IsValidFolder(path) && !_unaddressableExtensions.Contains(Path.GetExtension(path)))
                .Where(path => settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path), true) == null)
                .ToList();
            AssertNoMisses(misses, "Assets outside every Addressables entry (built into whichever scene bundles reference them, so duplicated)");
        }

        [Test]
        public void PerFileFolders_EveryFileHasOwnEntry()
        {
            AddressableAssetSettings settings = GetSettings();
            List<string> misses = GetSubfolders(_perFileRoot).Prepend(_perFileRoot)
                .SelectMany(GetLooseFiles)
                .Where(path => !HasOwnEntry(settings, path))
                .ToList();
            AssertNoMisses(misses, $"Files under '{_perFileRoot}' without their own entry (bundled in their folder's catch-all)");
        }

        [Test]
        public void SplitFolders_EverySubfolderAndLooseFileHasOwnEntry()
        {
            AddressableAssetSettings settings = GetSettings();
            IEnumerable<string> splitFolders = _splitFolders.Concat(_splitFamilyFolders.SelectMany(GetSubfolders));
            List<string> misses = splitFolders
                .SelectMany(folder => GetSubfolders(folder).Concat(GetLooseFiles(folder)))
                .Where(path => !HasOwnEntry(settings, path))
                .Distinct()
                .ToList();
            AssertNoMisses(misses, "Subfolders / files in split folders without their own entry (bundled in their parent's catch-all)");
        }
        #endregion
    }
}
