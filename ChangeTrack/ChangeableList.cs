using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ChangeTrack
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class ChangeableList<T> : ObservableCollection<T>, IChangeableList<T> where T : IChangeTrackable
    {
        /// <summary>Name of the Count property</summary>
        public const string CountProperty = "Count";
        /// <summary>Name of the ITEM property. Used when an items change event bubbles up 
        /// to descern from the <see cref="ObservableCollection&lt;T&gt;"/> own use of "Item[]"</summary>
        public const string ItemProperty = "Item";

        ~ChangeableList()
        {
            //TODO this.Detach();
        }

        private void ItemChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(ItemProperty));
        }

        private void HookItem(INotifyPropertyChanged item)
        {
            item.PropertyChanged += ItemChanged;
        }

        private void UnhookItem(INotifyPropertyChanged item)
        {
            item.PropertyChanged -= ItemChanged;
        }

        /// <summary>
        /// Inserts an item into the collection at the specified index.
        /// This implementation hooks change tracking up on the inserted item
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The object to insert.</param>
        protected override void InsertItem(int index, T item)
        {
            HookItem(item);
            base.InsertItem(index, item);
        }

        /// <summary>
        /// Removes the item at the specified index of the collection.
        /// This implementation unhooks change tracking from the removed item
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        protected override void RemoveItem(int index)
        {
            UnhookItem(this[index]);
            base.RemoveItem(index);
        }

        /// <summary>
        /// Removes all items from the collection.
        /// This implementation unhooks change tracking from all cleared items
        /// </summary>
        protected override void ClearItems()
        {
            foreach (var item in this)
                UnhookItem(item);
            base.ClearItems();
        }

        /// <summary>
        /// Replaces the element at the specified index.
        /// This implementation unhooks change tracking up from the removed item and hooks it on the inserted item
        /// </summary>
        /// <param name="index">The zero-based index of the element to replace.</param>
        /// <param name="item">The new value for the element at the specified index.</param>
        protected override void SetItem(int index, T item)
        {
            UnhookItem(this[index]);
            HookItem(item);
            base.SetItem(index, item);
        }
    }
}
