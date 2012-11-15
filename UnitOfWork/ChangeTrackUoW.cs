using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using ChangeTrack;
using Common.interfaces;

namespace UnitOfWork
{
    public class ChangeTrackUoW : IUnitOfWork
    {
        private readonly IPropertyManager _propertyManager;
        private readonly IRepository _repository;
        private readonly ICollection<int> _changeables = new HashSet<int>();
        private readonly ICollection<int> _newChangeables = new HashSet<int>();
        private readonly ICollection<int> _deletedChangeables = new HashSet<int>();
        private readonly IDictionary<int, Changes> _changedChangeables = new Dictionary<int, Changes>();

        private IChangeTrackable _rootTrackable;

        public ChangeTrackUoW(IPropertyManager propertyManager, IRepository repository)
        {
            if (propertyManager == null) throw new ArgumentNullException("propertyManager");
            if (repository == null) throw new ArgumentNullException("repository");

            _propertyManager = propertyManager;
            _repository = repository;
        }

        public void StartTransaction(object rootItem)
        {
            if (rootItem == null) throw new ArgumentNullException("rootItem");
            if (!(rootItem is IChangeTrackable)) throw new InvalidOperationException("rootItem is not an " + typeof(IChangeTrackable).Name);

            if (_rootTrackable != null) throw new InvalidOperationException("Transaction already running");

            _rootTrackable = (IChangeTrackable)rootItem;

            CheckDetached(_rootTrackable);
            InternalAttach(_rootTrackable);
        }

        public void Commit()
        {
            if (_rootTrackable == null) throw new InvalidOperationException("No transaction running");

            InternalAcceptChanges(_rootTrackable);

            CloseTransaction();
        }

        public void Rollback()
        {
            if (_rootTrackable == null) throw new InvalidOperationException("No transaction running");

            //            InternalRollback(changeTrackable);
            CloseTransaction();
        }

        /// <summary>
        /// Attaches the specified changeTrackable.
        /// </summary>
        /// <param name="changeTrackable">The changeTrackable.</param>
        private void InternalAttach(IChangeTrackable changeTrackable)
        {
            if ((changeTrackable == null) || IsAttached(changeTrackable)) return;

            foreach (var changeablePropertyInfo in _propertyManager.GetInfos(changeTrackable))
                AttachChangeableValue(changeablePropertyInfo, changeTrackable);

            changeTrackable.PropertyChanged += ChangeablePropertyChanged;

            _changeables.Add(changeTrackable.GetHashCode());
            _newChangeables.Add(changeTrackable.GetHashCode());

        }

        private void CheckDetached(IChangeTrackable changeTrackable)
        {
            if (changeTrackable == null) throw new ArgumentNullException("changeTrackable");
            if (_changeables.Contains(changeTrackable.GetHashCode())) throw new ArgumentException("Instance already attached!");
        }

        private bool IsAttached(IChangeTrackable changeTrackable)
        {
            return _changeables.Contains(changeTrackable.GetHashCode());
        }

        private void AttachChangeableValue(IPropertyManagerInfo propertyManagerInfo, IChangeTrackable changeTrackable)
        {
            switch (propertyManagerInfo.InfoType)
            {
                case PropertyManagerInfoType.Reference: InternalAttach(propertyManagerInfo.GetReferenceValue<IChangeTrackable>(changeTrackable)); break;
                //TODO case ChangeablePropertyInfoType.Association: ChangeableListUoW.InternalAttach(changeablePropertyInfo.GetAssociationValue(changeTrackable)); break;
            }
        }

        private bool HasNotTrackableAttribute(MemberInfo memberInfo)
        {
            return Attribute.GetCustomAttributes(memberInfo).OfType<NotTrackedAttribute>().Any();
        }

        private void ChangeablePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var args = e as TrackablePropertyChangedEventArgs;
            var propertyInfo = _propertyManager.GetInfo(sender, e.PropertyName);

            if ((args == null) || (propertyInfo == null)) return;

            var changeable = (IChangeTrackable)sender;
            if (!IsNew(changeable) && !IsDeleted(changeable))
            {
                var hashCode = changeable.GetHashCode();
                Changes changes;
                if (!_changedChangeables.TryGetValue(hashCode, out changes))
                    _changedChangeables.Add(hashCode, changes = new Changes());

                changes.Add(args.PropertyName, new Change { OldValue = args.OldValue, NewValue = args.NewValue });
            }

            AttachChangeableValue(propertyInfo, changeable);
        }

