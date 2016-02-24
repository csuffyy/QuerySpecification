using System;

namespace QuerySpecification
{
	[AttributeUsage(AttributeTargets.Property)]
	public class PrimaryKeyAttribute : Attribute
	{
		public PrimaryKeyAttribute()
		{
		}
	}
}