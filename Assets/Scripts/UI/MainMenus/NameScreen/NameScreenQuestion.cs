using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Stats;
using Frankie.Utils.Localization;

namespace Frankie.Menu.UI
{
    [CreateAssetMenu(fileName = "New Name Screen Question", menuName = "NameScreen/New Question", order = 30)]
    public class NameScreenQuestion : ScriptableObject, ILocalizable
    {
        // Properties
        [SimpleLocalizedString(LocalizationTableType.UI, true)] public LocalizedString localizedQuestion;
        public GameObject thingPrefab; 
        public DontCareAnswer[] localizedDontCareAnswers = new DontCareAnswer[5];
        public NameScreenQuestionType questionType = NameScreenQuestionType.CharacterName;
        public CharacterProperties optionalCharacterProperties;
        
        // State
        [HideInInspector][SerializeField] private string cachedName;
        public string iCachedName { get => cachedName; set => cachedName = value; }
        
        // Localization
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            var localizationEntries = new List<TableEntryReference> { localizedQuestion.TableEntryReference };
            localizationEntries.AddRange(from dontCareAnswer in localizedDontCareAnswers where dontCareAnswer.entry != null select dontCareAnswer.entry.TableEntryReference);
            return localizationEntries;
        }
        
        public List<(string propertyName, LocalizedString localizedString, bool setToName)> GetPropertyLinkedLocalizationEntries()
        {
            var propertyLinkedLocalizationEntries = new List<(string propertyName, LocalizedString localizedString, bool setToName)>
            {
                new ValueTuple<string, LocalizedString, bool>(nameof(localizedQuestion), localizedQuestion, false)
            };
            if (localizedDontCareAnswers == null) { return propertyLinkedLocalizationEntries; }
            
            for (int i = 0; i < localizedDontCareAnswers.Length; i++)
            {
                if (localizedDontCareAnswers[i] == null || localizedDontCareAnswers[i].entry == null) { continue; }
                propertyLinkedLocalizationEntries.Add(new ValueTuple<string, LocalizedString, bool>($"{nameof(localizedDontCareAnswers)}[{i}]", localizedDontCareAnswers[i].entry, false));
            }
            return propertyLinkedLocalizationEntries;
        }
    }

    [Serializable]
    public class DontCareAnswer
    {
        [SimpleLocalizedString(LocalizationTableType.UI, true)] public LocalizedString entry;
    }
}
