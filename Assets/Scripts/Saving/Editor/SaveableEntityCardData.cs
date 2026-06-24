using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace Frankie.Saving.Editor
{
    public class SaveableEntityCardData
    {
        // Core Data
        public SaveableEntity saveableEntity { get; private set; }
        public JObject saveableEntityStateDict { get; private set; }
        public Dictionary<string, SaveableSubCardData> subCards { get; private set; } = new();
        
        // Derived Data
        public string entityName { get; private set; }
        public string entityID { get; private set; }
        
        // UI State
        private Box cardView;
        private bool isGameObjectSelected = false;
        private Action cachedSelectCallback;
        
        // Const Tunables
        private const float _smallButtonWidth = 100f;
        private const float _standardButtonWidth = 175f;
        private const float _largeButtonWidth = 250f;
        private static readonly Color _selectEntityButtonColor = Color.cornflowerBlue;
        private static readonly Color _saveEntityButtonColor = Color.chocolate;
        private static readonly Color _gameObjectSelectedColor = Color.steelBlue / 1.5f;

        public SaveableEntityCardData(SaveableEntity saveableEntity, JObject saveableEntityStateDict)
        {
            this.saveableEntity = saveableEntity;
            saveableEntityStateDict ??= new JObject();
            this.saveableEntityStateDict = saveableEntityStateDict;
            
            if (saveableEntity == null) { return; }
            
            entityName = saveableEntity.gameObject.name;
            entityID = saveableEntity.GetUniqueIdentifier();
            if (saveableEntity.transform.parent != null) { entityName = $"{saveableEntity.transform.parent.gameObject.name}/{entityName}"; }
            
            foreach (ISaveableBase saveable in saveableEntity.GetSaveableComponents())
            {
                string typeString = saveable.GetType().ToString();
                SaveState saveState = null;
                if (saveableEntityStateDict.ContainsKey(typeString)) { saveState = saveableEntityStateDict[typeString]?.ToObject<SaveState>(); }
                subCards[typeString] = SaveableSubCardData.CreateTypeSpecificSubCard(saveable, saveState);
            }
            
            Selection.selectionChanged -= OnEntitySelected;
            Selection.selectionChanged += OnEntitySelected;
        }

        #region Getters
        public bool TryGetSaveableSubCardData<T>(out T matchTypeSubCardData)
        {
            matchTypeSubCardData = default;
            foreach (SaveableSubCardData subCardData in subCards.Values)
            {
                if (subCardData is not T matchedType) { continue; }
                matchTypeSubCardData = matchedType;
                return true;
            }
            return false;
        }

        public bool HasPlayerMoverWithAlteredPosition()
        {
            foreach (SaveableSubCardData subCardData in subCards.Values)
            {
                if (subCardData is not MoverSaveableSubCard moverSaveableSubCard || !moverSaveableSubCard.IsPlayerMoverSubCard()) { continue; }
                if (moverSaveableSubCard.IsSaveStateSynced()) { continue; }
                
                return true;
            }
            return false;
        }
        #endregion
        
        #region SettersUtility
        public void SelfReferenceInSubCards()
        {
            // Call separately because doing so in construction is unsafe
            foreach (SaveableSubCardData subCardData in subCards.Values)
            {
                subCardData.SetSaveableEntityCardData(this);
            }
        }

        public void ResetSaveableSyncFlag()
        {
            foreach (SaveableSubCardData subCardData in subCards.Values)
            {
                subCardData.ResetSyncFlag();
                subCardData.Redraw();
            }
        }
        
        public void UpdateStateDict(JObject updatedStateDict)
        {
            if (updatedStateDict == null) { return; }
            saveableEntityStateDict = updatedStateDict;
        }

        public void UpdateSaveableEntry(string typeString, SaveState updatedSaveState)
        {
            SaveableEntity.ManualCaptureSaveState(saveableEntityStateDict, typeString, updatedSaveState);
        }
        
        public void ClearStateDict()
        {
            saveableEntityStateDict = new JObject();
        }
        
        private void SelectAndFocusGameObject()
        {
            if (saveableEntity == null) { return; }
            
            Selection.activeGameObject = saveableEntity.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
        }
        #endregion
        
        #region DrawUIMethods
        public Box DrawSaveableEntityCard(Action<SaveableEntityCardData> saveCallback)
        {
            if (cardView != null)
            {
                cardView.Clear();
                cardView = null;
            }
            cardView = new Box { style = { marginBottom = 4 } };
            
            var entitySubHeader = new Box();
            
            var gameObjectRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            entitySubHeader.Add(gameObjectRow);
            
            gameObjectRow.Add(new Label($"GameObject:  {entityName}"));
            
            var focusGameObjectButton = new Button { text = "Select Entity", style = { width = _smallButtonWidth, backgroundColor = _selectEntityButtonColor, color = Color.white } };
            focusGameObjectButton.RegisterCallback<ClickEvent>(_ => SelectAndFocusGameObject());
            gameObjectRow.Add(focusGameObjectButton);
            
            entitySubHeader.Add(new Label($"ID:  {entityID}"));
            cardView.Add(entitySubHeader);
            
            var saveEntityButton = new Button { text = "Save Entity", style = { width = _standardButtonWidth, backgroundColor = _saveEntityButtonColor, color = Color.white } };
            entitySubHeader.Add(saveEntityButton);

            foreach (KeyValuePair<string, SaveableSubCardData> keyValuePair in subCards)
            {
                Box subCard = DrawISaveableSubCard(keyValuePair.Key, keyValuePair.Value);
                cardView.Add(subCard);
            }
            
            if (saveCallback != null) { saveEntityButton.RegisterCallback<ClickEvent>(_ => saveCallback(this)); }
            
            isGameObjectSelected = Selection.activeGameObject == saveableEntity.gameObject;
            UpdateSelectedColor();
            
            return cardView;
        }

        public void SetSelectCallback(Action selectCallback)
        {
            cachedSelectCallback = selectCallback;
        }
        
        private Box DrawISaveableSubCard(string typeString, SaveableSubCardData saveableSubCardData)
        {
            var subCardView = new Box { style = { marginTop = 2, marginLeft = 8 } };
            subCardView.Add(new Label($"Component:  {typeString}"));
            saveableSubCardData.DrawIntoSubCardView(subCardView);
            return subCardView;
        }
        
        private void OnEntitySelected()
        {
            if (saveableEntity == null || cardView == null) { return; }
            isGameObjectSelected = Selection.activeGameObject == saveableEntity.gameObject;
            UpdateSelectedColor();
            if (isGameObjectSelected) { cachedSelectCallback?.Invoke(); }
        }

        private void UpdateSelectedColor()
        {
            cardView.style.backgroundColor = isGameObjectSelected ? _gameObjectSelectedColor : StyleKeyword.Null;
        }
        #endregion
    }
}
