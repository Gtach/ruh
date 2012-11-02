using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace UnitOfWork
{
    /// <summary>
    /// Static class adding extension methods to IChangeableList instances
    /// </summary>
    public static class ChangeableListUoW
    {
        private static readonly IDictionary<int, IList<NotifyCollectionChangedEventArgs>> ChangeableLists = new Dictionary<int, IList<NotifyCollectionChangedEventArgs>>();

        /// <summary>
        /// Attaches the specified changeable list.
        /// </summary>
        /// <param name="changeableList">The changeable list.</param>
        public static void Attach(this IChangeableList changeableList)
        {
            CheckDetached(changeableList);
            InternalAttach(changeableList);
        }

        /// <summary>
        /// Detaches the specified changeable list.
        /// </summary>
        /// <param name="changeableList">The changeable list.</param>
        public static void Detach(this IChangeableList changeableList)
        {
            InternalDetach(changeableList);
        }

        /// <summary>
        /// Determines whether the specified changeable list is attached.
        /// </summary>
        /// <param name="changeableList">The changeable list.</param>
        /// <returns>
        /// 	<c>true</c> if the specified changeable list is attached; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAttached(this IChangeableList changeableList)
        {
            return ChangeableLists.ContainsKey(changeableList.GetHashCode());
        }

        /// <summary>
        /// Accepts the changes.
        /// </summary>
        /// <param name="changeableList">The changeable list.</param>
        public static void AcceptChanges(this IChangeableList changeableList)
        {
            CheckAttached(changeableList);
            InternalAcceptChanges(changeableList);
        }

        /// <summary>
        /// Rollbacks the specified changeable list.
        /// </summary>
        /// <param name="changeableList">The changeable list.</param>
        public static void RollbackChanges(this IChangeableList changeableList)
        {
            CheckAttached(changeableList);
            InternalRollback(changeableList);
        }

        /// <summary>
        /// States the specified changeable list.
        /// </summary>
        /// <param name="changeableList">The changeable list.</param>
        /// <returns></returns>
        public static ChangeState ListState(this IChangeableList changeableList)
        {
            return IsAttached(changeableList) ? (IsChanged(changeableList) ? ChangeState.Changed : ChangeState.Unchanged) : ChangeState.Unattached;
        }

        /// <summary>
        /// Added changeables.
        /// </summary>
        /// <param name="changeableList">The changeable list.</param>
        /// <returns></returns>
        public static IList<IChangeable> InsertedChangeables(this IChangeableList changeableList)
        {
            var list = new List<IChangeable>();
            foreach (var e in ChangeableLists[changeableList.GetHashCode()].Where(x => x.Action == NotifyCollectionChangedAction.Add))
                foreach (IChangeable changeable in e.NewItems)
                    if (changeableList.Contains(changeable))
                        list.Add(changeable);
            return list;
        }

        /// <summary>
        /// Removed changeables.
        /// </summary>
        /// <param name="changeableList">The changeable list.</param>
        /// <returns></returns>
        public static IList<IChangeable> RemovedChangeables(this IChangeableList changeableList)
        {
            var list = new List<IChangeable>();
            foreach (var e in ChangeableLists[changeableList.GetHashCode()].Where(x => x.Action == NotifyCollectionChangedAction.Remove))
                foreach (IChangeable changeable in e.OldItems)
                    if (!changeableList.Contains(changeable))
                        list.Add(changeable);
            return list;
        }

        #region internal

        internal static void InternalAttach(IChangeableList changeableList)
        {
            if ((changeableList == null) || changeableList.IsAttached()) return;

            ChangeableLists.Add(changeableList.GetHashCode(), new List<NotifyCollectionChangedEventArgs>());
            changeableList.CollectionChanged += ChangeableListCollectionChanged;
            AttachItems(changeableList);
        }

        internal static void InternalDetach(IChangeableList changeableList)
        {
            if ((changeableList == null) || !changeableList.IsAttached()) return;

            changeableList.CollectionChanged -= ChangeableListCollectionChanged;
            ChangeableLists.Remove(changeableList.GetHashCode());
            foreach (IChangeable item in changeableList)
                ChangeableUoW.InternalDetach(item);
        }

        internal static void InternalRollback(IChangeableList changeableList)
        {
            foreach (IChangeable changeable in changeableList)
                changeable.RollbackChanges();

            changeableList.CollectionChanged -= ChangeableListCollectionChanged;
            try
            {
                var changes = ChangeableLists[changeableList.GetHashCode()];
                while (changes.Count > 0)   // loop all changes backwards
                {
                    var changeArg = changes[changes.Count - 1];
                    switch (changeArg.Action)
                    {
                        case NotifyCollectionChangedAction.Add: DoRollback(changeArg.NewItems, index => changeableList.RemoveAt(changeArg.NewStartingIndex + index)); break;
                        case NotifyCollectionChangedAction.Remove: DoRollback(changeArg.OldItems, index => changeableList.Insert(changeArg.OldStartingIndex + index, changeArg.OldItems[index])); break;
                        case NotifyCollectionChangedAction.Move: DoRollback(changeArg.OldItems, index => changeableList.Move(changeArg.NewStartingIndex + index, changeArg.OldStartingIndex + index)); break;
                        default: throw new NotImplementedException();
                    }
                    changes.RemoveAt(changes.Count - 1);
                }
            }
            finally
            {
                changeableList.CollectionChanged += ChangeableListCollectionChanged;
            }
        }

        internal static void InternalAcceptChanges(IChangeableList changeableList)
        {
            if (changeableList == null) return;

            foreach (IChangeable changeable in changeableList)
                changeable.AcceptChanges();
            ChangeableLists[changeableList.GetHashCode()].Clear();
        }

        #endregion

        #region private

        private static void CheckAttached(IChangeableList changeableList)
        {
            if (changeableList == null) throw new ArgumentNullException("changeableList");
            if (!ChangeableLists.ContainsKey(changeableList.GetHashCode()))
                throw new ArgumentException("Instance not attached!");
        }

        private static void CheckDetached(IChangeableList changeableList)
        {
            if (changeableList == null) throw new ArgumentNullException("changeableList");
            if (ChangeableLists.ContainsKey(changeableList.GetHashCode()))
                throw new ArgumentException("Instance already attached!");
        }

        private static bool IsChanged(IChangeableList changeableList)
        {
            return (ChangeableLists[changeableList.GetHashCode()].Count > 0) || changeableList.Cast<IChangeable>().Any(x => x.ChangeableState() != ChangeState.Unchanged);
        }

        private delegate void RollbackAction(int index);

        private static void DoRollback(IList list, RollbackAction rollbackAction)
        {
            for (var i = 0; i < list.Count; i++)
                rollbackAction(i);
        }

        #endregion

        #region Eventhandling

        private static void ChangeableListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add: 
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Move: LogChanges(sender as IChangeableList, e); break;
                default: throw new NotImplementedException();
            }
        }

        private static void LogChanges(IChangeableList iChangeableList, NotifyCollectionChangedEventArgs e)
        {
            if (iChangeableList == null) return;
            ChangeableLists[iChangeableList.GetHashCode()].Add(e);

            if (e.Action == NotifyCollectionChangedAction.Add)
                AttachItems(e.NewItems);
        }

        private static void AttachItems(IEnumerable items)
        {
            foreach (IChangeable item in items)
                ChangeableUoW.InternalAttach(item);
        }

        #endregion
    }
}
