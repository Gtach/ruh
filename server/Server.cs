//
//  Weather update server
//  Binds PUB socket to tcp://*:5556
//  Publishes random weather updates
//

//  Author:     Michael Compton, Tomas Roos
//  Email:      michael.compton@littleedge.co.uk, ptomasroos@gmail.com

using System;
using System.IO;
using System.Text;
using Domain;
using ProtoBuf;
using ZMQ;

namespace zmgServer
{
    internal class Server
    {
        public static void Main(string[] args)
        {
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
                            //  Get values that will fool the boss
                            var weather = new Weather{ZipCode = randomizer.Next(0, 100000), Temperature = randomizer.Next(-80, 135), RelativeHumidity = randomizer.Next(10, 60) };

                            stream.SetLength(0);
                            Serializer.Serialize(stream, weather);
                            stream.Seek(0, SeekOrigin.Begin);

                            //  Send message to 0..N subscribers via a pub socket
                            var status = publisher.SendMore(weather.ZipCode + " ", Encoding.Unicode);
                            if (status == SendStatus.Sent) status = publisher.Send(stream.ToArray());
                            if (status != SendStatus.Sent) throw new InvalidOperationException("Not sent!");
                        }
                    }
                }
            }
        }
    }
}