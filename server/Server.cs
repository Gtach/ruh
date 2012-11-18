using System;
using System.Collections.Generic;
using System.IO;
using Common.classes;
using Common.interfaces;
using Domain;
using ProtoBuf.Meta;
using Repository;
using ZMQ;
using UnitOfWork;

namespace zmgServer
{
    internal class Server
    {
        public static void Main(string[] args)
        {
            IDictionary<int, City> cities = new Dictionary<int, City>();

            var domainMapper = new DomainMapper();
            var propertyManager = new PropertyManager();
            var repository = new ZmqRepository(domainMapper, propertyManager, @"C:\temp\data");
            IUnitOfWork unitOfWork = new ChangeTrackUoW(propertyManager, repository);

            var randomizer = new Random(DateTime.Now.Millisecond);

            var weather = new Weather { Temperature = randomizer.Next(-80, 135), RelativeHumidity = randomizer.Next(10, 60) };

            for (var i = 0; i < 3; i++)
            {
                var city = new City
                               {
                                   CitySize = CitySize.Medium,
                                   Name = "Test",
                                   ZipCode = randomizer.Next(1, 100000),
                                   Weather = weather
                               };

                unitOfWork.StartTransaction(city);

                try
                {
                    unitOfWork.Commit();
                }
                catch (System.Exception exception)
                {
                    unitOfWork.Rollback();
                    Console.WriteLine("Error: " + exception.Message);
                }
            }
        }

/*
        public static void Main(string[] args)
        {
            IDictionary<int, City> cities = new Dictionary<int, City>();

            var typeModel = RuntimeTypeModel.Default;
            IUnitOfWork unitOfWork = new ChangeTrackUoW(new ZmqRepository(@"C:\temp\data"));

            using (var context = new Context(1))
            {
                using (var publisher = context.Socket(SocketType.PUB))
                {
                    publisher.Bind("tcp://*:5556");

                    var randomizer = new Random(DateTime.Now.Millisecond);
                    while (true)
                    {
                        var zipCode = randomizer.Next(1, 100000);
                        var weather = new Weather { ZipCode = zipCode, Temperature = randomizer.Next(-80, 135), RelativeHumidity = randomizer.Next(10, 60) };

                        unitOfWork.StartTransaction(weather);

                        try
                        {
                            unitOfWork.Commit();
                        }
                        catch (System.Exception exception)
                        {
                            unitOfWork.Rollback();
                            Console.WriteLine("Error: " + exception.Message);
                        }

                        Thread.Sleep(100);
                    }
                }
            }
        }
*/
        private static City GetCity(IDictionary<int, City> cities, int zipCode)
        {
            City city;

            if (!cities.TryGetValue(zipCode, out city))
                cities.Add(zipCode, city = new City { ZipCode = zipCode, Name = "City " + zipCode });

            return city;
        }

        public static void Send(IDomainMapper domainMapper, Socket publisher, RuntimeTypeModel typeModel, MemoryStream stream, DomainBase instance)
        {
            var status = publisher.SendMore(domainMapper.Type2Bytes(instance.GetType()));
            if (status != SendStatus.Sent) throw new InvalidOperationException("Key not sent!");

            stream.SetLength(0);
            typeModel.Serialize(stream, instance);
            status = publisher.Send(stream.ToArray());
            if (status != SendStatus.Sent) throw new InvalidOperationException("Instance not sent!");
        }
    }
}