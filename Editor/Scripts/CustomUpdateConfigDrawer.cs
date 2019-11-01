using UnityEngine;
using UnityEditor;

using Miscreant.Utilities.Lifecycle;

[CustomPropertyDrawer(typeof(CustomUpdateManager.Config))]
public class CustomUpdateConfigDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(position, label, property);

		EditorGUI.BeginChangeCheck();

		EditorGUI.PropertyField(position, property, label, property.isExpanded);

		if (EditorGUI.EndChangeCheck() && EditorApplication.isPlaying)
		{
			property.serializedObject.ApplyModifiedProperties();

			var selectedObjects = property.serializedObject.targetObjects;
			// System.Array.Sort(selectedObjects, new InstanceIdComparer());

			foreach (var selection in selectedObjects)
			{
				var updateBehaviour = selection as CustomUpdateBehaviour;
				if (updateBehaviour != null)
				{
					updateBehaviour.updateConfig.valueEnabledAction?.Invoke();
				}
			}
		}

		EditorGUI.EndProperty();
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return EditorGUI.GetPropertyHeight(property, label, property.isExpanded);
	}

	// private class InstanceIdComparer : System.Collections.Generic.IComparer<Object>
	// {
	// 	public int Compare(Object one, Object other)
	// 	{
	// 		if (one.GetInstanceID() < other.GetInstanceID())
	// 		{
	// 			return -1;
	// 		}
	// 		else if (one.GetInstanceID() > other.GetInstanceID())
	// 		{
	// 			return 1;
	// 		}
	// 		return 0;
	// 	}
	// }
}
