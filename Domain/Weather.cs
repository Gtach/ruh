using System;
using ProtoBuf;

namespace Domain
{
    [ProtoContract]
    public class Weather
    {
        [ProtoMember(1)]
        public int ZipCode { get; set; }
        [ProtoMember(2)]
        public int Temperature { get; set; }
        [ProtoMember(3)]
        public int RelativeHumidity { get; set; }
    }
}
