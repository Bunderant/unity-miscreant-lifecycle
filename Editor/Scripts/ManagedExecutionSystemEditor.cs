using UnityEngine;
using UnityEditorInternal;
using UnityEditor;
using System.Collections.Generic;

namespace Miscreant.Lifecycle.Editor
{
	using Editor = UnityEditor.Editor;

	[CustomEditor(typeof(ManagedExecutionSystem))]
	public class ManagedExecutionSystemEditor : Editor
	{
		private ManagedExecutionSystem _target;
		private ManagedExecutionSystem_RuntimeDisplayData _latestData;
		private ReorderableList _executionGroups;

		private List<ManagedExecutionGroup> _lastSavedExecutionGroups;
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
			_target = (ManagedExecutionSystem)target;
			_executionGroups = new ReorderableList(
				serializedObject,
				serializedObject.FindProperty(nameof(_executionGroups)),
				true,
				true,
				true,
				true
			);

			_executionGroups.drawHeaderCallback = (
				rect =>
				{
					EditorGUI.LabelField(rect, "Execution Order", EditorStyles.boldLabel);
				}
			);

			_executionGroups.drawElementCallback = (
				(Rect rect, int index, bool isActive, bool isFocused) =>
				{
					var element = _executionGroups.serializedProperty.GetArrayElementAtIndex(index);
					EditorGUI.ObjectField(
						new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
						element,
						GUIContent.none
					);
				}
			);

			_executionGroups.onChangedCallback = (
				rect =>
				{
					_isDirty = true;
				}
			);

			SetLastSavedExecutionOrderToCurrent();

			_runtimeGroupVisibility = new bool[_executionGroups.count];
			_runtimeGroupScrollPositions = new Vector2[_executionGroups.count];

			_latestData = ScriptableObject.CreateInstance<ManagedExecutionSystem_RuntimeDisplayData>();

			EditorApplication.update += OnUpdate;
		}

		private void OnDisable()
		{
			if (_isDirty)
			{
				serializedObject.Update();
				SetCurrentExecutionOrderToLastSaved();
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
				_executionGroups.DoLayoutList();

				if (_isDirty)
				{
					EditorGUILayout.Separator();
					GUILayout.BeginHorizontal();

					if (GUILayout.Button("Apply"))
						SetLastSavedExecutionOrderToCurrent();
					if (GUILayout.Button("Revert"))
						SetCurrentExecutionOrderToLastSaved();

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
				DisplayRuntimeExecutionGroupLists();
			}

			serializedObject.ApplyModifiedProperties();
		}

		private void DisplayRuntimeExecutionGroupLists()
		{
			_latestData.Initialize(_target);
			var so = new SerializedObject(_latestData);

			_selectedTabIndex = GUILayout.Toolbar(_selectedTabIndex, _tabNames);
			DrawGroupCollection(
				so.FindProperty(nameof(_executionGroups)),
				so.FindProperty(_tabPropertyNameLookup[_selectedTabIndex])
			);
		}

		/// <summary>
		/// Cleanly displays the runtime objects registered in the system.
		/// </summary>
		/// <param name="executionGroups"></param>
		/// <param name="groupsForSelectedType"></param>
		private void DrawGroupCollection(SerializedProperty executionGroups, SerializedProperty groupsForSelectedType, int maxVisibleElementsPerGroup = 16)
		{
			EditorGUI.indentLevel++;

			int groupCount = executionGroups.arraySize;
			for (int groupIndex = 0; groupIndex < groupCount; groupIndex++)
			{
				string groupName = executionGroups.GetArrayElementAtIndex(groupIndex).objectReferenceValue.name;
				SerializedProperty currentGroup = groupsForSelectedType.GetArrayElementAtIndex(groupIndex).FindPropertyRelative("value");
				int currentGroupSize = currentGroup.arraySize;

				if (_runtimeGroupVisibility[groupIndex] = EditorGUILayout.Foldout(
					_runtimeGroupVisibility[groupIndex],
					groupName,
					currentGroupSize != 0,
					EditorStyles.foldout))
				{
					bool scrollViewShowing = currentGroupSize > maxVisibleElementsPerGroup;
					if (scrollViewShowing)
					{
						_runtimeGroupScrollPositions[groupIndex] = EditorGUILayout.BeginScrollView(
							_runtimeGroupScrollPositions[groupIndex],
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

		private void SetLastSavedExecutionOrderToCurrent()
		{
			int originalSize = _executionGroups.serializedProperty.arraySize;
			_lastSavedExecutionGroups = new List<ManagedExecutionGroup>(originalSize);
			for (int i = 0; i < originalSize; i++)
			{
				_lastSavedExecutionGroups.Add((ManagedExecutionGroup)_executionGroups.serializedProperty.GetArrayElementAtIndex(i).objectReferenceValue);
			}

			_isDirty = false;
		}

		private void SetCurrentExecutionOrderToLastSaved()
		{
			_executionGroups.serializedProperty.arraySize = _lastSavedExecutionGroups.Count;
			for (int i = 0; i < _lastSavedExecutionGroups.Count; i++)
			{
				_executionGroups.serializedProperty.GetArrayElementAtIndex(i).objectReferenceValue = _lastSavedExecutionGroups[i];
			}

			_isDirty = false;
		}
	}
}


