using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Linq.Expressions;

namespace UnitOfWork
{
    /// <summary>
    /// Implementation of the <see cref="IChangeable"/> interface.
    /// Implements <see cref="INotifyPropertyChanged"/> but adds event bubbling to notify changes in 
    /// instances it references to.
    /// </summary>
    [Serializable]
    public abstract class Changeable : IChangeable, IEquatable<Changeable>
    {
        private readonly PropertyChangedEventHandler _referenceChangedEventHandler;
        private readonly IDictionary<string, INotifyPropertyChanged> _references = new Dictionary<string, INotifyPropertyChanged>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Changeable"/> class.
        /// </summary>
        protected Changeable()
        {
            _referenceChangedEventHandler = ReferenceChanged;
        }

        ~Changeable()
        {
            this.Detach();
        }

        private void ReferenceChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((!(sender is INotifyCollectionChanged) || (e.PropertyName == "Count") || (e.PropertyName == "Item")))
                OnPropertyChanged(FindReferencePropertyName((INotifyPropertyChanged)sender), sender, sender);
        }

        private string FindReferencePropertyName(INotifyPropertyChanged reference)
        {
            foreach (KeyValuePair<string, INotifyPropertyChanged> keyValuePair in _references)
                if (keyValuePair.Value == reference)
                    return keyValuePair.Key;

            throw new InvalidOperationException("Reference not found!");
        }

        private void AddReference(INotifyPropertyChanged reference, string propertyName)
        {
            reference.PropertyChanged += _referenceChangedEventHandler;
            _references.Add(propertyName, reference);
        }

        private void RemoveReference(INotifyPropertyChanged reference, string propertyName)
        {
            reference.PropertyChanged -= _referenceChangedEventHandler;
            _references.Remove(propertyName);
        }

        private void OnPropertyChanging(string propertyName)
        {
            if (PropertyChanging != null)
                PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
        }

        private void OnPropertyChanged(string propertyName, object oldValue, object newValue)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new TrackablePropertyChangedEventArgs(propertyName, oldValue, newValue));
        }

        /// <summary>
        /// Sets the referenced property with the new value if they are not the same.
        /// If the property is a reference to a new <see cref="INotifyPropertyChanged"/> instance,
        /// it will be hooked to event bubbling
        /// Last, the method fires notifies about the change happened.
        /// </summary>
        /// <typeparam name="TP">Type of the property to change</typeparam>
        /// <param name="exp">The expression describing the property to change.</param>
        /// <param name="newValue">The new value the property should be set to.</param>
        /// <param name="property">The reference to property.</param>
        protected void SetProperty<TP>(Expression<Func<TP>> exp, TP newValue, ref TP property)
        {
// ReSharper disable CompareNonConstrainedGenericWithNull
            if ((property == null) && (newValue == null)) return;
// ReSharper restore CompareNonConstrainedGenericWithNull
            if ((property is Changeable) && (property as Changeable).Equals(newValue as Changeable)) return;
// ReSharper disable CompareNonConstrainedGenericWithNull
            if ((property != null) && property.Equals(newValue)) return;
// ReSharper restore CompareNonConstrainedGenericWithNull

            //the cast will always succeed
            var memberExpression = (MemberExpression)exp.Body;
            var propertyName = memberExpression.Member.Name;

            var oldValue = property;

            var notifyPropertyChanged = property as INotifyPropertyChanged;
            if (notifyPropertyChanged != null) RemoveReference(notifyPropertyChanged, propertyName);

            OnPropertyChanging(propertyName);

            property = newValue;

            var propertyChanged = property as INotifyPropertyChanged;
            if (propertyChanged != null) AddReference(propertyChanged, propertyName);

            OnPropertyChanged(propertyName, oldValue, newValue);
        }

        #region Implementation of INotifyPropertyChanging

        /// <summary>Event to notify an occurring change</summary>
        public event PropertyChangingEventHandler PropertyChanging;

        #endregion

        #region INotifyPropertyChanged Members

        /// <summary>Event to notify a change</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region IEquatable<Changeable> Members

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        public virtual bool Equals(Changeable other)
        {
            return ReferenceEquals(this, other);
        }

        #endregion
    }
}
