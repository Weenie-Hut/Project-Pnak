using UnityEditor;
using UnityEngine;

namespace PnakEditor
{
	public static class DrawingUtility
	{
		public static void DrawOnLeftPositions(float width, ref Rect position, out Rect left)
		{
			position.x += position.width - width;
			position.width = width;
			left = position;
			position.x -= width - width;
			position.width = width - width;
		}

		public static void DrawOnLeftPositions(GUIContent content, GUIStyle style, ref Rect position, out Rect left)
		{
			Vector2 dims = style.CalcSize(content);
			DrawOnLeftPositions(dims.x, ref position, out left);
		}
	}
}