        private bool IsNew(IChangeTrackable changeTrackable)
        {
            return _newChangeables.Contains(changeTrackable.GetHashCode());
        }

        private bool IsChanged(IChangeTrackable changeTrackable)
        {
            return _changedChangeables.ContainsKey(changeTrackable.GetHashCode());
        }

        private bool IsDeleted(IChangeTrackable changeTrackable)
        {
            return _deletedChangeables.Contains(changeTrackable.GetHashCode());
        }

        private void CloseTransaction()
        {
            _rootTrackable = null;
            _changeables.Clear();
            _newChangeables.Clear();
            _deletedChangeables.Clear();
            _changedChangeables.Clear();
        }

        private void InternalRollback(IChangeTrackable changeTrackable)
        {
            if (ChangeableState(changeTrackable) != ChangeState.Changed) return;

            var type = changeTrackable.GetType();
            foreach (var keyValuePair in _changedChangeables[changeTrackable.GetHashCode()].AllChanges)
                _propertyManager.GetInfo(type, keyValuePair.Key).SetValue(changeTrackable, RollbackValidObject(keyValuePair.Value.OldValue));

            InternalAcceptChanges(changeTrackable);
        }

        private ChangeState ChangeableState(IChangeTrackable changeTrackable)
        {
            if (!IsAttached(changeTrackable)) return ChangeState.Unattached;
            if (IsDeleted(changeTrackable)) return ChangeState.Deleted;
            if (IsNew(changeTrackable)) return ChangeState.New;
            return IsChanged(changeTrackable) ? ChangeState.Changed : ChangeState.Unchanged;
        }

        private void InternalAcceptChanges(IChangeTrackable changeTrackable)
        {
            if (changeTrackable == null) return;

            var type = changeTrackable.GetType();
            foreach (var propertyManagerInfo in _propertyManager.GetInfos(type))
                AcceptChangeableValue(propertyManagerInfo, changeTrackable);

            var hashCode = changeTrackable.GetHashCode();
            if (IsNew(changeTrackable))
            {
                _repository.Add(changeTrackable);
                _newChangeables.Remove(hashCode);
            }
            else if (IsChanged(changeTrackable))
            {
                _repository.Update(changeTrackable);
                _changedChangeables.Remove(hashCode);
            }
        }

        private void AcceptChangeableValue(IPropertyManagerInfo propertyManagerInfo, IChangeTrackable changeTrackable)
        {
            switch (propertyManagerInfo.InfoType)
            {
                case PropertyManagerInfoType.Reference: InternalAcceptChanges(propertyManagerInfo.GetReferenceValue<IChangeTrackable>(changeTrackable)); break;
                //TODOcase ChangeablePropertyInfoType.Association: ChangeableListUoW.InternalAcceptChanges(changeablePropertyInfo.GetAssociationValue(changeTrackable)); break;
            }
        }

        private object RollbackValidObject(object obj)
        {
            if (obj is IChangeTrackable) InternalRollback((IChangeTrackable)obj);
            //TODO else if (obj is IChangeableList) ((IChangeableList)obj).RollbackChanges();

