using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace QuerySpecification
{
    [DataContract(IsReference = true)]
    [Serializable]
    public class ConditionTree
    {
        private object val;

        [DataMember]
        public List<ConditionTree> Children { get; set; }

        [DataMember]
        public long Id { get; set; }

        [DataMember]
        public LogicalOperator NextLogicalOperator { get; set; }

        [DataMember]
        public Operator OperationType { get; set; }

        [DataMember]
        public string SelectorString { get; set; }

        [DataMember]
        public string SerializedValue { get; set; }

        public object Value
        {
            get
            {
                return val;
            }
            set
            {
                val = value;
                object obj = val;
                JsonSerializerSettings jsonSerializerSetting = new JsonSerializerSettings()
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects
                };
                SerializedValue = JsonConvert.SerializeObject(obj, jsonSerializerSetting).ToString(CultureInfo.InvariantCulture);
            }
        }

        public ConditionTree()
        {
            Children = new List<ConditionTree>();
        }
    }
}