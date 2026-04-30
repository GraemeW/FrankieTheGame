using System.Collections.Generic;
using UnityEngine;
using Frankie.Saving;
using Frankie.Utils;
using UnityEngine.Localization.Tables;

namespace Frankie.Control
{
    [ExecuteInEditMode]
    public abstract class CheckBase : MonoBehaviour, IRaycastable, ISaveable, ILocalizable
    {
        // Tunables
        [SerializeField] protected bool overrideDefaultInteractionDistance = false;
        [SerializeField] protected float interactionDistance = 0.3f;
        
        // Localizable Parameters
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

        public SaveState CaptureState()
        {
            var saveState = new SaveState(GetLoadPriority(), activeCheck);
            return saveState;
        }

        public virtual void RestoreState(SaveState state)
        {
            SetActiveCheck((bool)state.GetState(typeof(bool)));
        }
        #endregion
    }
}
