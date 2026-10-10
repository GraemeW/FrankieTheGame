namespace Frankie.Menu.UI
{
    public readonly struct ConfirmAnswerCard
    {
        public readonly string question;
        public readonly string answer;

        public ConfirmAnswerCard(string question, string answer)
        {
            this.question = question;
            this.answer = answer;
        }
    }
}
