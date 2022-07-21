using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Validator.Editor
{
	public class ValidatableSceneValidator : IValidator
	{
		public string MenuName => nameof(ValidatableSceneValidator);

		public Report Validate()
		{
			Report report = new Report(nameof(ValidatableSceneValidator));

			List<IValidatable> objects = FindAllObjectsOfType<IValidatable>();
			for (int i = 0; i < objects.Count; i++)
			{
				EditorUtility.DisplayProgressBar("SceneValidator", "Validate...", (float)i / objects.Count);

				objects[i].Validate(report);
			}
			EditorUtility.ClearProgressBar();

			return report;
		}

		private static List<GameObject> GetAllRootGameObjects()
		{
			List<GameObject> gameObjects = new List<GameObject>();

			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				EditorUtility.DisplayProgressBar("SceneValidator", "GetAllRootGameObjects...", (float)i / SceneManager.sceneCount);
				gameObjects.AddRange(SceneManager.GetSceneAt(i).GetRootGameObjects());
			}
			EditorUtility.ClearProgressBar();

			return gameObjects;
		}

		public static List<T> FindAllObjectsOfType<T>()
		{
			List<T> objects = new List<T>();

			List<GameObject> gameObjects = GetAllRootGameObjects();
			for (int i = 0; i < gameObjects.Count; i++)
			{
				EditorUtility.DisplayProgressBar("SceneValidator", "FindAllObjectsOfType...", (float)i / gameObjects.Count);
				objects.AddRange(gameObjects[i].GetComponentsInChildren<T>(true));
			}
			EditorUtility.ClearProgressBar();

			return objects;
		}
	}
}