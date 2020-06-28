using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;
using System.Reflection;

[CustomPropertyDrawer(typeof(SortingLayerAttribute))]
public class SortingLayerDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		if (property.propertyType != SerializedPropertyType.Integer)
			return;

		EditorGUI.LabelField(position, label);

		position.x += EditorGUIUtility.labelWidth;
		position.width -= EditorGUIUtility.labelWidth;

		string[] tempNames = GetSortingLayerNames();
		string[] sortingLayerNames = new string[tempNames.Length + 1];
		for (int i = 0; i < tempNames.Length; i++)
			sortingLayerNames[i] = tempNames[i];
		sortingLayerNames[sortingLayerNames.Length - 1] = "Add Sorting Layer...";

		int[] tempIDs = GetSortingLayerIDs();
		int[] sortingLayerIDs = new int[tempIDs.Length + 1];
		for (int i = 0; i < tempIDs.Length; i++)
			sortingLayerIDs[i] = tempIDs[i];
		sortingLayerIDs[sortingLayerIDs.Length - 1] = -1;

		int sortingLayerIndex = Mathf.Max(-1, System.Array.IndexOf<int>(sortingLayerIDs, property.intValue));
		sortingLayerIndex = EditorGUI.Popup(position, sortingLayerIndex, sortingLayerNames);

		if (sortingLayerIDs[sortingLayerIndex] == -1)
		{
			EditorApplication.ExecuteMenuItem("Edit/Project Settings/Tags and Layers");
			return;
		}
		else
			property.intValue = sortingLayerIDs[sortingLayerIndex];
	}

	private string[] GetSortingLayerNames()
	{
		System.Type internalEditorUtilityType = typeof(InternalEditorUtility);
		PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty(
				"sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
		return (string[])sortingLayersProperty.GetValue(null, new object[0]);
	}

	private int[] GetSortingLayerIDs()
	{
		System.Type internalEditorUtilityType = typeof(InternalEditorUtility);
		PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty(
				"sortingLayerUniqueIDs", BindingFlags.Static | BindingFlags.NonPublic);
		return (int[])sortingLayersProperty.GetValue(null, new object[0]);
	}
}