using System;
using System.Collections.Generic;
using Unity.Properties;
using LowDefMustard.UIBox;

namespace Frankie.Inventory.UI
{
    public sealed class EquipmentModel : BindableModel, IDisposable
    {
        // Live readout of a character's equipment:  one slot per equip location (in EquipLocation order), empty or not
        // Note:  Values are raw (items, not display text); screens and rows decide how to present them

        // State
        private IReadOnlyList<EquipmentSlot> internalSlots;
        private bool isDisposed = false;

        // Constructor
        public EquipmentModel(Equipment equipment)
        {
            this.equipment = equipment;
            internalSlots = ReadSlots();
            equipment.equipmentUpdated += HandleEquipmentUpdated;
        }

        #region BoundProperties
        // Replaced wholesale on every equipment update
        [CreateProperty] public IReadOnlyList<EquipmentSlot> slots
        {
            get => internalSlots;
            private set => SetProperty(ref internalSlots, value);
        }
        #endregion

        #region PublicMethods
        public Equipment equipment { get; }

        public void Dispose()
        {
            if (isDisposed) { return; }
            isDisposed = true;
            if (equipment != null) { equipment.equipmentUpdated -= HandleEquipmentUpdated; }
        }
        #endregion

        #region PrivateMethods
        private void HandleEquipmentUpdated(EquipableItemBase equipableItem) => slots = ReadSlots();

        private EquipmentSlot[] ReadSlots()
        {
            var equipmentSlots = new List<EquipmentSlot>();
            foreach (EquipLocation equipLocation in Enum.GetValues(typeof(EquipLocation)))
            {
                if (equipLocation == EquipLocation.None) { continue; }
                equipmentSlots.Add(new EquipmentSlot(equipLocation, equipment.GetItemInSlot(equipLocation)));
            }
            return equipmentSlots.ToArray();
        }
        #endregion
    }
}
