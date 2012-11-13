using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Common.interfaces;
using ProtoBuf.Meta;

namespace Repository
{
    public class ZmqRepository : IRepository
    {
        private readonly string _path;
        private readonly RuntimeTypeModel _typeModel = RuntimeTypeModel.Default;

        public ZmqRepository(string path)
        {
            if (path == null) throw new ArgumentNullException("path");
            _path = path;
        }

        public T Get<T>(Guid id) where T : class, IIdentifyable
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> GetAll<T>() where T : class, IIdentifyable
        {
            throw new NotImplementedException();
        }

        public void Add<T>(T item) where T : class, IIdentifyable
        {
            SerializeItem(item);
        }

        public void Update<T>(T item) where T : class, IIdentifyable
        {
            SerializeItem(item);
        }

        public void Delete<T>(Guid id) where T : class, IIdentifyable
        {
            throw new NotImplementedException();
        }

        private void SerializeItem(IIdentifyable item)
        {
            var fullPath = _path + @"\" + GetName(item);
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                _typeModel.Serialize(stream, item);
                stream.Close();
            }
        }

        private static string GetName(IIdentifyable item)
        {
            var stringBuilder = new StringBuilder(item.GetType().FullName);
            stringBuilder.Append("-");
            stringBuilder.Append(item.Id);
            stringBuilder.Append(".bin");

            return stringBuilder.ToString();
        }
    }
}
