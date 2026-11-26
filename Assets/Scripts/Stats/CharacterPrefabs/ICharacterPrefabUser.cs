using System.Collections.Generic;

namespace Frankie.Stats
{
    public interface ICharacterPrefabUser
    {
        IEnumerable<CharacterProperties> GetCharactersToPreload(); // Any scene components that can spawn characters
    }
}
