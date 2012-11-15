using ProtoBuf;

namespace Domain
{
    [ProtoContract]
    public class City : DomainBase
    {
        private int _zipCode;
        private string _name;
        private CitySize _citySize;
        private Weather _weather;

        [ProtoMember(1)]
        public int ZipCode
        {
            get { return _zipCode; }
            set { SetProperty(() => ZipCode, value, ref _zipCode); }
        }

        [ProtoMember(2)]
        public string Name
        {
            get { return _name; }
            set { SetProperty(() => Name, value, ref _name); }
        }

        [ProtoMember(3)]
        public CitySize CitySize
        {
            get { return _citySize; }
            set { SetProperty(() => CitySize, value, ref _citySize); }
        }

        public Weather Weather
        {
            get { return _weather; }
            set { SetProperty(() => Weather, value, ref _weather); }
        }

        public override string ToInfo(bool shortInfo)
        {
            return string.Format("{0}, Zip: {1}, Name: {2}, Size: {3}", base.ToInfo(shortInfo), ZipCode, Name, CitySize);
        }
    }
}
