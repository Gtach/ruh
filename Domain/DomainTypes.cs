using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain
{
    public static class DomainTypes
    {
        public static IDictionary<int, Type> TagToType = new Dictionary<int, Type>
        {  
            {1, typeof(Weather)}, 
            {2, typeof(City)}
        };

        public static IDictionary<Type, int> TypeToTag = TagToType.ToDictionary(pair => pair.Value, pair => pair.Key);

        public static byte[] Tag2Bytes(Type type)
        {
            return BitConverter.GetBytes(DomainTypes.TypeToTag[type]);
        }

        public static Type Bytes2Type(byte[] bytes)
        {
            return TagToType[BitConverter.ToInt32(bytes, 0)];
        }
    }
}
