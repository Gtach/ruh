using System;
using System.IO;
using Common.classes;
using Common.interfaces;
using Domain;
using ProtoBuf.Meta;
using Repository;
using ZMQ;

namespace zmgClient
{
    internal class Client
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Collecting updates from weather server…");

            var domainMapper = new DomainMapper();
            var propertyManager = new PropertyManager();
            var repository = new ZmqRepository(domainMapper, propertyManager, @"C:\temp\data");

            var cities = repository.GetAll<City>();

            foreach (var city in cities)
            {
                Console.WriteLine(string.Format("Received: {0}, Weather: {1}", city.ToInfo(), city.Weather.ToInfo()));
            }
        }

        /*
        public static void Main(string[] args)
        {
            Console.WriteLine("Collecting updates from weather server…");

            var domainMapper = new DomainMapper();
            var propertyManager = new PropertyManager();
            var repository = new ZmqRepository(domainMapper, propertyManager, @"C:\temp\data");
            var typeModel = RuntimeTypeModel.Default;

            using (var context = new Context(1))
            {
                using (var subscriber = context.Socket(SocketType.SUB))
                {
                    subscriber.Subscribe(domainMapper.Type2Bytes(typeof(Weather)));
                    subscriber.Subscribe(domainMapper.Type2Bytes(typeof(City)));
                    subscriber.Connect("tcp://localhost:5556");

                    while (true)
                    {
                        var type = domainMapper.Bytes2Type(subscriber.Recv());
                        var stream = new MemoryStream(subscriber.Recv());
                        var obj = typeModel.Deserialize(stream, null, type);

                        Console.WriteLine(string.Format("Received: {0}", ((IInfo)obj).ToInfo()));
                    }
                }
            }
        }
        */
    }
}