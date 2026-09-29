using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Properties;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox
{
    // Base for UI Toolkit data sources:  push-based change notification for runtime data binding
    // Usage:  mark bindable properties with [CreateProperty], route setters through SetProperty(...)
    public abstract class BindableModel : INotifyBindablePropertyChanged, IDataSourceViewHashProvider
    {
        // State
        private long version = 0;

        // Events
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        public long GetViewHashCode() => version;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) { return false; }

            field = value;
            version++;
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(new PropertyPath(propertyName)));
            return true;
        }
    }
}
