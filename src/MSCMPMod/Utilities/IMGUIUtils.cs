using UnityEngine;

namespace MSCMP.Utilities {
	static class IMGUIUtils {
		/// <summary>
		/// The 1x1 plain white pixel texture.
		/// </summary>
		static Texture2D fillText = new Texture2D(1, 1);

		/// <summary>
		/// Small label style.
		/// </summary>
		static GUIStyle smallLabelStyle = new GUIStyle();

		/// <summary>
		/// Setup all rendering objects.
		/// </summary>
		public static void Setup() {
			smallLabelStyle.fontSize = 11;

			fillText.SetPixel(0, 0, Color.white);
			fillText.wrapMode = TextureWrapMode.Repeat;
			fillText.Apply();
		}

		/// <summary>
		/// Draw plain color rectangle.
		/// </summary>
		/// <param name="rct">Where rectangle should be drawn.</param>
		public static void DrawPlainColorRect(Rect rct) {
			if (fillText != null) { GUI.DrawTexture(rct, fillText); }
		}

		/// <summary>
		/// Draw small label.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="rct">The rectangle where label should be drawn.</param>
		/// <param name="color">Color of the label.</param>
		/// <param name="shadow">Should the method also draw shadow?</param>
		public static void DrawSmallLabel(
				string text, Rect rct, Color color, bool shadow = false) {
			if (shadow) {
				rct.y += 1;
				rct.x += 1;
				smallLabelStyle.normal.textColor = Color.black;
				GUI.Label(rct, text, smallLabelStyle);
				rct.y -= 1;
				rct.x -= 1;
			}
			smallLabelStyle.normal.textColor = color;
			GUI.Label(rct, text, smallLabelStyle);
		}
	}
}
