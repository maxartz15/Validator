using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Validator.Editor
{
	public class ValidatableAssetValidator : IValidator
	{
		public string MenuName => nameof(ValidatableAssetValidator);

		public Report Validate()
		{
			Report report = new Report(nameof(ValidatableAssetValidator));

			List<Object> objects = FindAssetsByType<Object>();
			for (int i = 0; i < objects.Count; i++)
			{
				EditorUtility.DisplayProgressBar("AssetValidator", "Validate...", (float)i / objects.Count);
				if (objects[i] is IValidatable validatable)
				{
					validatable.Validate(report);
				}
			}
			EditorUtility.ClearProgressBar();

			return report;
		}

		public static List<T> FindAssetsByType<T>() where T : Object
		{
			List<T> assets = new List<T>();
			string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)));

			for (int i = 0; i < guids.Length; i++)
			{
				EditorUtility.DisplayProgressBar("AssetValidator", "FindAssetsByType...", (float)i / guids.Length);

				string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
				T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
				if (asset != null)
				{
					assets.Add(asset);
				}
			}
			EditorUtility.ClearProgressBar();

			return assets;
		}
	}
}