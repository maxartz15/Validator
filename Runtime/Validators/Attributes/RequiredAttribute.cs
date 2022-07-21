using System;

namespace Validator
{
	[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	public class RequiredAttribute : Attribute
	{
		public WarningType WarningType { get; private set; } = WarningType.Error;
		public string Category { get; private set; } = ReportCategories.Design;

		public RequiredAttribute() { }

		public RequiredAttribute(WarningType warningType = WarningType.Error, string category = ReportCategories.Design)
		{
			WarningType = warningType;
			Category = category;
		}
	}
}