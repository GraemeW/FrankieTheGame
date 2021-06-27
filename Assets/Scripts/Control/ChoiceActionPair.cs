using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public struct ChoiceActionPair
    {
        public ChoiceActionPair(string choice, Action action)
        {
            choiceActionPairType = ChoiceActionPairType.Simple;
            this.choice = choice;
            this.simpleAction = action;
            this.simpleStringAction = null;
            this.simpleIntAction = null;
            this.stringActionParameter = null;
            this.intActionParameter = 0;
        }

        public ChoiceActionPair(string choice, Action<string> action, string parameter)
        {
            choiceActionPairType = ChoiceActionPairType.SimpleString;
            this.choice = choice;
            this.simpleAction = null;
            this.simpleStringAction = action;
            this.simpleIntAction = null;
            this.stringActionParameter = parameter;
            this.intActionParameter = 0;
        }

        public ChoiceActionPair(string choice, Action<int> action, int parameter)
        {
            choiceActionPairType = ChoiceActionPairType.SimpleInt;
            this.choice = choice;
            this.simpleAction = null;
            this.simpleStringAction = null;
            this.simpleIntAction = action;
            this.stringActionParameter = null;
            this.intActionParameter = parameter;

        }

        public void ExecuteAction()
        {
            if (choiceActionPairType == ChoiceActionPairType.SimpleString)
            {
                simpleStringAction.Invoke(stringActionParameter);
            }
            else if (choiceActionPairType == ChoiceActionPairType.SimpleInt)
            {
                simpleIntAction.Invoke(intActionParameter);
            }
            else if (choiceActionPairType == ChoiceActionPairType.Simple)
            {
                simpleAction.Invoke();
            }
        }

        public ChoiceActionPairType choiceActionPairType;
        public string choice;
        public Action simpleAction;
        public Action<string> simpleStringAction;
        public Action<int> simpleIntAction;
        public string stringActionParameter;
        public int intActionParameter;
    }

}