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
	public static class DrawingUtility
	{
		public static Rect GetInlinePosition(ref Rect position, float width, bool right = true)
		{
			if (!right) // left
			{
				Rect result = position;
				position.x += width;
				position.width -= width;
				result.width = width;
				return result;
			}
			else // right
			{
				Rect result = position;
				position.width -= width;
				result.x += position.width;
				result.width = width;
				return result;
			}
		}

		public static void InlineLabel(ref Rect position, GUIContent content, GUIStyle style = null, bool right = true)
		{
			if (content == null) return;
			if (style == null) style = EditorStyles.miniLabel;

			int indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			Rect labelPosition = GetInlinePosition(ref position, style.CalcSize(content).x, right);
			UnityEditor.EditorGUI.LabelField(labelPosition, content, style);

			EditorGUI.indentLevel = indent;
		}

		public static void GUISuffix(ref Rect position, GUIContent content, GUIStyle style = null)
		{
			InlineLabel(ref position, content, style, true);
		}

		public static void GUIPrefix(ref Rect position, GUIContent content, GUIStyle style = null)
		{
			InlineLabel(ref position, content, style, false);
		}

		public static bool InlineButton(ref Rect position, GUIContent content, GUIStyle style = null, bool right = true)
		{
			if (content == null) return false;
			if (style == null) style = EditorStyles.miniButton;

			int indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			Rect buttonPosition = GetInlinePosition(ref position, style.CalcSize(content).x, right);
			bool result = GUI.Button(buttonPosition, content, style);

			EditorGUI.indentLevel = indent;

			return result;
		}

		public static void InlineHelpBox(ref Rect position, string content, MessageType type, bool right = true)
		{
			GUIStyle style = EditorStyles.helpBox;

			int indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			Rect boxPosition = GetInlinePosition(ref position, style.CalcSize(new GUIContent(content)).x + 14, right);
			EditorGUI.HelpBox(boxPosition, content, type);

			EditorGUI.indentLevel = indent;
		}

		public static GUIStyle GetGUIStyle(LabelType type)
		{
			GUIStyle style = new GUIStyle(GUI.skin.label);
			style.wordWrap = true;

			if (type.HasFlag(LabelType.Bold))
				style.fontStyle = FontStyle.Bold;

			if (type.HasFlag(LabelType.Italic))
				style.fontStyle = FontStyle.Italic;

			if (type.HasFlag(LabelType.Mini))
				style.fontSize = 9;

			if (type.HasFlag(LabelType.Left))
				style.alignment = TextAnchor.MiddleLeft;
			
			if (type.HasFlag(LabelType.Center))
				style.alignment = TextAnchor.MiddleCenter;

			if (type.HasFlag(LabelType.Right))
				style.alignment = TextAnchor.MiddleRight;

			return style;
		}
	}
}