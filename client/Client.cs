//
//  Weather update client
//  Connects SUB socket to tcp://localhost:5556
//  Collects weather updates and finds avg temp in zipcode
//

//  Author:     Michael Compton, Tomas Roos
//  Email:      michael.compton@littleedge.co.uk, ptomasroos@gmail.com

using System;
using System.IO;
using System.Text;
using Domain;
using ProtoBuf;
using ZMQ;

namespace zmgClient
{
    internal class Client
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Collecting updates from weather server…");

            // default zipcode is 10001
            var wantedZipcode = "10001 "; // the reason for having a space after 10001 is in case of the message would start with 100012 which we are not interested in

            if (args.Length > 0)
                wantedZipcode = args[1] + " ";

            using (var context = new Context(1))
            {
                using (var subscriber = context.Socket(SocketType.SUB))
                {
                    subscriber.Subscribe(wantedZipcode, Encoding.Unicode);
                    subscriber.Connect("tcp://localhost:5556");

                    const int updatesToCollect = 10;
                    var totalTemperature = 0;

                    for (var updateNumber = 0; updateNumber < updatesToCollect; updateNumber++)
                    {
                        var zipcode = subscriber.Recv(Encoding.Unicode);

                        var stream = new MemoryStream(subscriber.Recv());
                        var weather = Serializer.Deserialize<Weather>(stream);

                        totalTemperature += weather.Temperature;
                    }

                    Console.WriteLine("Average temperature for zipcode {0} was {1}F", wantedZipcode, totalTemperature / updatesToCollect);
                }
            }
        }
    }
}