using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace UnitOfWork
{
    /// <summary>
    /// Static class adding extension methods to IChangeable instances
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
        /// Attaches the specified changeable.
        /// </summary>
        /// <param name="changeable">The changeable.</param>
        public static void Attach(this IChangeable changeable)
        {
            CheckDetached(changeable);
            InternalAttach(changeable);
        }

        /// <summary>
        /// Detaches the specified changeable.
        /// </summary>
        /// <param name="changeable">The changeable.</param>
        public static void Detach(this IChangeable changeable)
        {
            InternalDetach(changeable);
        }

        /// <summary>
        /// Accepts the changes.
        /// </summary>
        /// <param name="changeable">The changeable.</param>
        public static void AcceptChanges(this IChangeable changeable)
        {
            CheckAttached(changeable);
            InternalAcceptChanges(changeable);
        }

        /// <summary>
        /// Deletes the specified changeable.
        /// </summary>
        /// <param name="changeable">The changeable.</param>
        public static void Remove(this IChangeable changeable)
        {
            CheckAttached(changeable);
            InternalDelete(changeable);
        }

        /// <summary>
        /// Rollbacks the specified changeable.
        /// </summary>
        /// <param name="changeable">The changeable.</param>
        public static void RollbackChanges(this IChangeable changeable)
        {
            CheckAttached(changeable);
            InternalRollback(changeable);
        }

        /// <summary>
        /// Determines whether the specified changeable is attached.
        /// </summary>
        /// <param name="changeable">The changeable.</param>
        /// <returns>
        /// 	<c>true</c> if the specified changeable is attached; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAttached(this IChangeable changeable)
        {
            return Changeables.Contains(changeable.GetHashCode());
        }

        /// <summary>
        /// States the specified changeable.
        /// </summary>
        /// <param name="changeable">The changeable.</param>
        /// <returns></returns>
        public static ChangeState ChangeableState(this IChangeable changeable)
        {
            if (!changeable.IsAttached()) return ChangeState.Unattached;
            if (IsDeleted(changeable)) return ChangeState.Deleted;
            if (IsNew(changeable)) return ChangeState.New;
            return IsChanged(changeable) ? ChangeState.Changed : ChangeState.Unchanged;
        }

        /// <summary>
        /// Determines whether the specified changeable has changes.
        /// </summary>
        /// <param name="changeable">The changeable.</param>
        /// <returns>
        /// 	<c>true</c> if the specified changeable has changes; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasChanges(this IChangeable changeable)
        {
            Changes changes;
            return ChangedChangeables.TryGetValue(changeable.GetHashCode(), out changes) && changes.HasChanges(PropertyInfos[changeable.GetType()]);
        }

        /// <summary>
        /// Referenceses the specified changeable.
        /// </summary>
        /// <param name="changeable">The changeable.</param>
        /// <returns></returns>
        public static IDictionary<string, IChangeable> References(this IChangeable changeable)
        {
            return PropertiesOfType<IChangeable>(changeable, ChangeablePropertyInfoType.Reference);
        }

        /// <summary>
        /// Associationses the specified changeable.
        /// </summary>
        /// <param name="changeable">The changeable.</param>
        /// <returns></returns>
        public static IDictionary<string, IChangeableList> Associations(this IChangeable changeable)
        {
            return PropertiesOfType<IChangeableList>(changeable, ChangeablePropertyInfoType.Association);
        }

        #endregion

        #region internal

        internal static void InternalAttach(IChangeable changeable)
        {
            if ((changeable == null) || IsAttached(changeable)) return;
            CheckPropertyCache(changeable);

            foreach (var changeablePropertyInfo in PropertyInfos[changeable.GetType()].Values)
                AttachChangeableValue(changeablePropertyInfo, changeable);

            changeable.PropertyChanged += PropertyChangedEventHandler;

            Changeables.Add(changeable.GetHashCode());
            NewChangeables.Add(changeable.GetHashCode());
        }

        internal static void InternalDetach(IChangeable changeable)
        {
            if ((changeable == null) || !IsAttached(changeable)) return;

            foreach (var changeablePropertyInfo in PropertyInfos[changeable.GetType()].Values)
                DetachChangeableValue(changeablePropertyInfo, changeable);

            changeable.PropertyChanged -= PropertyChangedEventHandler;

            var hashCode = changeable.GetHashCode();

            if (NewChangeables.Contains(hashCode)) NewChangeables.Remove(hashCode);
            if (DeletedChangeables.Contains(hashCode)) DeletedChangeables.Remove(hashCode);
            if (ChangedChangeables.ContainsKey(hashCode)) ChangedChangeables.Remove(hashCode);
            Changeables.Remove(hashCode);
        }

        #endregion

        #region private

        private static IDictionary<string, T> PropertiesOfType<T>(IChangeable changeable, ChangeablePropertyInfoType changeablePropertyInfoType) where T : class
        {
            CheckAttached(changeable);

            IDictionary<string, T> properties = new Dictionary<string, T>();

            foreach (var changeablePropertyInfo in PropertyInfos[changeable.GetType()].Values.Where(x => x.InfoType == changeablePropertyInfoType))
            {
                var t = changeablePropertyInfo.GetValue(changeable) as T;
                if (t != null)
                    properties.Add(changeablePropertyInfo.PropertyInfo.Name, t);
            }

            return properties;
        }

        private static bool IsNew(IChangeable changeable)
        {
            return NewChangeables.Contains(changeable.GetHashCode());
        }

        private static bool IsChanged(IChangeable changeable)
        {
            return ChangedChangeables.ContainsKey(changeable.GetHashCode());
        }

        private static bool IsDeleted(IChangeable changeable)
        {
            return DeletedChangeables.Contains(changeable.GetHashCode());
        }

        private static void InternalAcceptChanges(IChangeable changeable)
        {
            if (changeable != null)
            {
                foreach (var changeablePropertyInfo in PropertyInfos[changeable.GetType()].Values)
                    AcceptChangeableValue(changeablePropertyInfo, changeable);

                var hashCode = changeable.GetHashCode();
                if (IsNew(changeable)) NewChangeables.Remove(hashCode);
                if (IsChanged(changeable)) ChangedChangeables.Remove(hashCode);
            }
        }

        private static void InternalDelete(IChangeable changeable)
        {
            var hashCode = changeable.GetHashCode();
            if (IsNew(changeable)) NewChangeables.Remove(hashCode);
            if (IsChanged(changeable)) ChangedChangeables.Remove(hashCode);

            DeletedChangeables.Add(changeable.GetHashCode());
        }

        private static void CheckAttached(IChangeable changeable)
        {
            if (changeable == null) throw new ArgumentNullException("changeable");
            if (!Changeables.Contains(changeable.GetHashCode()))
                throw new ArgumentException("Instance not attached!");
        }

        private static void CheckDetached(IChangeable changeable)
        {
            if (changeable == null) throw new ArgumentNullException("changeable");
            if (Changeables.Contains(changeable.GetHashCode()))
                throw new ArgumentException("Instance already attached!");
        }

        private static void InternalRollback(IChangeable changeable)
        {
            if (changeable.ChangeableState() != ChangeState.Changed) return;

            foreach (var keyValuePair in ChangedChangeables[changeable.GetHashCode()].AllChanges)
                PropertyInfos[changeable.GetType()][keyValuePair.Key].SetValue(changeable, RollbackValidObject(keyValuePair.Value.OldValue));

            InternalAcceptChanges(changeable);
        }

        private static void AttachChangeableValue(ChangeablePropertyInfo changeablePropertyInfo, IChangeable changeable)
        {
            switch (changeablePropertyInfo.InfoType)
            {
                case ChangeablePropertyInfoType.Reference: InternalAttach(changeablePropertyInfo.GetReferenceValue(changeable)); break;
                case ChangeablePropertyInfoType.Association: ChangeableListUoW.InternalAttach(changeablePropertyInfo.GetAssociationValue(changeable)); break;
            }
        }

        private static void DetachChangeableValue(ChangeablePropertyInfo changeablePropertyInfo, IChangeable changeable)
        {
            switch (changeablePropertyInfo.InfoType)
            {
                case ChangeablePropertyInfoType.Reference: InternalDetach(changeablePropertyInfo.GetReferenceValue(changeable)); break;
                case ChangeablePropertyInfoType.Association: ChangeableListUoW.InternalDetach(changeablePropertyInfo.GetAssociationValue(changeable)); break;
            }
        }

        private static void AcceptChangeableValue(ChangeablePropertyInfo changeablePropertyInfo, IChangeable changeable)
        {
            switch (changeablePropertyInfo.InfoType)
            {
                case ChangeablePropertyInfoType.Reference: InternalAcceptChanges(changeablePropertyInfo.GetReferenceValue(changeable)); break;
                case ChangeablePropertyInfoType.Association: ChangeableListUoW.InternalAcceptChanges(changeablePropertyInfo.GetAssociationValue(changeable)); break;
            }
        }

        private static object RollbackValidObject(object obj)
        {
            if (obj is IChangeable)
                InternalRollback((IChangeable)obj);
            else if (obj is IChangeableList)
                ((IChangeableList)obj).RollbackChanges();

            return obj;
        }

        private static void CheckPropertyCache(IChangeable changeable)
        {
            var type = changeable.GetType();

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
            var changeable = (IChangeable)sender;
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
}
