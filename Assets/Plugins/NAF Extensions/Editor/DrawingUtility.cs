/**
 * This file is part of the NAF-Extension, an editor extension for Unity3d.
 *
 * @link   NAF-URL
 * @author Nevin Foster
 * @since  14.06.23
 */
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