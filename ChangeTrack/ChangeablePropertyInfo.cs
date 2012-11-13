using System;
using System.Reflection;

namespace ChangeTrack
{
    public enum ChangeablePropertyInfoType
    {
        Field,
        Reference,
        Association
    }

    public class ChangeablePropertyInfo
    {
        public readonly PropertyInfo PropertyInfo;
        public readonly ChangeablePropertyInfoType InfoType;

        public ChangeablePropertyInfo(PropertyInfo propertyInfo)
        {
            PropertyInfo = propertyInfo;
            if (typeof(IChangeTrackable).IsAssignableFrom(PropertyInfo.PropertyType))
                InfoType = ChangeablePropertyInfoType.Reference;
            else if (typeof(IChangeableList).IsAssignableFrom(PropertyInfo.PropertyType))
                InfoType = ChangeablePropertyInfoType.Association;
            else
                InfoType = ChangeablePropertyInfoType.Field;
        }

        public object GetValue(IChangeTrackable changeTrackable)
        {
            return PropertyInfo.GetValue(changeTrackable, null);
        }

        public void SetValue(IChangeTrackable changeTrackable, object obj)
        {
            PropertyInfo.SetValue(changeTrackable, obj, null);
        }

        public IChangeTrackable GetReferenceValue(IChangeTrackable changeTrackable)
        {
            if (InfoType != ChangeablePropertyInfoType.Reference)
                throw new InvalidOperationException("This ChangeablePropertyInfo is not a Reference!");
            return PropertyInfo.GetValue(changeTrackable, null) as IChangeTrackable;
        }

        public IChangeableList GetAssociationValue(IChangeTrackable changeTrackable)
        {
            if (InfoType != ChangeablePropertyInfoType.Association)
                throw new InvalidOperationException("This ChangeablePropertyInfo is not a List!");
            return PropertyInfo.GetValue(changeTrackable, null) as IChangeableList;
        }
    }
}
