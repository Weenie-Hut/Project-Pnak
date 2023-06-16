/**
 * This file is part of the NAF-Extension, an editor extension for Unity3d.
 *
 * @link   NAF-URL
 * @author Nevin Foster
 * @since  14.06.23
 */

using UnityEditor;
using UnityEngine;

namespace Pnak
{
	[CustomPropertyDrawer(typeof(TabAttribute))]
	public class TabAttributePropertyDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property,
												GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, true);
		}

		public override void OnGUI(Rect position,
								SerializedProperty property,
								GUIContent label)
		{
			TabAttribute tabAttribute = attribute as TabAttribute;
			int indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel += tabAttribute.TabCount;

			EditorGUI.PropertyField(position, property, label, true);
			
			EditorGUI.indentLevel = indent;
		}
	}
}