using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using Object = UnityEngine.Object;

namespace Validator.Editor
{
	public class ValidatorEditorWindow : EditorWindow
	{
		private class ValidatorInfo
		{
			public IValidator validator = null;
			public bool isEnabled = true;

			public ValidatorInfo(IValidator validator)
			{
				this.validator = validator;
			}
		}

		private class ReportStats
		{
			public int infoCount = 0;
			public int warningCount = 0;
			public int errorCount = 0;
		}

		private class Settings
		{
			public bool showInfo = true;
			public bool showWarning = true;
			public bool showError = true;
			public string searchInput = "";

			public readonly Color lightColor = new Color(0.25f, 0.25f, 0.25f, 1);
			public readonly Color darkColor = new Color(0.22f, 0.22f, 0.22f, 1);
		}

		private static readonly List<ValidatorInfo> validators = new List<ValidatorInfo>();
		private static readonly List<Report> reports = new List<Report>();

		private static readonly Settings settings = new Settings();
		private static ReportStats reportStats = new ReportStats();

		private MultiColumnHeaderState multiColumnHeaderState;
		private MultiColumnHeader multiColumnHeader;
		private MultiColumnHeaderState.Column[] columns;
		private Vector2 scrollPosition;
		private float multiColumnHeaderWidth;
		private float rows;

		[MenuItem("Window/General/Validator")]
		private static void Init()
		{
			ValidatorEditorWindow window = (ValidatorEditorWindow)GetWindow(typeof(ValidatorEditorWindow));
			window.titleContent = new GUIContent("Validator", EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow").image);
			window.Show();
			window.LoadValidators();
		}

		private void OnGUI()
		{
			using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
			{
				DrawMenu();
				GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
				DrawMultiColumnScope();
			}
		}

		private void InitializeMultiColumn()
		{
			columns = new MultiColumnHeaderState.Column[]
			{
				new MultiColumnHeaderState.Column()
				{
					allowToggleVisibility = false, // At least one column must be there.
					autoResize = false,
					width = 36f,
					minWidth = 36f,
					maxWidth = 36f,
					canSort = false,
					sortingArrowAlignment = TextAlignment.Right,
					headerContent = EditorGUIUtility.IconContent("d_console.erroricon.inactive.sml", "Warning Type."),
					headerTextAlignment = TextAlignment.Center,
				},
				new MultiColumnHeaderState.Column()
				{
					allowToggleVisibility = true,
					autoResize = false,
					width = 36f,
					minWidth = 36f,
					maxWidth = 36f,
					canSort = false,
					sortingArrowAlignment = TextAlignment.Right,
					headerContent = EditorGUIUtility.IconContent("Animation.FilterBySelection", "Target Objects"),
					headerTextAlignment = TextAlignment.Center,
				},
				new MultiColumnHeaderState.Column()
				{
					allowToggleVisibility = true,
					autoResize = true,
					width = 75.0f,
					minWidth = 75.0f,
					canSort = false,
					sortingArrowAlignment = TextAlignment.Right,
					headerContent = new GUIContent("Category", "Warning Category."),
					headerTextAlignment = TextAlignment.Center,
				},
				new MultiColumnHeaderState.Column()
				{
					allowToggleVisibility = true,
					autoResize = true,
					width = 200.0f,
					minWidth = 200.0f,
					canSort = false,
					sortingArrowAlignment = TextAlignment.Right,
					headerContent = new GUIContent("Message", "Warning Message."),
					headerTextAlignment = TextAlignment.Center,
				},
				new MultiColumnHeaderState.Column()
				{
					allowToggleVisibility = true,
					autoResize = true,
					width = 200.0f,
					minWidth = 200.0f,
					canSort = false,
					sortingArrowAlignment = TextAlignment.Right,
					headerContent = new GUIContent("Solution", "Warning Solution."),
					headerTextAlignment = TextAlignment.Center,
				},
			};

			multiColumnHeaderState = new MultiColumnHeaderState(columns);
			multiColumnHeader = new MultiColumnHeader(multiColumnHeaderState);
			multiColumnHeader.ResizeToFit();
		}

		private void DrawMenu()
		{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				if (GUILayout.Button(EditorGUIUtility.IconContent("Refresh", "Refresh"), EditorStyles.toolbarButton))
				{
					LoadValidators();
				}

				if (GUILayout.Button(EditorGUIUtility.IconContent("d_Settings", "Select validator"), EditorStyles.toolbarButton))
				{
					GenericMenu validatorOptionsMenu = new GenericMenu();
					validatorOptionsMenu.AddItem(new GUIContent($"All"), false, OnValidatorInfoVisibilityAllEvent);
					validatorOptionsMenu.AddItem(new GUIContent($"None"), false, OnValidatorInfoVisibilityNoneEvent);
					validatorOptionsMenu.AddSeparator("");
					foreach (ValidatorInfo validatorInfo in validators)
					{
						validatorOptionsMenu.AddItem(new GUIContent($"{validatorInfo.validator.MenuName}"), validatorInfo.isEnabled, OnValidatorInfoVisibilityChangedEvent, validatorInfo);
					}
					validatorOptionsMenu.ShowAsContext();
				}

				if (GUILayout.Button(EditorGUIUtility.IconContent("PlayButton", "Run all"), EditorStyles.toolbarButton))
				{
					RunValidators();
					UpdateStats();
				}

				GUILayout.FlexibleSpace();

				settings.searchInput = GUILayout.TextField(settings.searchInput, EditorStyles.toolbarSearchField, GUILayout.MaxWidth(300));

				if(reportStats != null)
				{
					settings.showInfo = GUILayout.Toggle(settings.showInfo, new GUIContent($"{reportStats.infoCount}", EditorGUIUtility.IconContent(GetWarningIconName(WarningType.Info)).image), EditorStyles.toolbarButton);
					settings.showWarning =  GUILayout.Toggle(settings.showWarning, new GUIContent($"{reportStats.warningCount}", EditorGUIUtility.IconContent(GetWarningIconName(WarningType.Warning)).image), EditorStyles.toolbarButton);
					settings.showError = GUILayout.Toggle(settings.showError, new GUIContent($"{reportStats.errorCount}", EditorGUIUtility.IconContent(GetWarningIconName(WarningType.Error)).image), EditorStyles.toolbarButton);
				}
			}
		}

