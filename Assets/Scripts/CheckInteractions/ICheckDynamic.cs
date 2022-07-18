using Frankie.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public interface ICheckDynamic
    {
        public string GetMessage();
        public List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateMachine);
    }
}

