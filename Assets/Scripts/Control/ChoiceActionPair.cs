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
            isComplexAction = false;
            this.choice = choice;
            this.action = action;
            this.complexAction = null;
            this.complexActionParameter = null;
        }

        public ChoiceActionPair(string choice, Action<string> action, string parameter)
        {
            isComplexAction = true;
            this.choice = choice;
            this.action = null;
            this.complexAction = action;
            this.complexActionParameter = parameter;
        }

        public bool isComplexAction;
        public string choice;
        public Action action;
        public Action<string> complexAction;
        public string complexActionParameter;
    }

}