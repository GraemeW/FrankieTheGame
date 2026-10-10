using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.UIElements;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    [UxmlElement]
    public sealed partial class ConfirmAnswerCardList : VisualElement
    {
        // Const Tunables
        private const string _cardUssClassName = "confirm-card--answer";
        private const string _questionUssClassName = "confirm-card__question";
        private const string _answerUssClassName = "confirm-card__answer";

        // State
        private IReadOnlyList<ConfirmAnswerCard> internalCards;

        [CreateProperty] public IReadOnlyList<ConfirmAnswerCard> cards
        {
            get => internalCards;
            set
            {
                internalCards = value;
                Rebuild();
            }
        }

        public ConfirmAnswerCardList()
        {
            pickingMode = PickingMode.Ignore;
            SetBinding(nameof(cards), UIToolkitBindings.ToTarget(nameof(NamingConfirmModel.answerCards)));
        }

        private void Rebuild()
        {
            Clear();
            if (internalCards == null) { return; }
            foreach (ConfirmAnswerCard answerCard in internalCards)
            {
                VisualElement card = ConfirmCardParts.CreateCard(_cardUssClassName);
                ConfirmCardParts.AddLabel(card, answerCard.question, _questionUssClassName);
                ConfirmCardParts.AddLabel(card, answerCard.answer, _answerUssClassName);
                Add(card);
            }
        }
    }
}
