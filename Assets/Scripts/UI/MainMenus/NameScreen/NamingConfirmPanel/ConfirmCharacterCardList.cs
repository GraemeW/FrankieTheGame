using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;
using Frankie.Utils.UI;

namespace Frankie.Menu.UI
{
    [UxmlElement]
    public sealed partial class ConfirmCharacterCardList : VisualElement
    {
        // Const Tunables
        private const string _cardUssClassName = "confirm-card--character";
        private const string _spriteUssClassName = "confirm-card__sprite";
        private const string _nameUssClassName = "confirm-card__name";

        // State
        private IReadOnlyList<ConfirmCharacterCard> internalCards;

        [CreateProperty] public IReadOnlyList<ConfirmCharacterCard> cards
        {
            get => internalCards;
            set
            {
                internalCards = value;
                Rebuild();
            }
        }

        public ConfirmCharacterCardList()
        {
            pickingMode = PickingMode.Ignore;
            SetBinding(nameof(cards), UIToolkitBindings.ToTarget(nameof(NamingConfirmModel.characterCards)));
        }

        private void Rebuild()
        {
            Clear();
            if (internalCards == null) { return; }
            foreach (ConfirmCharacterCard characterCard in internalCards)
            {
                VisualElement card = ConfirmCardParts.CreateCard(_cardUssClassName);
                // Note:  A character with nothing to show keeps the sprite's space, so names stay aligned
                VisualElement spriteElement = characterCard.spriteModel != null ? new SpriteElement { dataSource = characterCard.spriteModel } : new VisualElement { pickingMode = PickingMode.Ignore };
                spriteElement.AddToClassList(_spriteUssClassName);
                card.Add(spriteElement);
                ConfirmCardParts.AddLabel(card, characterCard.characterName, _nameUssClassName);
                Add(card);
            }
        }
    }
}
