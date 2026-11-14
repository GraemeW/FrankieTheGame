using UnityEngine;
using Frankie.Core;

namespace Frankie.Menu.UI
{
    public class NameScreenAnswer
    {
        public readonly PlayerQuestion question;
        public readonly string answer;
        public readonly Color optionalAnswerColor;

        public NameScreenAnswer(PlayerQuestion question, string answer)
        {
            this.question = question;
            this.answer = answer;
            optionalAnswerColor = Color.white;
        }

        public NameScreenAnswer(PlayerQuestion question, string answer, Color optionalAnswerColor)
        {
            this.question = question;
            this.answer = answer;
            this.optionalAnswerColor = optionalAnswerColor;
        }
    }
}
