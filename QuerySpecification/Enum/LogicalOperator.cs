using System;
using System.Runtime.Serialization;

namespace QuerySpecification
{
    [DataContract]
    [Serializable]
    public enum LogicalOperator
    {
        [EnumMember]
        And = 1,

        [EnumMember]
        Or = 2,

        [EnumMember]
        None = 3
    }
}