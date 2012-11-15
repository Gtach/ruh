using System;
using System.Collections.Generic;
using Common.classes;

namespace Common.interfaces
{
    public interface IPropertyManager
    {
        IEnumerable<IPropertyManagerInfo> GetInfos(object obj);
        IEnumerable<IPropertyManagerInfo> GetInfos(Type type);
        IPropertyManagerInfo GetInfo(Type type, string propertyName);
        IPropertyManagerInfo GetInfo(object obj, string propertyName);
        PropertyManagerInfoType GetInfoType(object obj, string propertyName);
    }
}
