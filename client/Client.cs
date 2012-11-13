using System;
using System.IO;
using Common.interfaces;
using Domain;
using ProtoBuf.Meta;
using ZMQ;

namespace zmgClient
{
    internal class Client
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Collecting updates from weather server…");

            var typeModel = RuntimeTypeModel.Default;

            using (var context = new Context(1))
            {
                using (var subscriber = context.Socket(SocketType.SUB))
                {
                    subscriber.Subscribe(DomainTypes.Tag2Bytes(typeof(Weather)));
                    subscriber.Subscribe(DomainTypes.Tag2Bytes(typeof(City)));
                    subscriber.Connect("tcp://localhost:5556");

                    while (true)
                    {
                        var type = DomainTypes.Bytes2Type(subscriber.Recv());
                        var stream = new MemoryStream(subscriber.Recv());
                        var obj = typeModel.Deserialize(stream, null, type);

                        Console.WriteLine(string.Format("Received: {0}", ((IInfo)obj).ToInfo()));
                    }
                }
            }
        }
    }
}