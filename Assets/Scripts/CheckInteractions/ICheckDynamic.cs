using System.Collections;
using System.Collections.Generic;
using Frankie.Utils;

namespace Frankie.Control
{
    public interface ICheckDynamic
    {
        public string GetMessage();
        public List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateMachine);
    }
}
