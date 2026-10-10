using System.Collections.Generic;
using Unity.Properties;
using LowDefMustard.UIBox;

namespace Frankie.Menu.UI
{
    public sealed class NamingConfirmModel : BindableModel
    {
        // State
        private IReadOnlyList<ConfirmCharacterCard> internalCharacterCards;
        private IReadOnlyList<ConfirmAnswerCard> internalAnswerCards;
        
        [CreateProperty] public IReadOnlyList<ConfirmCharacterCard> characterCards
        {
            get => internalCharacterCards;
            set => SetProperty(ref internalCharacterCards, value);
        }

        [CreateProperty] public IReadOnlyList<ConfirmAnswerCard> answerCards
        {
            get => internalAnswerCards;
            set => SetProperty(ref internalAnswerCards, value);
        }
    }
}
