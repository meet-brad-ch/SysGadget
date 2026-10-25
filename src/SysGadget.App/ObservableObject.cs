using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SysGadget.App
{
    /// <summary>Base class for view-models: raises PropertyChanged when a value changes.</summary>
    internal abstract class ObservableObject : INotifyPropertyChanged
    {
        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Stores a value and raises PropertyChanged when it differs.</summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="field">The backing field.</param>
        /// <param name="value">The new value.</param>
        /// <param name="name">The property name; filled in by the compiler.</param>
        protected void Set<T>(ref T field, T value, [CallerMemberName] string name = "")
        {
            if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                Raise(name);
            }
        }

        /// <summary>Raises PropertyChanged, for properties derived from others.</summary>
        /// <param name="name">The property name.</param>
        protected void Raise(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
