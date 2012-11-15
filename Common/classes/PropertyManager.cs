using System;
using System.Collections.Generic;
using System.Reflection;
using Common.interfaces;

namespace Common.classes
{
    public class PropertyManager : IPropertyManager
    {
        private readonly IDictionary<Type, IDictionary<string, PropertyManagerInfo>> _propertyInfos = new Dictionary<Type, IDictionary<string, PropertyManagerInfo>>();

        #region PropertyManagerInfo

        private class PropertyManagerInfo : IPropertyManagerInfo
        {
            private readonly PropertyInfo _propertyInfo;
            private readonly PropertyManagerInfoType _infoType;

            public PropertyManagerInfo(PropertyInfo propertyInfo)
            {
                if (propertyInfo == null) throw new ArgumentNullException("propertyInfo");

                _propertyInfo = propertyInfo;
                if (typeof(IIdentifyable).IsAssignableFrom(_propertyInfo.PropertyType)) _infoType = PropertyManagerInfoType.Reference;
                //TODO else if (typeof(IChangeableList).IsAssignableFrom(PropertyInfo.PropertyType))  _infoType = PropertyManagerInfoType.Association;
                else _infoType = PropertyManagerInfoType.Field;
            }

            public string PropertyName { get { return _propertyInfo.Name; } }

            public PropertyManagerInfoType InfoType { get { return _infoType; } }

            public object GetValue(object obj)
            {
                return _propertyInfo.GetValue(obj, null);
            }

            public void SetValue(object obj, object value)
            {
                _propertyInfo.SetValue(obj, value, null);
            }

            public T GetReferenceValue<T>(object obj) where T : class
            {
                if (_infoType != PropertyManagerInfoType.Reference) throw new InvalidOperationException("This PropertyManagerInfo is not a Reference!");

                var t = _propertyInfo.GetValue(obj, null);
                if (t == null) return null;

                if (!(t is T)) throw new InvalidOperationException(string.Format("The value is not of the requested type ({0})!", typeof(T)));
                return (T)t;
            }

            //TODO
            /*
            public IChangeableList GetAssociationValue(IChangeTrackable changeTrackable)
            {
                if (InfoType != ChangeablePropertyInfoType.Association)
                    throw new InvalidOperationException("This ChangeablePropertyInfo is not a List!");
                return PropertyInfo.GetValue(changeTrackable, null) as IChangeableList;
            }
            */
        }
        #endregion

        public IEnumerable<IPropertyManagerInfo> GetInfos(object obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            return CheckPropertyCache(obj.GetType()).Values;
        }

        public IEnumerable<IPropertyManagerInfo> GetInfos(Type type)
        {
            return CheckPropertyCache(type).Values;
        }

        public IPropertyManagerInfo GetInfo(object obj, string propertyName)
        {
            return GetInfo(obj.GetType(), propertyName);
        }

        public IPropertyManagerInfo GetInfo(Type type, string propertyName)
        {
            return CheckPropertyCache(type)[propertyName];
        }

        public PropertyManagerInfoType GetInfoType(object obj, string propertyName)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            if (propertyName == null) throw new ArgumentNullException("propertyName");

            return CheckPropertyCache(obj)[propertyName].InfoType;
        }

        private IDictionary<string, PropertyManagerInfo> CheckPropertyCache(object obj)
        {
            return CheckPropertyCache(obj.GetType());
        }

        private IDictionary<string, PropertyManagerInfo> CheckPropertyCache(Type type)
        {
            IDictionary<string, PropertyManagerInfo> propertyManagerInfos;

            if (!_propertyInfos.TryGetValue(type, out propertyManagerInfos))
            {
                propertyManagerInfos = new Dictionary<string, PropertyManagerInfo>();
                foreach (var propertyInfo in type.GetProperties())
                    propertyManagerInfos.Add(propertyInfo.Name, new PropertyManagerInfo(propertyInfo));
                _propertyInfos.Add(type, propertyManagerInfos);
            }

            return propertyManagerInfos;
        }
    }
}