            return obj;
        }
    }

    /*
        /// <summary>
        /// Static class adding extension methods to IChangeTrackable instances
        /// </summary>
        public static class ChangeableUoW
        {
            /// <summary>Dictionary of reflected ChangeablePropertyInfo for each class attached during runtime</summary>
            private static readonly IDictionary<Type, IDictionary<string, ChangeablePropertyInfo>> PropertyInfos = new Dictionary<Type, IDictionary<string, ChangeablePropertyInfo>>();
            /// <summary>Dictionary of IChangeables attached to the UoW, keying to a list of parents they are reference to for change notification</summary>
            private static readonly ICollection<int> Changeables = new HashSet<int>();
            /// <summary>Dictionary of IChangeables newly attached</summary>
            private static readonly ICollection<int> NewChangeables = new HashSet<int>();
            /// <summary>Dictionary of IChangeables deleted under attachment</summary>
            private static readonly ICollection<int> DeletedChangeables = new HashSet<int>();
            /// <summary>Dictionary of IChangeables that have been changed during attachment</summary>
            private static readonly IDictionary<int, Changes> ChangedChangeables = new Dictionary<int, Changes>();

            private static readonly PropertyChangedEventHandler PropertyChangedEventHandler = ChangeablePropertyChanged;

            #region public static

            /// <summary>
            /// Attaches the specified changeTrackable.
            /// </summary>
            /// <param name="changeTrackable">The changeTrackable.</param>
            public static void Attach(this IChangeTrackable changeTrackable)
            {
                CheckDetached(changeTrackable);
                InternalAttach(changeTrackable);
            }

            /// <summary>
            /// Detaches the specified changeTrackable.
            /// </summary>
            /// <param name="changeTrackable">The changeTrackable.</param>
            public static void Detach(this IChangeTrackable changeTrackable)
            {
                InternalDetach(changeTrackable);
            }

            /// <summary>
            /// Accepts the changes.
            /// </summary>
            /// <param name="changeTrackable">The changeTrackable.</param>
            public static void AcceptChanges(this IChangeTrackable changeTrackable)
            {
                CheckAttached(changeTrackable);
                InternalAcceptChanges(changeTrackable);
            }

            /// <summary>
            /// Deletes the specified changeTrackable.
            /// </summary>
            /// <param name="changeTrackable">The changeTrackable.</param>
            public static void Remove(this IChangeTrackable changeTrackable)
            {
                CheckAttached(changeTrackable);
                InternalDelete(changeTrackable);
            }

            /// <summary>
            /// Rollbacks the specified changeTrackable.
            /// </summary>
            /// <param name="changeTrackable">The changeTrackable.</param>
            public static void RollbackChanges(this IChangeTrackable changeTrackable)
            {
                CheckAttached(changeTrackable);
                InternalRollback(changeTrackable);
            }

            /// <summary>
            /// Determines whether the specified changeTrackable is attached.
            /// </summary>
            /// <param name="changeTrackable">The changeTrackable.</param>
            /// <returns>
            /// 	<c>true</c> if the specified changeTrackable is attached; otherwise, <c>false</c>.
            /// </returns>
            public static bool IsAttached(this IChangeTrackable changeTrackable)
            {
                return Changeables.Contains(changeTrackable.GetHashCode());
            }

            /// <summary>
            /// States the specified changeTrackable.
            /// </summary>
            /// <param name="changeTrackable">The changeTrackable.</param>
            /// <returns></returns>
            public static ChangeState ChangeableState(this IChangeTrackable changeTrackable)
            {
                if (!changeTrackable.IsAttached()) return ChangeState.Unattached;
                if (IsDeleted(changeTrackable)) return ChangeState.Deleted;
                if (IsNew(changeTrackable)) return ChangeState.New;
                return IsChanged(changeTrackable) ? ChangeState.Changed : ChangeState.Unchanged;
            }

            /// <summary>
            /// Determines whether the specified changeTrackable has changes.
            /// </summary>
            /// <param name="changeTrackable">The changeTrackable.</param>
            /// <returns>
            /// 	<c>true</c> if the specified changeTrackable has changes; otherwise, <c>false</c>.
            /// </returns>
            public static bool HasChanges(this IChangeTrackable changeTrackable)
            {
                Changes changes;
                return ChangedChangeables.TryGetValue(changeTrackable.GetHashCode(), out changes) && changes.HasChanges(PropertyInfos[changeTrackable.GetType()]);
            }

            /// <summary>
            /// Referenceses the specified changeTrackable.
            /// </summary>
            /// <param name="changeTrackable">The changeTrackable.</param>
            /// <returns></returns>
            public static IDictionary<string, IChangeTrackable> References(this IChangeTrackable changeTrackable)
            {
                return PropertiesOfType<IChangeTrackable>(changeTrackable, ChangeablePropertyInfoType.Reference);
            }

            /// <summary>
            /// Associationses the specified changeTrackable.
            /// </summary>
            /// <param name="changeTrackable">The changeTrackable.</param>
            /// <returns></returns>
            public static IDictionary<string, IChangeableList> Associations(this IChangeTrackable changeTrackable)
            {
                return PropertiesOfType<IChangeableList>(changeTrackable, ChangeablePropertyInfoType.Association);
            }

            #endregion

            #region internal

            internal static void InternalAttach(IChangeTrackable changeTrackable)
            {
                if ((changeTrackable == null) || IsAttached(changeTrackable)) return;
                CheckPropertyCache(changeTrackable);

                foreach (var changeablePropertyInfo in PropertyInfos[changeTrackable.GetType()].Values)
                    AttachChangeableValue(changeablePropertyInfo, changeTrackable);

                changeTrackable.PropertyChanged += PropertyChangedEventHandler;

                Changeables.Add(changeTrackable.GetHashCode());
                NewChangeables.Add(changeTrackable.GetHashCode());
            }

            internal static void InternalDetach(IChangeTrackable changeTrackable)
            {
                if ((changeTrackable == null) || !IsAttached(changeTrackable)) return;

                foreach (var changeablePropertyInfo in PropertyInfos[changeTrackable.GetType()].Values)
                    DetachChangeableValue(changeablePropertyInfo, changeTrackable);

                changeTrackable.PropertyChanged -= PropertyChangedEventHandler;

                var hashCode = changeTrackable.GetHashCode();

                if (NewChangeables.Contains(hashCode)) NewChangeables.Remove(hashCode);
                if (DeletedChangeables.Contains(hashCode)) DeletedChangeables.Remove(hashCode);
                if (ChangedChangeables.ContainsKey(hashCode)) ChangedChangeables.Remove(hashCode);
                Changeables.Remove(hashCode);
            }

            #endregion

            #region private

            private static IDictionary<string, T> PropertiesOfType<T>(IChangeTrackable changeTrackable, ChangeablePropertyInfoType changeablePropertyInfoType) where T : class
            {
                CheckAttached(changeTrackable);

                IDictionary<string, T> properties = new Dictionary<string, T>();

                foreach (var changeablePropertyInfo in PropertyInfos[changeTrackable.GetType()].Values.Where(x => x.InfoType == changeablePropertyInfoType))
                {
                    var t = changeablePropertyInfo.GetValue(changeTrackable) as T;
                    if (t != null)
                        properties.Add(changeablePropertyInfo.PropertyInfo.Name, t);
                }

                return properties;
            }

            private static bool IsNew(IChangeTrackable changeTrackable)
            {
                return NewChangeables.Contains(changeTrackable.GetHashCode());
            }

            private static bool IsChanged(IChangeTrackable changeTrackable)
            {
                return ChangedChangeables.ContainsKey(changeTrackable.GetHashCode());
            }

            private static bool IsDeleted(IChangeTrackable changeTrackable)
            {
                return DeletedChangeables.Contains(changeTrackable.GetHashCode());
            }

            private static void InternalAcceptChanges(IChangeTrackable changeTrackable)
            {
                if (changeTrackable != null)
                {
                    foreach (var changeablePropertyInfo in PropertyInfos[changeTrackable.GetType()].Values)
                        AcceptChangeableValue(changeablePropertyInfo, changeTrackable);

                    var hashCode = changeTrackable.GetHashCode();
                    if (IsNew(changeTrackable)) NewChangeables.Remove(hashCode);
                    if (IsChanged(changeTrackable)) ChangedChangeables.Remove(hashCode);
                }
            }

            private static void InternalDelete(IChangeTrackable changeTrackable)
            {
                var hashCode = changeTrackable.GetHashCode();
                if (IsNew(changeTrackable)) NewChangeables.Remove(hashCode);
                if (IsChanged(changeTrackable)) ChangedChangeables.Remove(hashCode);

                DeletedChangeables.Add(changeTrackable.GetHashCode());
            }

            private static void CheckAttached(IChangeTrackable changeTrackable)
            {
                if (changeTrackable == null) throw new ArgumentNullException("changeTrackable");
                if (!Changeables.Contains(changeTrackable.GetHashCode()))
                    throw new ArgumentException("Instance not attached!");
            }

            private static void CheckDetached(IChangeTrackable changeTrackable)
            {
                if (changeTrackable == null) throw new ArgumentNullException("changeTrackable");
                if (Changeables.Contains(changeTrackable.GetHashCode()))
                    throw new ArgumentException("Instance already attached!");
            }

            private static void InternalRollback(IChangeTrackable changeTrackable)
            {
                if (changeTrackable.ChangeableState() != ChangeState.Changed) return;

                foreach (var keyValuePair in ChangedChangeables[changeTrackable.GetHashCode()].AllChanges)
                    PropertyInfos[changeTrackable.GetType()][keyValuePair.Key].SetValue(changeTrackable, RollbackValidObject(keyValuePair.Value.OldValue));

                InternalAcceptChanges(changeTrackable);
            }

            private static void AttachChangeableValue(ChangeablePropertyInfo changeablePropertyInfo, IChangeTrackable changeTrackable)
            {
                switch (changeablePropertyInfo.InfoType)
                {
                    case ChangeablePropertyInfoType.Reference: InternalAttach(changeablePropertyInfo.GetReferenceValue(changeTrackable)); break;
                    case ChangeablePropertyInfoType.Association: ChangeableListUoW.InternalAttach(changeablePropertyInfo.GetAssociationValue(changeTrackable)); break;
                }
            }

            private static void DetachChangeableValue(ChangeablePropertyInfo changeablePropertyInfo, IChangeTrackable changeTrackable)
            {
                switch (changeablePropertyInfo.InfoType)
                {
                    case ChangeablePropertyInfoType.Reference: InternalDetach(changeablePropertyInfo.GetReferenceValue(changeTrackable)); break;
                    case ChangeablePropertyInfoType.Association: ChangeableListUoW.InternalDetach(changeablePropertyInfo.GetAssociationValue(changeTrackable)); break;
                }
            }

            private static void AcceptChangeableValue(ChangeablePropertyInfo changeablePropertyInfo, IChangeTrackable changeTrackable)
            {
                switch (changeablePropertyInfo.InfoType)
                {
                    case ChangeablePropertyInfoType.Reference: InternalAcceptChanges(changeablePropertyInfo.GetReferenceValue(changeTrackable)); break;
                    case ChangeablePropertyInfoType.Association: ChangeableListUoW.InternalAcceptChanges(changeablePropertyInfo.GetAssociationValue(changeTrackable)); break;
                }
            }

            private static object RollbackValidObject(object obj)
            {
                if (obj is IChangeTrackable)
                    InternalRollback((IChangeTrackable)obj);
                else if (obj is IChangeableList)
                    ((IChangeableList)obj).RollbackChanges();

                return obj;
            }

            private static void CheckPropertyCache(IChangeTrackable changeTrackable)
            {
                var type = changeTrackable.GetType();

                if (PropertyInfos.ContainsKey(type)) return;

                IDictionary<string, ChangeablePropertyInfo> dictionary = new Dictionary<string, ChangeablePropertyInfo>();
                foreach (var propertyInfo in type.GetProperties())
                    if (!HasNotTrackableAttribute(propertyInfo))
                        dictionary.Add(propertyInfo.Name, new ChangeablePropertyInfo(propertyInfo));
                PropertyInfos.Add(type, dictionary);
            }

            private static bool HasNotTrackableAttribute(MemberInfo memberInfo)
            {
                return Attribute.GetCustomAttributes(memberInfo).OfType<NotTrackedAttribute>().Any();
            }

            #endregion

            #region Change event handling

            private static void ChangeablePropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                var changeable = (IChangeTrackable)sender;
                var args = e as TrackablePropertyChangedEventArgs;
                var propertyInfo = PropertyInfos[changeable.GetType()];

                if ((args == null) || !propertyInfo.ContainsKey(e.PropertyName)) return;

                if (!IsNew(changeable) && !IsDeleted(changeable))
                {
                    var hashCode = changeable.GetHashCode();
                    Changes changes;
                    if (!ChangedChangeables.TryGetValue(hashCode, out changes))
                        ChangedChangeables.Add(hashCode, changes = new Changes());

                    changes.Add(args.PropertyName, new Change { OldValue = args.OldValue, NewValue = args.NewValue });
                }

                AttachChangeableValue(propertyInfo[e.PropertyName], changeable);
            }

            #endregion
        }
    */

    /*
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
        public static IList<IChangeTrackable> InsertedChangeables(this IChangeableList changeableList)
        {
            var list = new List<IChangeTrackable>();
            foreach (var e in ChangeableLists[changeableList.GetHashCode()].Where(x => x.Action == NotifyCollectionChangedAction.Add))
                foreach (IChangeTrackable changeable in e.NewItems)
                    if (changeableList.Contains(changeable))
                        list.Add(changeable);
            return list;
        }

        /// <summary>
        /// Removed changeables.
        /// </summary>
        /// <param name="changeableList">The changeable list.</param>
        /// <returns></returns>
        public static IList<IChangeTrackable> RemovedChangeables(this IChangeableList changeableList)
        {
            var list = new List<IChangeTrackable>();
            foreach (var e in ChangeableLists[changeableList.GetHashCode()].Where(x => x.Action == NotifyCollectionChangedAction.Remove))
                foreach (IChangeTrackable changeable in e.OldItems)
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
            foreach (IChangeTrackable item in changeableList)
                ChangeableUoW.InternalDetach(item);
        }

        internal static void InternalRollback(IChangeableList changeableList)
        {
            foreach (IChangeTrackable changeable in changeableList)
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

            foreach (IChangeTrackable changeable in changeableList)
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
            return (ChangeableLists[changeableList.GetHashCode()].Count > 0) || changeableList.Cast<IChangeTrackable>().Any(x => x.ChangeableState() != ChangeState.Unchanged);
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
            foreach (IChangeTrackable item in items)
                ChangeableUoW.InternalAttach(item);
        }

        #endregion
    }
    */
}
