using System;

namespace QuerySpecification
{
	[AttributeUsage(AttributeTargets.Property)]
	public class NavigationPropertyAttribute : Attribute
	{
		public NavigationPropertyAttribute()
		{
		}
	}
}