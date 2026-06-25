using System.Collections.Generic;
using Frankie.Stats;
using UnityEngine.UIElements;

namespace Frankie.Saving.Editor
{
    public abstract class PartyBaseSubCard : SaveableSubCardData
    {
        protected SaveableEntityCardData parentSaveableEntityCardData;
        protected readonly Dictionary<CharacterProperties, SaveableEntityCardData> characterSaveableEntityCards = new();
        
        protected void RebuildCharacterSaveableEntityCards(List<CharacterProperties> checkCharacterProperties)
        {
            foreach (CharacterProperties characterProperties in checkCharacterProperties)
            {
                if (characterProperties == null || characterProperties.GetCharacterPrefab() == null) { continue; }
                if (characterSaveableEntityCards.ContainsKey(characterProperties)) { continue; }
                
                SaveableEntityCardData characterSaveableEntityCard = parentSaveableEntityCardData.BuildFromCharacterPropertiesWithCache(characterProperties);
                if (characterSaveableEntityCard == null) { continue; }
                characterSaveableEntityCards[characterProperties] = characterSaveableEntityCard;
            }
        }

        protected void DrawCharacterEntityView(VisualElement container)
        {
            foreach (SaveableEntityCardData characterSaveableEntityCard in characterSaveableEntityCards.Values)
            { 
                Box entityCardView = characterSaveableEntityCard.DrawSaveableEntityCard(() => characterSaveableEntityCard.SaveSaveableEntity(true));
                container.Add(entityCardView);
                container.Add(new VisualElement { style = { height = entityCardSpacerHeight } });
            }
        }
    }
}
