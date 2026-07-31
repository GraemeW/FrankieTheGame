using System.Collections.Generic;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Menu.UI
{
    [System.Serializable]
    public class NameScreenQuestion
    {
        public string question = string.Empty;
        public GameObject thingPrefab; 
        public List<string> dontCareAnswers = new();
        public NameScreenQuestionType questionType = NameScreenQuestionType.CharacterName;
        public CharacterProperties optionalCharacterProperties;
    }
}
