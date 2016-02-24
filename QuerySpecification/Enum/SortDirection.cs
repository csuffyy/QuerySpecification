using System;
using System.Runtime.Serialization;

namespace QuerySpecification
{
    [DataContract]
    [Serializable]
    public enum SortDirection
    {
        [EnumMember]
        Ascending = 1,

        [EnumMember]
        Descending = 2
    }
}