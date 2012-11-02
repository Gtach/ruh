using ProtoBuf;

namespace Domain
{
    [ProtoContract]
    public class Weather : DomainBase
    {
        private int _zipCode;
        private int _temperature;
        private int _relativeHumidity;

        [ProtoMember(1)]
        public int ZipCode
        {
            get { return _zipCode; }
            set { SetProperty(() => ZipCode, value, ref _zipCode); }
        }

        [ProtoMember(2)]
        public int Temperature
        {
            get { return _temperature; }
            set { SetProperty(() => Temperature, value, ref _temperature); }
        }

        [ProtoMember(3)]
        public int RelativeHumidity
        {
            get { return _relativeHumidity; }
            set { SetProperty(() => RelativeHumidity, value, ref _relativeHumidity); }
        }

    }
}
