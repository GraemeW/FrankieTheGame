using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Tables;
using Frankie.Saving;
using Frankie.Utils.Localization;

namespace Frankie.Control
{
    [ExecuteInEditMode]
    public abstract class CheckBase : MonoBehaviour, IRaycastable, ISaveable<bool>, ILocalizable
    {
        // Tunables
        [SerializeField] protected bool overrideDefaultInteractionDistance = false;
        [SerializeField] protected float interactionDistance = 0.3f;
        
        // Localization Parameters
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.ChecksWorldObjects;
        public virtual List<TableEntryReference> GetLocalizationEntries() => new();

        // State
        private bool activeCheck = true;

        // Static + Const
        private const string _defaultLayerMask = "Interactable";
        private const string _inactiveLayerMask = "Ignore Raycast";
        protected const string defaultPartyLeaderName = "Frankie";
        
        protected void OnDestroy()
        {
            ILocalizable.TriggerOnDestroy(this);
        }
        
        protected bool IsInRange(PlayerController playerController)
        {
            return activeCheck && IRaycastable.CheckDistance(gameObject, transform.position, playerController, overrideDefaultInteractionDistance, interactionDistance);
        }
        
        public void SetActiveCheck(bool enable) // Called via Unity Events
        {
            activeCheck = enable;
            gameObject.layer = LayerMask.NameToLayer(enable ? _defaultLayerMask : _inactiveLayerMask);
        }
        
        #region RaycastableInterface
        public virtual CursorType GetCursorType() => CursorType.Check;

        public abstract bool HandleRaycast(PlayerStateMachine playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType);
        #endregion

        #region SaveInterface
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;

        public SaveState CaptureState() => ManualGetStateFromData(activeCheck);

        public virtual void RestoreState(SaveState saveState)
        {
            SetActiveCheck(ManualGetDataFromState(saveState));
        }
        #endregion

        public SaveState ManualGetStateFromData(bool data) => new(GetLoadPriority(), data);

        public bool ManualGetDataFromState(SaveState saveState) => (bool)saveState.GetState(typeof(bool));
    }
}
