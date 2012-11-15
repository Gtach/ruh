using System;
using System.Collections.Generic;
using System.Linq;
using Common.interfaces;

namespace Domain
{
    public class DomainMapper : IDomainMapper
    {
        private readonly IDictionary<int, Type> _tagToType = new Dictionary<int, Type>
        {  
            {1, typeof(Weather)}, 
            {2, typeof(City)}
        };

        private readonly IDictionary<Type, int> _typeToTag;

        public DomainMapper()
        {
            _typeToTag = _tagToType.ToDictionary(pair => pair.Value, pair => pair.Key);
        }

        public byte[] Type2Bytes(object obj)
        {
            return Type2Bytes(obj.GetType());
        }

        public byte[] Type2Bytes(Type type)
        {
            return BitConverter.GetBytes(_typeToTag[type]);
        }

        public int Type2Tag(object obj)
        {
            return Type2Tag(obj.GetType());
        }

        public int Type2Tag(Type type)
        {
            return _typeToTag[type];
        }

        public Type Tag2Type(int tag)
        {
            return _tagToType[tag];
        }

        public Type Bytes2Type(byte[] bytes)
        {
            return _tagToType[BitConverter.ToInt32(bytes, 0)];
        }
    }
}
