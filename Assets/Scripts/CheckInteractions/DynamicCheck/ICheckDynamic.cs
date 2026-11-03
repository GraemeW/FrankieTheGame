using System.Collections.Generic;
using LowDefMustard.Utils;
using Frankie.Core;

namespace Frankie.Control
{
    public interface ICheckDynamic
    {
        public string GetMessage(PlayerStateMachine playerStateMachine);
        public List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateMachine);
    }
}
