using System;

namespace Domain
{
    [Serializable]
    public class Weather
    {
        public int ZipCode { get; set; }
        public int Temperature { get; set; }
        public int RelativeHumidity { get; set; }
    }
}
