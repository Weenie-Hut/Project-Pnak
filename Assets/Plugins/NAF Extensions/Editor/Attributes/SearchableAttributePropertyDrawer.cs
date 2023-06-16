/**
 * This file is part of the NAF-Extension, an editor extension for Unity3d.
 *
 * @link   NAF-URL
 * @author Nevin Foster
 * @since  14.06.23
 */

using System.Linq;
using UnityEditor;
using UnityEngine;
using Pnak;

namespace PnakEditor
{
	[CustomPropertyDrawer(typeof(SearchableAttribute))]
	public class SearchableAttributePropertyDrawer : PropertyDrawer
	{
		public static string GetAnyErrors(SerializedProperty property, System.Type[] requiredComponents = null, bool assetsOnly = false, bool includeChildren = false)
		{
			if (property.propertyType != SerializedPropertyType.ObjectReference)
			{
				return "ERROR: Field is not an Object";
			}

			Object obj = property.objectReferenceValue;

			if (obj == null) return null; // Use the required attribute if you want to force a value

			if (requiredComponents != null)
			{
				if (!(obj is GameObject go))
				{
					if (obj is Component component)
						go = component.gameObject;
					else
						return "ERROR: Field Target Invalid";
				}

				foreach (System.Type type in requiredComponents)
				{
					if (includeChildren)
						{ if (go.GetComponent(type) != null) continue; }
					else
						{ if (go.GetComponentInChildren(type) != null) continue; }

					return "Missing Component: " + type.Name;
				}
			}

			if (assetsOnly && PrefabUtility.GetPrefabAssetType(obj) == PrefabAssetType.NotAPrefab)
			{
				return "Not a Prefab";
			}

			return null;
		}

		public static float GetExtraHeight(SerializedProperty property, System.Type[] requiredComponents = null, bool assetsOnly = false, bool includeChildren = false)
		{
			return (GetAnyErrors(property, requiredComponents, assetsOnly, includeChildren) != null ? EditorGUIUtility.singleLineHeight : 0);
		}

		private float ThisGetExtraHeight(SerializedProperty property)
		{
			SearchableAttribute searchable = attribute as SearchableAttribute;
			return GetExtraHeight(property, searchable.RequiredComponents, searchable.AssetsOnly, searchable.IncludeChildren);
		}

		public override float GetPropertyHeight(SerializedProperty property,
												GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, true) + ThisGetExtraHeight(property);
		}

		public static void Draw(ref Rect position, SerializedProperty property, System.Type propertyType, System.Type[] requiredComponents = null, bool assetsOnly = false, bool includeChildren = false)
		{
			float originalWidth = position.width;

			GUIContent content = new GUIContent("Search", "Search for a prefab using the type of the field (will show all component on the root of prefabs)");

			if (DrawingUtility.InlineButton(ref position, content))
			{
				UnityObjectPicker.Show(property, go => IsValid(go, propertyType, requiredComponents, includeChildren), assetsOnly);
			}

			string error = GetAnyErrors(property, requiredComponents, assetsOnly, includeChildren);
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

		public override void OnGUI(Rect position,
								SerializedProperty property,
								GUIContent label)
		{
			SearchableAttribute searchable = attribute as SearchableAttribute;
			Draw(ref position, property, fieldInfo.FieldType, searchable.RequiredComponents, searchable.AssetsOnly, searchable.IncludeChildren);
			EditorGUI.PropertyField(position, property, label, true);
		}

		public static bool IsValid(GameObject go, System.Type type, System.Type[] requiredComponents, bool includeChildren)
		{
			if (typeof(Component).IsAssignableFrom(type) &&
				go.GetComponent(type) == null) return false;
			if (requiredComponents == null) return true;

			foreach (System.Type cType in requiredComponents)
			{
				if (includeChildren)
					{ if (go.GetComponent(cType) != null) continue; }
				else
					{ if (go.GetComponentInChildren(cType) != null) continue; }

				return false;
			}
			return true;
		}
	}
}