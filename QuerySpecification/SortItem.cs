using System;
using System.Runtime.Serialization;

namespace QuerySpecification
{
    [DataContract]
    [Serializable]
    public class SortItem
    {
        [DataMember]
        public string PropertySelector { get; set; }

        [DataMember]
        public SortDirection SortDirection { get; set; }
    }
}