﻿using ProtoBuf;

namespace Domain
{
    [ProtoContract]
    public class City : DomainBase
    {
        private int _zipCode;
        private string _name;

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
    }
}