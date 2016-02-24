using System;

namespace QuerySpecification
{
	[AttributeUsage(AttributeTargets.Class)]
	public class EntityFlagAttribute : Attribute
	{
		public EntityFlagAttribute()
		{
		}
	}
}