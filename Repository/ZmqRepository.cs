using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Common.interfaces;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Repository
{
    #region ItemContainer

    [ProtoContract]
    public class ItemContainer : Extensible
    {
        [ProtoMember(1, AsReference = true, DynamicType = true)]
        public IIdentifyable Item { get; set; }
    }

    #endregion

    public class ZmqRepository : IRepository
    {
        private const char FileNameSeparator = ' ';
        private const string FileNameExtension = ".bin";

        private readonly IDomainMapper _domainMapper;
        private readonly IPropertyManager _propertyManager;
        private readonly string _path;
        private readonly RuntimeTypeModel _typeModel = RuntimeTypeModel.Default;

        private IDictionary<Type, IDictionary<Guid, IIdentifyable>> _itemCache;

        public ZmqRepository(IDomainMapper domainMapper, IPropertyManager propertyManager, string path)
        {
            if (domainMapper == null) throw new ArgumentNullException("domainMapper");
            if (propertyManager == null) throw new ArgumentNullException("propertyManager");
            if (path == null) throw new ArgumentNullException("path");

            _domainMapper = domainMapper;
            _propertyManager = propertyManager;
            _path = path;
        }

        public T Get<T>(Guid id) where T : class, IIdentifyable
        {
            CheckItemCache();

            var type = typeof(T);
            
            var item = GetItem(type, id) as T;
            if (item == null) throw new InvalidOperationException(string.Format("No item of type {0} found for id {1}", type, id));

            return item;
        }

        public IEnumerable<T> GetAll<T>() where T : class, IIdentifyable
        {
            CheckItemCache();
            
            var type = typeof(T);
            var list = new List<T>();
            
            IDictionary<Guid, IIdentifyable> typeCache;
            if (_itemCache.TryGetValue(type, out typeCache)) list.AddRange(typeCache.Values.Cast<T>());

            return list;
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

        private void CheckItemCache()
        {
            if (_itemCache != null) return;

            _itemCache = new Dictionary<Type, IDictionary<Guid, IIdentifyable>>();

            foreach (var file in Directory.EnumerateFiles(_path, "*" + FileNameExtension).Where(file => GetItemFromCache(file) == null))
                AddToItemCache(DeserializeItem(file));
        }

        private void SerializeItem(IIdentifyable item)
        {
            var filePath = GetFilePath(item); 
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                var itemContainer = new ItemContainer {Item = item};
                
                AppendReferences(itemContainer);
                
                Serializer.Serialize(stream, itemContainer);
            }
        }

        private IIdentifyable DeserializeItem(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                var itemContainer = Serializer.Deserialize<ItemContainer>(stream); 

                if (itemContainer == null) return null; //TODO

                GetReferences(stream, itemContainer);

                return itemContainer.Item as IIdentifyable;
            }
        }

        private void GetReferences(FileStream stream, ItemContainer itemContainer)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (itemContainer == null) throw new ArgumentNullException("itemContainer");

            var idx = 2;
            foreach (var info in _propertyManager.GetInfos(itemContainer.Item).Where(x => x.InfoType == PropertyManagerInfoType.Reference))
            {
                var type = _domainMapper.Tag2Type(Extensible.GetValue<int>(itemContainer, idx++));
                var guid = Extensible.GetValue<Guid>(itemContainer, idx++);
                info.SetValue(itemContainer.Item, GetItem(type, guid));
            }
        }

        private void AppendReferences(ItemContainer itemContainer)
        {
            if (itemContainer == null) throw new ArgumentNullException("itemContainer");

            var idx = 2;
            foreach (var reference in _propertyManager.GetInfos(itemContainer.Item).Where(x => x.InfoType == PropertyManagerInfoType.Reference).Select(info => info.GetReferenceValue<IIdentifyable>(itemContainer.Item)))
            {
                var tag = _domainMapper.Type2Tag(reference);
                Extensible.AppendValue(itemContainer, idx++, tag);
                Extensible.AppendValue(itemContainer, idx++, reference.Id);
            }
        }

        private void AddToItemCache(IIdentifyable item)
        {
            var type = item.GetType();
            
            IDictionary<Guid, IIdentifyable> typeCache;
            if (!_itemCache.TryGetValue(type, out typeCache))
            {
                typeCache = new Dictionary<Guid, IIdentifyable>();
                _itemCache.Add(type, typeCache);
            }

            typeCache.Add(item.Id, item);
        }

        private IIdentifyable GetItem(Type type, Guid guid)
        {
            var item = GetItemFromCache(type, guid);
            if (item != null) return item;

            var filePath = GetFilePath(type, guid);

            item = DeserializeItem(filePath);
            AddToItemCache(item);

            return item;
        }

        private IIdentifyable GetItemFromCache(string filePath)
        {
            if (filePath == null) throw new ArgumentNullException("filePath");

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            if (fileName == null) throw new ArgumentException(string.Format("Path '{0}' does not contain a valid file name", filePath));

            var parts = fileName.Split(FileNameSeparator);
            if (parts.Length != 2) throw new InvalidOperationException(string.Format("Filename {0} is not a type-guid combination!", fileName));

            var type = Type.GetType(parts[0]);
            var guid = Guid.Parse(parts[1]);

            return GetItemFromCache(type, guid);
        }

        private IIdentifyable GetItemFromCache(Type type, Guid guid)
        {
            IDictionary<Guid, IIdentifyable> typeCache;
            IIdentifyable item;

            if (!_itemCache.TryGetValue(type, out typeCache)) return null;
            return !typeCache.TryGetValue(guid, out item) ? null : item;
        }

        private string GetFilePath(Type type, Guid guid)
        {
            return _path + @"\" + GetFileName(type, guid);
        }

        private string GetFilePath(IIdentifyable item)
        {
            return _path + @"\" + GetFileName(item);
        }

        private static string GetFileName(IIdentifyable item)
        {
            return GetFileName(item.GetType(), item.Id);
        }

        private static string GetFileName(Type type, Guid guid)
        {
            var stringBuilder = new StringBuilder(type.FullName);
            stringBuilder.Append(',');
            stringBuilder.Append(type.Assembly.GetName().Name);
            stringBuilder.Append(FileNameSeparator);
            stringBuilder.Append(guid);
            stringBuilder.Append(FileNameExtension);

            return stringBuilder.ToString();
        }
    }
}
