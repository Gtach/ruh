using System;
using System.Collections.Generic;

namespace Common.interfaces
{
    public interface IRepository
    {
        T Get<T>(Guid id) where T : class, IIdentifyable;
        IEnumerable<T> GetAll<T>() where T : class, IIdentifyable;
        void Add<T>(T item) where T : class, IIdentifyable;
        void Update<T>(T item) where T : class, IIdentifyable;
        void Delete<T>(Guid id) where T : class, IIdentifyable;
    }
}
