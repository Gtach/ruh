//
//  Weather update client
//  Connects SUB socket to tcp://localhost:5556
//  Collects weather updates and finds avg temp in zipcode
//

//  Author:     Michael Compton, Tomas Roos
//  Email:      michael.compton@littleedge.co.uk, ptomasroos@gmail.com

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Domain;
using ProtoBuf;
using ProtoBuf.Meta;
using ZMQ;

namespace zmgClient
{
    internal class Client
    {
        IDictionary<int, City> cities = new Dictionary<int, City>();

        public static void Main(string[] args)
        {
            Console.WriteLine("Collecting updates from weather server…");

            var typeModel = RuntimeTypeModel.Default;

            
            using (var context = new Context(1))
            {
                using (var subscriber = context.Socket(SocketType.SUB))
                {
                    subscriber.Subscribe(typeof(Weather).GUID.ToByteArray());
                    //subscriber.Subscribe(typeof(City).GUID.ToByteArray());
                    subscriber.Connect("tcp://localhost:5556");

                    while (true)
                    {
                        var guid = new Guid(subscriber.Recv());

                        var stream = new MemoryStream(subscriber.Recv());
                        var obj = typeModel.DeserializeWithLengthPrefix(stream, null, null, PrefixStyle.Base128, 0, key => DomainTypes.TagToType[key]);

                        Console.WriteLine(string.Format("Received: {0}", obj));

                    }
                }
            }
        }
    }
}