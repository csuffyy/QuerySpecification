using System;
using System.Runtime.Serialization;

namespace QuerySpecification
{
    [DataContract]
    [Serializable]
    public class Condition
    {
        [DataMember]
        public string EntityTypeName { get; set; }

        [DataMember]
        public long Id { get; internal set; }

        [DataMember]
        public ConditionTree Tree { get; set; }
    }
}