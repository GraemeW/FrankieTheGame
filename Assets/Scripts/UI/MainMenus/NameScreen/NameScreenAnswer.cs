using Frankie.Stats;

namespace Frankie.Menu.UI
{
    public class NameScreenAnswer
    {
        public readonly NameScreenQuestionType questionType;
        public readonly CharacterProperties characterProperties;
        public readonly string text;

        public NameScreenAnswer(NameScreenQuestionType questionType, CharacterProperties characterProperties, string text)
        {
            this.questionType = questionType;
            this.characterProperties = characterProperties;
            this.text = text;
        }
    }
}
