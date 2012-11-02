using System.ComponentModel;

namespace UnitOfWork
{
    /// <summary>
    /// Event arguments for use in IChangeabel handling. Inheirs from <see cref="PropertyChangedEventArgs"/> to 
    /// comply to an <see cref="INotifyPropertyChanged"/> implementation.
    /// Adds the old and the new set values so that they don't need to be reflected
    /// </summary>
    public class TrackablePropertyChangedEventArgs : PropertyChangedEventArgs
    {
        /// <summary>The readonly old value</summary>
        public readonly object OldValue;
        /// <summary>The readonly new value</summary>
        public readonly object NewValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackablePropertyChangedEventArgs"/> class.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        public TrackablePropertyChangedEventArgs(string propertyName, object oldValue, object newValue)
            : base(propertyName)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
