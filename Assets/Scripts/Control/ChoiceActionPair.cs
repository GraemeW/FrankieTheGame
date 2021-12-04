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
            this.choice = choice;
            this.action = action;
        }

        public string choice;
        public Action action;
    }
}