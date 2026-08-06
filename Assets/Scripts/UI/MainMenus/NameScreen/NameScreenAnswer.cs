namespace Frankie.Menu.UI
{
    public class NameScreenAnswer
    {
        public NameScreenQuestion question;
        public readonly string answer;

        public NameScreenAnswer(NameScreenQuestion question, string answer)
        {
            this.question = question;
            this.answer = answer;
        }
    }
}
