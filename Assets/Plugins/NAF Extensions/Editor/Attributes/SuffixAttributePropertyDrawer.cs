/**
 * This file is part of the NAF-Extension, an editor extension for Unity3d.
 *
 * @link   NAF-URL
 * @author Nevin Foster
 * @since  14.06.23
 */

using UnityEditor;
using UnityEngine;
using Pnak;

namespace PnakEditor
{
	[CustomPropertyDrawer(typeof(SuffixAttribute))]
	public class SuffixAttributePropertyDrawer : PropertyDrawer
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
			SuffixAttribute suffixAttribute = attribute as SuffixAttribute;
			string suffix = suffixAttribute?.Suffix;
			string tooltip = suffixAttribute?.Tooltip;

			if (!string.IsNullOrEmpty(suffix))
			{
				GUIContent content = new GUIContent(suffix);
				if (!string.IsNullOrEmpty(tooltip))
					label.tooltip = tooltip;

				DrawingUtility.GUISuffix(ref position, content);
			}

			EditorGUI.PropertyField(position, property, label, true);
		}
	}
}