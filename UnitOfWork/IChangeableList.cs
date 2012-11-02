using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections;
using Common.interfaces;

namespace UnitOfWork
{
    /// <summary>
    /// Interface for changeable list classes. Adds extension methods when using UnitOfWork namespace
    /// </summary>
    public interface IChangeableList : IList, INotifyCollectionChanged, INotifyPropertyChanged
    {
        /// <summary>
        /// Moves the instance at oldIndex to newIndex
        /// </summary>
        /// <param name="oldIndex">The old index.</param>
        /// <param name="newIndex">The new index.</param>
        void Move(int oldIndex, int newIndex);
    }

    /// <summary>
    /// Interface for changeable list classes. Adds extension methods when using UnitOfWork namespace
    /// </summary>
    public interface IChangeableList<T> : IList<T>, IObservableEnumerable<T>, IChangeableList where T : IChangeable
    {
    }
}
