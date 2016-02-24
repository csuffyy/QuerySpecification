using System;
using System.Runtime.Serialization;

namespace QuerySpecification
{
    [DataContract]
    [Serializable]
    public enum TrueFalse
    {
        [EnumMember]
        True = 1,

        [EnumMember]
        False = 2
    }
}