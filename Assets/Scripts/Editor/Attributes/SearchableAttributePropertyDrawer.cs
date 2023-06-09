using System.Linq;
using UnityEditor;
using UnityEngine;
using Pnak;

namespace PnakEditor
{
	[CustomPropertyDrawer(typeof(SearchableAttribute))]
	public class SearchableAttributePropertyDrawer : PropertyDrawer
	{
		public string GetAnyErrors(SerializedProperty property)
		{
			if (property.propertyType != SerializedPropertyType.ObjectReference)
			{
				return "ERROR: Field is not an Object";
			}

			SearchableAttribute searchable = attribute as SearchableAttribute;

			Object obj = property.objectReferenceValue;

			if (obj == null) return null; // Use the required attribute if you want to force a value

			if (searchable.RequiredComponents != null)
			{
				if (!(obj is GameObject go))
				{
					if (obj is Component component)
						go = component.gameObject;
					else
						return "ERROR: Field Target Invalid";
				}

				foreach (System.Type type in searchable.RequiredComponents)
				{
					if (searchable.IncludeChildren)
						{ if (go.GetComponent(type) != null) continue; }
					else
						{ if (go.GetComponentInChildren(type) != null) continue; }

					return "Missing Component: " + type.Name;
				}
			}

			if (searchable.AssetsOnly && PrefabUtility.GetPrefabAssetType(obj) == PrefabAssetType.NotAPrefab)
			{
				return "Not a Prefab";
			}

			return null;
		}

		public override float GetPropertyHeight(SerializedProperty property,
												GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, true) + (GetAnyErrors(property) != null ? EditorGUIUtility.singleLineHeight : 0);
		}

		public override void OnGUI(Rect position,
								SerializedProperty property,
								GUIContent label)
		{
			int indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			GUIContent content = new GUIContent("Search", "Search for a prefab using the type of the field (will show all component on the root of prefabs)");
			GUIStyle style = EditorStyles.miniButton;


			float thisWidth = style.CalcSize(content).x;
			float originalWidth = position.width;
			position.x += position.width - thisWidth;
			position.width = thisWidth;

			if (GUI.Button(position, content, style))
			{
				SearchableAttribute searchable = attribute as SearchableAttribute;

				var options = ObjectPickerEntry.CreateObjectPickerDictionary(
					property, IsValid,
					searchable.AssetsOnly ? ObjectPickerEntry.IncludeDropdowns.Assets : ObjectPickerEntry.IncludeDropdowns.All);

				UnityObjectPicker.Show(options, (obj) =>
				{
					property.objectReferenceValue = obj;
					property.serializedObject.ApplyModifiedProperties();
				});
			}

			position.x -= originalWidth - thisWidth;
			position.width = originalWidth - thisWidth;

			EditorGUI.indentLevel = indent;

			EditorGUI.PropertyField(position, property, label, true);

			string error = GetAnyErrors(property);
			if (error != null)
			{
				Rect errorRect = position;
				errorRect.y += EditorGUIUtility.singleLineHeight;
				errorRect.height = EditorGUIUtility.singleLineHeight;
				errorRect.width = originalWidth - EditorGUIUtility.labelWidth;
				errorRect.x += EditorGUIUtility.labelWidth;
				EditorGUI.HelpBox(errorRect, error, MessageType.Error);
			} 
		}

		public bool IsValid(GameObject go)
		{
			if (typeof(Component).IsAssignableFrom(fieldInfo.FieldType) &&
				go.GetComponent(fieldInfo.FieldType) == null) return false;

			SearchableAttribute searchable = attribute as SearchableAttribute;
			foreach (System.Type type in searchable.RequiredComponents)
			{
				if (searchable.IncludeChildren)
					{ if (go.GetComponent(type) != null) continue; }
				else
					{ if (go.GetComponentInChildren(type) != null) continue; }

				return false;
			}
			return true;
		}
	}
}