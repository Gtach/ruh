using System;

namespace Common.interfaces
{
    public interface IDomainMapper
    {
        int Type2Tag(object obj);
        int Type2Tag(Type type);
        byte[] Type2Bytes(object obj);
        byte[] Type2Bytes(Type type);
        Type Tag2Type(int tag);
        Type Bytes2Type(byte[] bytes);
    }
}
