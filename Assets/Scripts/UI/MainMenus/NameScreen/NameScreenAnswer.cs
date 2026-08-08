using UnityEngine;

namespace Frankie.Menu.UI
{
    public class NameScreenAnswer
    {
        public readonly NameScreenQuestion question;
        public readonly string answer;
        public readonly Color optionalAnswerColor;

        public NameScreenAnswer(NameScreenQuestion question, string answer)
        {
            this.question = question;
            this.answer = answer;
            optionalAnswerColor = Color.white;
        }

        public NameScreenAnswer(NameScreenQuestion question, string answer, Color optionalAnswerColor)
        {
            this.question = question;
            this.answer = answer;
            this.optionalAnswerColor = optionalAnswerColor;
        }
    }
}