		private void DrawMultiColumnScope()
		{
			//GUILayout.FlexibleSpace(); // If nothing has been drawn yet, uncomment this because GUILayoutUtility.GetLastRect() needs it.
			Rect windowRect = GUILayoutUtility.GetLastRect();
			windowRect.width = position.width;
			windowRect.height = position.height;

			if (multiColumnHeader == null)
			{
				InitializeMultiColumn();
			}

			float columnHeight = EditorGUIUtility.singleLineHeight * 2;

			Rect headerRect = new Rect(windowRect)
			{
				height = EditorGUIUtility.singleLineHeight,
			};

			Rect scrollViewPositionRect = GUILayoutUtility.GetRect(0, float.MaxValue, 0, float.MaxValue);
			scrollViewPositionRect.y += headerRect.height - EditorGUIUtility.standardVerticalSpacing;
			scrollViewPositionRect.height -= headerRect.height - EditorGUIUtility.standardVerticalSpacing;

			Rect scrollViewRect = new Rect(windowRect)
			{
				width = multiColumnHeaderState.widthOfAllVisibleColumns,
				height = rows * columnHeight
			};

			Rect rowRect = new Rect(windowRect)
			{
				width = multiColumnHeaderWidth,
				height = columnHeight,
			};

			// Draw column header.
			multiColumnHeader.OnGUI(headerRect, scrollPosition.x);

			// Draw scroll view.
			using (GUI.ScrollViewScope scope = new GUI.ScrollViewScope(scrollViewPositionRect, scrollPosition, scrollViewRect, false, false))
			{
				scrollPosition = scope.scrollPosition;
				multiColumnHeaderWidth = Mathf.Max(scrollViewPositionRect.width + scrollPosition.x, multiColumnHeaderWidth);

				rows = 0;
				for (int i = 0; i < reports.Count; i++)
				{
					for (int j = 0; j < reports[i].Reports.Count; j++)
					{
						// Type filter.
						switch (reports[i].Reports[j].WarningType)
						{
							case WarningType.Info:
								if (!settings.showInfo)
									continue;
								break;
							case WarningType.Warning:
								if (!settings.showWarning)
									continue;
								break;
							case WarningType.Error:
								if (!settings.showError)
									continue;
								break;
							default:
								break;
						}

						// Search filter.
						if (!string.IsNullOrWhiteSpace(settings.searchInput))
						{
							if (!reports[i].Reports[j].Category.Contains(settings.searchInput) && !reports[i].Reports[j].Message.Contains(settings.searchInput) && !reports[i].Reports[j].Solution.Contains(settings.searchInput))
							{
								continue;
							}
						}

						// Only draw what is visible within the view.
						if (rowRect.yMax > windowRect.y + scrollPosition.y && rowRect.yMin < windowRect.height + scrollPosition.y)
						{
							if (rows % 2 == 0)
							{
								EditorGUI.DrawRect(rowRect, settings.darkColor);
							}
							else
							{
								EditorGUI.DrawRect(rowRect, settings.lightColor);
							}

							// Warning Field.
							int columnIndex = 0;
							if (multiColumnHeader.IsColumnVisible(columnIndex))
							{
								int visibleColumnIndex = multiColumnHeader.GetVisibleColumnIndex(columnIndex);
								Rect columnRect = multiColumnHeader.GetColumnRect(visibleColumnIndex);
								columnRect.y = rowRect.y;
								columnRect.height = rowRect.height;

								Rect labelFieldRect = multiColumnHeader.GetCellRect(visibleColumnIndex, columnRect);
								labelFieldRect.x += 9;
								labelFieldRect.width -= 9;

								EditorGUI.LabelField(
									labelFieldRect,
									EditorGUIUtility.IconContent(GetWarningIconName(reports[i].Reports[j].WarningType), $"{reports[i].Reports[j].WarningType}")
								);
							}

							// Target Field.
							columnIndex = 1;
							if (multiColumnHeader.IsColumnVisible(columnIndex))
							{
								int visibleColumnIndex = multiColumnHeader.GetVisibleColumnIndex(columnIndex);
								Rect columnRect = multiColumnHeader.GetColumnRect(visibleColumnIndex);
								columnRect.y = rowRect.y;
								columnRect.height = rowRect.height;

								if(reports[i].Reports[j].Target != null)
								{
									if (GUI.Button(
										multiColumnHeader.GetCellRect(visibleColumnIndex, columnRect),
										EditorGUIUtility.IconContent("Animation.FilterBySelection", "Ping Object")
									))
									{
										Selection.objects = new Object[] { reports[i].Reports[j].Target };

										if (!Selection.activeObject)
										{
											Debug.Log($"{nameof(Selection.activeObject)} is null.");
											continue;
										}

										foreach (Object o in Selection.objects)
										{
											EditorGUIUtility.PingObject(o);
										}
									}
								}
							}

							// Category Field.
							columnIndex = 2;
							if (multiColumnHeader.IsColumnVisible(columnIndex))
							{
								int visibleColumnIndex = multiColumnHeader.GetVisibleColumnIndex(columnIndex);
								Rect columnRect = multiColumnHeader.GetColumnRect(visibleColumnIndex);
								columnRect.y = rowRect.y;
								columnRect.height = rowRect.height;

								Rect labelFieldRect = multiColumnHeader.GetCellRect(visibleColumnIndex, columnRect);
								labelFieldRect.x += 7;
								labelFieldRect.width -= 7;

								EditorGUI.LabelField(
									labelFieldRect,
									new GUIContent($"{reports[i].Reports[j].Category}")
								);
							}

							// Message Field.
							columnIndex = 3;
							if (multiColumnHeader.IsColumnVisible(columnIndex))
							{
								int visibleColumnIndex = multiColumnHeader.GetVisibleColumnIndex(columnIndex);
								Rect columnRect = multiColumnHeader.GetColumnRect(visibleColumnIndex);
								columnRect.y = rowRect.y;
								columnRect.height = rowRect.height;

								Rect labelFieldRect = multiColumnHeader.GetCellRect(visibleColumnIndex, columnRect);
								labelFieldRect.x += 7;
								labelFieldRect.width -= 7;

								EditorGUI.LabelField(
									labelFieldRect,
									new GUIContent($"{reports[i].Reports[j].Message}")
								);
							}

							// Solution Field.
							columnIndex = 4;
							if (multiColumnHeader.IsColumnVisible(columnIndex))
							{
								int visibleColumnIndex = multiColumnHeader.GetVisibleColumnIndex(columnIndex);
								Rect columnRect = multiColumnHeader.GetColumnRect(visibleColumnIndex);
								columnRect.y = rowRect.y;
								columnRect.height = rowRect.height;

								Rect labelFieldRect = multiColumnHeader.GetCellRect(visibleColumnIndex, columnRect);
								labelFieldRect.x += 7;
								labelFieldRect.width -= 7;

								EditorGUI.LabelField(
									labelFieldRect,
									new GUIContent($"{reports[i].Reports[j].Solution}")
								);
							}
						}

						rowRect.y += columnHeight;
						rows++;
					}
				}
				scope.handleScrollWheel = true;
			}
		}

