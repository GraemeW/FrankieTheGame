using System;

namespace Frankie.Utils
{
    public struct ChoiceActionPair
    {
        public ChoiceActionPair(string choice, Action action)
        {
            this.choice = choice;
            this.action = action;
        }

        public readonly string choice;
        public readonly Action action;
    }
}
