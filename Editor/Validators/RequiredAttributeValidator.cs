using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Validator.Editor
{
	public class RequiredAttributeAssetValidator : IValidator
	{
		public string MenuName => "Attributes/RequiredAttributeAssetValidator";

		public Report Validate()
		{
			Report report = new Report(nameof(RequiredAttributeAssetValidator));

			List<Object> objects = AssetValidator.FindAssetsByType<Object>();

			for (int i = 0; i < objects.Count; i++)
			{
				EditorUtility.DisplayProgressBar("RequiredAttributeAssetValidator", "RequiredAttribute...", (float)i / objects.Count);

				FieldInfo[] fields = objects[i].GetType().GetFields();

				for (int j = 0; j < fields.Length; j++)
				{
					object[] attr = fields[j].GetCustomAttributes(typeof(RequiredAttribute), true);

					if(attr.Length > 0 && attr[0] is RequiredAttribute requiredAttribute)
					{
						object o = fields[j].GetValue(objects[i]);

						if (o == null || o.Equals(null))
						{
							report.Log(objects[i], requiredAttribute.WarningType, requiredAttribute.Category, $"{fields[j].Name} is null", $"Assign {fields[j].FieldType}");
						}
					}
				}

				//IEnumerable<(FieldInfo FieldInfo, RequiredAttribute Attribute)> fieldsWithRequiredAttribute = from fi in objects[i].GetType().GetFields()
				//																							  let attr = fi.GetCustomAttributes(typeof(RequiredAttribute), true)
				//																							  where attr.Length == 1
				//																							  select (FieldInfo: fi, Attribute: attr.First() as RequiredAttribute);

				//foreach ((FieldInfo FieldInfo, RequiredAttribute Attribute) field in fieldsWithRequiredAttribute)
				//{
				//	object o = field.FieldInfo.GetValue(objects[i]);
				//	if (o == null || o.Equals(null))
				//	{
				//		report.Log(objects[i], field.Attribute.WarningType, field.Attribute.Category, $"{field.FieldInfo.Name} is null", $"Assign {field.FieldInfo.FieldType}");
				//	}
				//}
			}
			EditorUtility.ClearProgressBar();

			return report;
		}
	}

	public class RequiredAttributeSceneValidator : IValidator
	{
		public string MenuName => "Attributes/RequiredAttributeSceneValidator";

		public Report Validate()
		{
			Report report = new Report(nameof(RequiredAttributeSceneValidator));

			List<MonoBehaviour> objects = SceneValidator.FindAllObjectsOfType<MonoBehaviour>();

			for (int i = 0; i < objects.Count; i++)
			{
				EditorUtility.DisplayProgressBar("RequiredAttributeSceneValidator", "RequiredAttribute...", (float)i / objects.Count);
				IEnumerable<(FieldInfo FieldInfo, RequiredAttribute Attribute)> fieldsWithRequiredAttribute = from fi in objects[i].GetType().GetFields()
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