		private void LoadValidators()
		{
			for (int i = validators.Count - 1; i >= 0; i--)
			{
				if (validators[i].validator == null)
				{
					validators.RemoveAt(i);
				}
			}

			foreach (Type type in GetValidatorTypes())
			{
				bool hasValidator = false;
				foreach (ValidatorInfo validatorInfo in validators)
				{
					if (validatorInfo.validator.GetType() == type)
					{
						hasValidator = true;
					}
				}

				if (!hasValidator)
				{
					IValidator validator = (IValidator)Activator.CreateInstance(type);
					if (validator != null)
					{
						validators.Add(new ValidatorInfo(validator));
					}
				}
			}
		}

		private void RunValidators()
		{
			reports.Clear();

			foreach (ValidatorInfo validatorInfo in validators)
			{
				if (validatorInfo.isEnabled)
				{
					reports.Add(validatorInfo.validator.Validate());
				}
			}
		}

		private void UpdateStats()
		{
			reportStats = new ReportStats();

			foreach (Report report in reports)
			{
				for (int i = 0; i < report.Reports.Count; i++)
				{
					switch (report.Reports[i].WarningType)
					{
						case WarningType.Info:
							reportStats.infoCount++;
							break;
						case WarningType.Warning:
							reportStats.warningCount++;
							break;
						case WarningType.Error:
							reportStats.errorCount++;
							break;
						default:
							break;
					}
				}
			}
		}

		private void OnValidatorInfoVisibilityChangedEvent(object info)
		{
			if(info is ValidatorInfo validatorInfo)
			{
				validatorInfo.isEnabled = !validatorInfo.isEnabled;
			}
		}

		private void OnValidatorInfoVisibilityAllEvent()
		{
			foreach (ValidatorInfo validatorInfo in validators)
			{
				validatorInfo.isEnabled = true;
			}
		}

		private void OnValidatorInfoVisibilityNoneEvent()
		{
			foreach (ValidatorInfo validatorInfo in validators)
			{
				validatorInfo.isEnabled = false;
			}
		}

		private static Type[] GetValidatorTypes()
		{
			return TypeCache.GetTypesDerivedFrom<IValidator>().ToArray();
		}

		private static string GetWarningIconName(WarningType warningType)
		{
			return warningType switch
			{
				WarningType.Info => "d_console.infoicon.sml",
				WarningType.Warning => "d_console.warnicon.sml",
				WarningType.Error => "d_console.erroricon.sml",
				_ => "d_console.erroricon.inactive.sml",
			};
		}
	}
}