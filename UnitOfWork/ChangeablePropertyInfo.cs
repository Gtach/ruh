using System;
using System.Reflection;

namespace UnitOfWork
{
    internal enum ChangeablePropertyInfoType
    {
        Field,
        Reference,
        Association
    }

    internal class ChangeablePropertyInfo
    {
        public readonly PropertyInfo PropertyInfo;
        public readonly ChangeablePropertyInfoType InfoType;

        public ChangeablePropertyInfo(PropertyInfo propertyInfo)
        {
            PropertyInfo = propertyInfo;
            if (typeof(IChangeable).IsAssignableFrom(PropertyInfo.PropertyType))
                InfoType = ChangeablePropertyInfoType.Reference;
            else if (typeof(IChangeableList).IsAssignableFrom(PropertyInfo.PropertyType))
                InfoType = ChangeablePropertyInfoType.Association;
            else
                InfoType = ChangeablePropertyInfoType.Field;
        }

        public object GetValue(IChangeable changeable)
        {
            return PropertyInfo.GetValue(changeable, null);
        }

        public void SetValue(IChangeable changeable, object obj)
        {
            PropertyInfo.SetValue(changeable, obj, null);
        }

        public IChangeable GetReferenceValue(IChangeable changeable)
        {
            if (InfoType != ChangeablePropertyInfoType.Reference)
                throw new InvalidOperationException("This ChangeablePropertyInfo is not a Reference!");
            return PropertyInfo.GetValue(changeable, null) as IChangeable;
        }

        public IChangeableList GetAssociationValue(IChangeable changeable)
        {
            if (InfoType != ChangeablePropertyInfoType.Association)
                throw new InvalidOperationException("This ChangeablePropertyInfo is not a List!");
            return PropertyInfo.GetValue(changeable, null) as IChangeableList;
        }
    }
}
