//
//  Weather update server
//  Binds PUB socket to tcp://*:5556
//  Publishes random weather updates
//

//  Author:     Michael Compton, Tomas Roos
//  Email:      michael.compton@littleedge.co.uk, ptomasroos@gmail.com

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Domain;
using ProtoBuf;
using ProtoBuf.Meta;
using ZMQ;

namespace zmgServer
{
    internal class Server
    {
        public static void Main(string[] args)
        {
            IDictionary<int, City> cities = new Dictionary<int, City>();
            
            var typeModel = RuntimeTypeModel.Default;

            using (var context = new Context(1))
            {
                using (var publisher = context.Socket(SocketType.PUB))
                {
                    publisher.Bind("tcp://*:5556");

                    var randomizer = new Random(DateTime.Now.Millisecond);
                    using (var stream = new MemoryStream())
                    {
                        while (true)
                        {
                            var zipCode = randomizer.Next(1, 100000);
                            var city = GetCity(cities, zipCode);

                            var weather = new Weather { ZipCode = zipCode, Temperature = randomizer.Next(-80, 135), RelativeHumidity = randomizer.Next(10, 60) };

                            Send(publisher, typeModel, stream, city);
                            Send(publisher, typeModel, stream, weather);

                            Thread.Sleep(100);
                        }
                    }
                }
            }
        }

        private static City GetCity(IDictionary<int, City> cities, int zipCode)
        {
            City city;

            if (!cities.TryGetValue(zipCode, out city))
                cities.Add(zipCode, city = new City { ZipCode = zipCode, Name = "City " + zipCode });

            return city;
        }

        public static void Send(Socket publisher, RuntimeTypeModel typeModel, MemoryStream stream, DomainBase instance)
        {
            var status = publisher.SendMore(instance.GetType().GUID.ToByteArray());
            if (status != SendStatus.Sent) throw new InvalidOperationException("Key not sent!");

            stream.SetLength(0);
            typeModel.SerializeWithLengthPrefix(stream, instance, null, PrefixStyle.Base128, DomainTypes.TypeToTag[instance.GetType()]);
            status = publisher.Send(stream.ToArray());
            if (status != SendStatus.Sent) throw new InvalidOperationException("Instance not sent!");
        }
/*
        public static void Send(Socket publisher, MemoryStream stream, DomainBase instance)
        {
            var status = publisher.SendMore(instance.GetType().GUID.ToByteArray());
            if (status != SendStatus.Sent) throw new InvalidOperationException("Key not sent!");

            stream.SetLength(0);
            Serializer.Serialize(stream, instance);
            status = publisher.Send(stream.ToArray());
            if (status != SendStatus.Sent) throw new InvalidOperationException("Instance not sent!");
        }
 */ 
    }
}