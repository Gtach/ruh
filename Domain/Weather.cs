using ProtoBuf;

namespace Domain
{
    [ProtoContract]
    public class Weather : DomainBase
    {
        private int _temperature;
        private int _relativeHumidity;

        [ProtoMember(1)]
        public int Temperature
        {
            get { return _temperature; }
            set { SetProperty(() => Temperature, value, ref _temperature); }
        }

        [ProtoMember(2)]
        public int RelativeHumidity
        {
            get { return _relativeHumidity; }
            set { SetProperty(() => RelativeHumidity, value, ref _relativeHumidity); }
        }

        public override string ToInfo(bool shortInfo)
        {
            return string.Format("{0}, Temperature: {1}, RelativeHumidity: {2}", base.ToInfo(shortInfo), Temperature, RelativeHumidity);
        }
    }
}
