using UnityEngine;
using UnityEditorInternal;
using UnityEditor;
using System.Collections.Generic;

namespace Miscreant.Lifecycle.Editor
{
	using Editor = UnityEditor.Editor;

	[CustomEditor(typeof(CustomUpdateManager))]
	public class CustomUpdateManagerEditor : Editor
	{
		private CustomUpdateManager _target;
		private CustomUpdateManager_RuntimeDisplayData _latestData;
		private ReorderableList _priorityList;

		private List<CustomUpdatePriority> _lastSavedPriorities;
		private bool _isDirty;

		private bool[] _runtimeGroupVisibility;
		private Vector2[] _runtimeGroupScrollPositions;

		private int _selectedTabIndex;
		private static readonly string[] _tabPropertyNameLookup = new string[] {
			"_updateGroups",
			"_fixedUpdateGroups"
		};

		private static readonly string[] _tabNames = new string[] {
			"Update",
			"Fixed Update"
		};

		private void OnEnable()
		{
			_target = (CustomUpdateManager)target;
			_priorityList = new ReorderableList(
				serializedObject,
				serializedObject.FindProperty("_priorities"),
				true,
				true,
				true,
				true
			);

			_priorityList.drawHeaderCallback = (
				rect =>
				{
					EditorGUI.LabelField(rect, "Execution Order", EditorStyles.boldLabel);
				}
			);

			_priorityList.drawElementCallback = (
				(Rect rect, int index, bool isActive, bool isFocused) =>
				{
					var element = _priorityList.serializedProperty.GetArrayElementAtIndex(index);
					EditorGUI.ObjectField(
						new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
						element,
						GUIContent.none
					);
				}
			);

			_priorityList.onChangedCallback = (
				rect =>
				{
					_isDirty = true;
				}
			);

			SetLastSavedPrioritiesToCurrent();

			_runtimeGroupVisibility = new bool[_priorityList.count];
			_runtimeGroupScrollPositions = new Vector2[_priorityList.count];

			_latestData = ScriptableObject.CreateInstance<CustomUpdateManager_RuntimeDisplayData>();

			EditorApplication.update += OnUpdate;
		}

		private void OnDisable()
		{
			if (_isDirty)
			{
				serializedObject.Update();
				SetCurrentPrioritiesToLastSaved();
				serializedObject.ApplyModifiedProperties();
			}

			EditorApplication.update -= OnUpdate;
		}

		private void OnUpdate()
		{
			if (EditorApplication.isPlaying)
			{
				Repaint();
			}
		}

		override public void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUI.BeginDisabledGroup(Application.isPlaying);
			{
				_priorityList.DoLayoutList();

				if (_isDirty)
				{
					EditorGUILayout.Separator();
					GUILayout.BeginHorizontal();

					if (GUILayout.Button("Apply"))
						SetLastSavedPrioritiesToCurrent();
					if (GUILayout.Button("Revert"))
						SetCurrentPrioritiesToLastSaved();

					GUILayout.EndHorizontal();
				}
			}
			EditorGUI.EndDisabledGroup();

			if (Application.isPlaying)
			{
				EditorGUILayout.Separator();
				EditorGUILayout.HelpBox(
					"Cannot modify execution order while in \'Play\' mode.",
					MessageType.Info
				);
			}

			EditorGUILayout.Separator();
			EditorGUILayout.LabelField(
				Application.isPlaying ? "Runtime Data" : "Enter \'Play\' mode for runtime data.",
				Application.isPlaying ? EditorStyles.boldLabel : EditorStyles.centeredGreyMiniLabel
			);

			if (Application.isPlaying)
			{
				DisplayRuntimePriorityLists();
			}

			serializedObject.ApplyModifiedProperties();
		}

		private void DisplayRuntimePriorityLists()
		{
			_latestData.Initialize(_target);
			var so = new SerializedObject(_latestData);

			_selectedTabIndex = GUILayout.Toolbar(_selectedTabIndex, _tabNames);
			DrawGroupCollection(
				so.FindProperty("_priorities"),
				so.FindProperty(_tabPropertyNameLookup[_selectedTabIndex])
			);
		}

		/// <summary>
		/// Cleanly displays the runtime objects registered in the system.
		/// </summary>
		/// <param name="priorities"></param>
		/// <param name="groups"></param>
		private void DrawGroupCollection(SerializedProperty priorities, SerializedProperty groups, int maxVisibleElementsPerGroup = 16)
		{
			Debug.Assert(
				priorities.arraySize == groups.arraySize,
				"Number of priority groups should always match the number of update groups. Something is terribly wrong.",
				this
			);

			EditorGUI.indentLevel++;

			var priorityGroupCount = priorities.arraySize;
			for (int priorityIndex = 0; priorityIndex < priorityGroupCount; priorityIndex++)
			{
				var priorityName = priorities.GetArrayElementAtIndex(priorityIndex).objectReferenceValue.name;
				var currentGroup = groups.GetArrayElementAtIndex(priorityIndex).FindPropertyRelative("value");
				var currentGroupSize = currentGroup.arraySize;

				if (_runtimeGroupVisibility[priorityIndex] = EditorGUILayout.Foldout(
					_runtimeGroupVisibility[priorityIndex],
					priorityName,
					currentGroupSize != 0,
					EditorStyles.foldout))
				{
					bool scrollViewShowing = currentGroupSize > maxVisibleElementsPerGroup;
					if (scrollViewShowing)
					{
						_runtimeGroupScrollPositions[priorityIndex] = EditorGUILayout.BeginScrollView(
							_runtimeGroupScrollPositions[priorityIndex],
							GUILayout.Height((EditorGUIUtility.singleLineHeight + 2) * (maxVisibleElementsPerGroup))
						);
					}

					for (int behaviourIndex = 0; behaviourIndex < currentGroupSize; behaviourIndex++)
					{
						EditorGUILayout.PropertyField(currentGroup.GetArrayElementAtIndex(behaviourIndex));
					}

					if (scrollViewShowing)
					{
						EditorGUILayout.EndScrollView();
					}
				}
			}

			EditorGUI.indentLevel--;
		}

		private void SetLastSavedPrioritiesToCurrent()
		{
			int originalSize = _priorityList.serializedProperty.arraySize;
			_lastSavedPriorities = new List<CustomUpdatePriority>(originalSize);
			for (int i = 0; i < originalSize; i++)
			{
				_lastSavedPriorities.Add((CustomUpdatePriority)_priorityList.serializedProperty.GetArrayElementAtIndex(i).objectReferenceValue);
			}

			_isDirty = false;
		}

		private void SetCurrentPrioritiesToLastSaved()
		{
			_priorityList.serializedProperty.arraySize = _lastSavedPriorities.Count;
			for (int i = 0; i < _lastSavedPriorities.Count; i++)
			{
				_priorityList.serializedProperty.GetArrayElementAtIndex(i).objectReferenceValue = _lastSavedPriorities[i];
			}

			_isDirty = false;
		}
	}
}


