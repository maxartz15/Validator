using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Validator.Editor
{
	public class RequiredAttributeAssetValidator : IValidator
	{
		public string MenuName => nameof(RequiredAttributeAssetValidator);

		private const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

		public Report Validate()
		{
			Report report = new Report(nameof(RequiredAttributeAssetValidator));

			List<Object> objects = ValidatableAssetValidator.FindAssetsByType<Object>();

			for (int i = 0; i < objects.Count; i++)
			{
				EditorUtility.DisplayProgressBar("RequiredAttributeAssetValidator", "RequiredAttribute...", (float)i / objects.Count);

				IEnumerable<(FieldInfo FieldInfo, RequiredAttribute Attribute)> fieldsWithRequiredAttribute = from fi in objects[i].GetType().GetFields(flags)
																											  let attr = fi.GetCustomAttributes(typeof(RequiredAttribute), true)
																											  where attr.Length == 1
																											  select (FieldInfo: fi, Attribute: attr.First() as RequiredAttribute);

				foreach ((FieldInfo FieldInfo, RequiredAttribute Attribute) field in fieldsWithRequiredAttribute)
				{
					object o = field.FieldInfo.GetValue(objects[i]);
					if (o == null || o.Equals(null))
					{
						report.Log(objects[i], field.Attribute.WarningType, field.Attribute.Category, $"{field.FieldInfo.Name} is null", $"Assign {field.FieldInfo.FieldType}");
					}
				}
			}
			EditorUtility.ClearProgressBar();

			return report;
		}
	}

	public class RequiredAttributeSceneValidator : IValidator
	{
		public string MenuName => nameof(RequiredAttributeSceneValidator);

		private const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

		public Report Validate()
		{
			Report report = new Report(nameof(RequiredAttributeSceneValidator));

			List<MonoBehaviour> objects = ValidatableSceneValidator.FindAllObjectsOfType<MonoBehaviour>();

			for (int i = 0; i < objects.Count; i++)
			{
				EditorUtility.DisplayProgressBar("RequiredAttributeSceneValidator", "RequiredAttribute...", (float)i / objects.Count);
				IEnumerable<(FieldInfo FieldInfo, RequiredAttribute Attribute)> fieldsWithRequiredAttribute = from fi in objects[i].GetType().GetFields(flags)
																											  let attr = fi.GetCustomAttributes(typeof(RequiredAttribute), true)
																											  where attr.Length == 1
																											  select (FieldInfo: fi, Attribute: attr.First() as RequiredAttribute);

				foreach ((FieldInfo FieldInfo, RequiredAttribute Attribute) field in fieldsWithRequiredAttribute)
				{
					object o = field.FieldInfo.GetValue(objects[i]);
					if (o == null || o.Equals(null))
					{
						report.Log(objects[i], field.Attribute.WarningType, field.Attribute.Category, $"{field.FieldInfo.Name} is null", $"Assign {field.FieldInfo.FieldType}");
					}
				}
			}
			EditorUtility.ClearProgressBar();

			return report;
		}
	}
}