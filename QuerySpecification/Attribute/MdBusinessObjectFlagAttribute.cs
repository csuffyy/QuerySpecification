using System;

namespace QuerySpecification
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MdBusinessObjectFlagAttribute : Attribute
    {
        public Type EntityType { get; set; }
    }
}