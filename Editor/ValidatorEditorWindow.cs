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
        private readonly List<Report> reports = new List<Report>();
        private readonly List<IValidator> validators = new List<IValidator>();

        private MultiColumnHeaderState multiColumnHeaderState;
        private MultiColumnHeader multiColumnHeader;
        private MultiColumnHeaderState.Column[] columns;
        private readonly Color lightColor = Color.white * 0.3f;
        private readonly Color darkColor = Color.white * 0.1f;
        private Vector2 scrollPosition;
        private float multiColumnHeaderWidth;
        private float rows;

        [MenuItem("Window/General/Validator")]
        private static void Init()
        {
            ValidatorEditorWindow window = (ValidatorEditorWindow)GetWindow(typeof(ValidatorEditorWindow));
            window.titleContent = new GUIContent("Validator");
            window.Show();
            window.LoadValidators();
        }

		private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(EditorGUIUtility.IconContent("Refresh", "Refresh")))
					{
                        LoadValidators();
                    }

                    if (GUILayout.Button(EditorGUIUtility.IconContent("PlayButton", "Run all")))
                    {
                        reports.Clear();

                        foreach (IValidator validator in validators)
                        {
                            reports.Add(validator.Validate());
                        }
                    }

                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_preAudioLoopOff", "Select validator")))
					{
                        GenericMenu validatorOptionsMenu = new GenericMenu();
                        foreach (IValidator validator in validators)
                        {
                            validatorOptionsMenu.AddItem(new GUIContent($"{validator.GetType().Name}"), false, OnGenericValidate, validator);
                        }
                        validatorOptionsMenu.ShowAsContext();
					}

                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"{reports.Count}/{validators.Count}");
                }

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

            multiColumnHeaderState = new MultiColumnHeaderState(columns: this.columns);
            multiColumnHeader = new MultiColumnHeader(state: this.multiColumnHeaderState);

            // When we chagne visibility of the column we resize columns to fit in the window.
            //multiColumnHeader.visibleColumnsChanged += (multiColumnHeader) => multiColumnHeader.ResizeToFit();
            // Initial resizing of the content.
            multiColumnHeader.ResizeToFit();
        }

        private void DrawMultiColumnScope()
        {
            GUILayout.FlexibleSpace();
            Rect windowRect = GUILayoutUtility.GetLastRect();
            windowRect.width = position.width;
            windowRect.height = position.height;

            if (multiColumnHeader == null)
            {
                InitializeMultiColumn();
            }

            float columnHeight = EditorGUIUtility.singleLineHeight * 2;

            Rect headerRect = new Rect(source: windowRect)
            {
                height = EditorGUIUtility.singleLineHeight,
            };

            Rect scrollViewPositionRect = GUILayoutUtility.GetRect(0, float.MaxValue, 0, float.MaxValue);
            scrollViewPositionRect.y += headerRect.height - EditorGUIUtility.standardVerticalSpacing;
            scrollViewPositionRect.height -= headerRect.height - EditorGUIUtility.standardVerticalSpacing;

            Rect scrollViewRect = new Rect(source: windowRect)
            {
                width = multiColumnHeaderState.widthOfAllVisibleColumns,
                height = rows * columnHeight
            };

            Rect rowRect = new Rect(source: windowRect)
            {
                width = multiColumnHeaderWidth,
                height = columnHeight,
            };

            // Draw column header.
            multiColumnHeader.OnGUI(rect: headerRect, xScroll: scrollPosition.x);

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
                        // Only draw what is visable within the view.
						if (rowRect.yMax > windowRect.y + scrollPosition.y && rowRect.yMin < windowRect.height + scrollPosition.y)
						{
                            if (j % 2 == 0)
                            {
                                EditorGUI.DrawRect(rect: rowRect, color: darkColor);
                            }
                            else
                            {
                                EditorGUI.DrawRect(rect: rowRect, color: lightColor);
                            }

                            // Warning Field.
                            int columnIndex = 0;
                            if (multiColumnHeader.IsColumnVisible(columnIndex: columnIndex))
                            {
                                int visibleColumnIndex = multiColumnHeader.GetVisibleColumnIndex(columnIndex: columnIndex);
                                Rect columnRect = multiColumnHeader.GetColumnRect(visibleColumnIndex: visibleColumnIndex);
                                columnRect.y = rowRect.y;
                                columnRect.height = rowRect.height;

                                Rect labelFieldRect = multiColumnHeader.GetCellRect(visibleColumnIndex: visibleColumnIndex, columnRect);
                                labelFieldRect.x += 9;
                                labelFieldRect.width -= 9;

                                EditorGUI.LabelField(
                                    position: labelFieldRect,
                                    label: EditorGUIUtility.IconContent(GetWarningIconName(reports[i].Reports[j].WarningType), $"{reports[i].Reports[j].WarningType}")
                                );
                            }

                            // Target Field.
                            columnIndex = 1;
                            if (multiColumnHeader.IsColumnVisible(columnIndex: columnIndex))
                            {
                                int visibleColumnIndex = multiColumnHeader.GetVisibleColumnIndex(columnIndex: columnIndex);
                                Rect columnRect = multiColumnHeader.GetColumnRect(visibleColumnIndex: visibleColumnIndex);
                                columnRect.y = rowRect.y;
                                columnRect.height = rowRect.height;

                                if(reports[i].Reports[j].Target != null)
								{
                                    if (GUI.Button(
                                        position: multiColumnHeader.GetCellRect(visibleColumnIndex: visibleColumnIndex, columnRect),
                                        content: EditorGUIUtility.IconContent("Animation.FilterBySelection", "Ping Object")
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
                            if (multiColumnHeader.IsColumnVisible(columnIndex: columnIndex))
                            {
                                int visibleColumnIndex = multiColumnHeader.GetVisibleColumnIndex(columnIndex: columnIndex);
                                Rect columnRect = multiColumnHeader.GetColumnRect(visibleColumnIndex: visibleColumnIndex);
                                columnRect.y = rowRect.y;
                                columnRect.height = rowRect.height;

                                Rect labelFieldRect = multiColumnHeader.GetCellRect(visibleColumnIndex: visibleColumnIndex, columnRect);
                                labelFieldRect.x += 7;
                                labelFieldRect.width -= 7;

                                EditorGUI.LabelField(
                                    position: labelFieldRect,
                                    label: new GUIContent($"{reports[i].Reports[j].Category}")
                                );
                            }

                            // Message Field.
                            columnIndex = 3;
                            if (multiColumnHeader.IsColumnVisible(columnIndex: columnIndex))
                            {
                                int visibleColumnIndex = multiColumnHeader.GetVisibleColumnIndex(columnIndex: columnIndex);
                                Rect columnRect = multiColumnHeader.GetColumnRect(visibleColumnIndex: visibleColumnIndex);
                                columnRect.y = rowRect.y;
                                columnRect.height = rowRect.height;

                                Rect labelFieldRect = multiColumnHeader.GetCellRect(visibleColumnIndex: visibleColumnIndex, columnRect);
                                labelFieldRect.x += 7;
                                labelFieldRect.width -= 7;

                                EditorGUI.LabelField(
                                    position: labelFieldRect,
                                    label: new GUIContent($"{reports[i].Reports[j].Message}")
                                );
                            }

                            // Solution Field.
                            columnIndex = 4;
                            if (multiColumnHeader.IsColumnVisible(columnIndex: columnIndex))
                            {
                                int visibleColumnIndex = multiColumnHeader.GetVisibleColumnIndex(columnIndex: columnIndex);
                                Rect columnRect = multiColumnHeader.GetColumnRect(visibleColumnIndex: visibleColumnIndex);
                                columnRect.y = rowRect.y;
                                columnRect.height = rowRect.height;

                                Rect labelFieldRect = multiColumnHeader.GetCellRect(visibleColumnIndex: visibleColumnIndex, columnRect);
                                labelFieldRect.x += 7;
                                labelFieldRect.width -= 7;

                                EditorGUI.LabelField(
                                    position: labelFieldRect,
                                    label: new GUIContent($"{reports[i].Reports[j].Solution}")
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
                if (validators[i] == null)
                {
                    validators.RemoveAt(i);
                }
            }

            foreach (Type type in GetValidatorTypes())
            {
                bool hasValidator = false;
                foreach (IValidator validator in validators)
                {
                    if (validator.GetType() == type)
                    {
                        hasValidator = true;
                    }
                }

                if (!hasValidator)
                {
                    IValidator validator = (IValidator)Activator.CreateInstance(type);
                    if (validator != null)
                    {
                        validators.Add(validator);
                    }
                }
            }
        }

        private void OnGenericValidate(object validator)
        {
            if (validator is IValidator val)
            {
                reports.Clear();
                reports.Add(val.Validate());
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
				WarningType.Info => "d_console.warnicon.inactive.sml",
				WarningType.Warning => "d_console.warnicon.sml",
				WarningType.Error => "d_console.erroricon.sml",
				_ => "d_console.erroricon.inactive.sml",
			};
		}
    }
}