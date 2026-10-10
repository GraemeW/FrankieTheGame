using System.Collections.Generic;
using LowDefMustard.Localization;

namespace Frankie.Menu.UI
{
    public sealed class DontCareCycle
    {
        // State
        private readonly List<string> answers = new();
        private int nextIndex = 0;

        public void SetAnswers(IEnumerable<DontCareAnswer> dontCareAnswers)
        {
            nextIndex = 0;
            answers.Clear();
            if (dontCareAnswers == null) { return; }

            foreach (DontCareAnswer dontCareAnswer in dontCareAnswers)
            {
                if (dontCareAnswer?.entry == null) { continue; }
                string answer = dontCareAnswer.entry.GetSafeLocalizedString();
                if (!string.IsNullOrEmpty(answer)) { answers.Add(answer); }
            }
        }

        public bool TryGetNext(out string answer)
        {
            answer = null;
            if (answers.Count == 0) { return false; }
            if (nextIndex >= answers.Count) { nextIndex = 0; }

            answer = answers[nextIndex];
            nextIndex++;
            return true;
        }
    }
}
