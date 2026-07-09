using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Frankie.Stats;

namespace Frankie.Saving.Editor
{
    public class PartyAssistSubCard : PartyBaseSubCard
    {
        public PartyAssistSubCard(ISaveableBase saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }

        protected override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not PartyAssist partyAssist) { return; }
            
            if (!partyAssist.TryManualGetDataFromState(saveState, out HashSet<CharacterProperties> partyAssistSaveData))
            {
                subCardView.Add(new Label("No PartyAssist save data available"));
                return;
            }

            // Section 1 - Party Assist Character Select
            var partyAssistCharacters = new List<CharacterProperties>(partyAssistSaveData);
            var listContainer = new VisualElement();
            subCardView.Add(listContainer);

            var addButton = new Button { text = "+ Add Character", style = { width = standardButtonWidth } };
            subCardView.Add(addButton);
            DrawBasicPartyList(listContainer, partyAssist, partyAssistCharacters);
            
            // Section 2 -- Party Entity View
            subCardView.Add(new Label("Party Entity View"));
            characterEntityContainer = new VisualElement();
            subCardView.Add(characterEntityContainer);
            ReconcileEntityView(partyAssistCharacters);
            
            // Button Callbacks
            addButton.RegisterCallback<ClickEvent>(_ =>
            {
                partyAssistCharacters.Add(null);
                saveState = partyAssist.ManualGetStateFromData(partyAssistCharacters.ToHashSet());
                RaiseSaveStateChanged();
                DrawBasicPartyList(listContainer, partyAssist, partyAssistCharacters);
            });
        }
    }
}
