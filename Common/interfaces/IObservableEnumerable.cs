using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.Specialized;

namespace Common.interfaces
{
    /// <summary>
    /// Interface to a observable enumerable
    /// Hides Add or Remove functionality but offers notification
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IObservableEnumerable<out T> : IEnumerable<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
    }

    /// <summary>
    /// Interface to a observable enumerable
    /// Hides Add or Remove functionality but offers notification
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IObservableList<T> : IList<T>, IObservableEnumerable<T>
    {
    }
}
