using UnityEngine;
using UnityEngine.UI;

namespace MSCMP.UI.Handlers {
	/// <summary>
	/// Handle of the message box window.
	/// </summary>
	class MessageBoxHandler : MonoBehaviour {

		/// <summary>
		/// Delegate of the callback called when window is closed.
		/// </summary>
		public delegate void OnClose();

		OnClose onClose = null;

		/// <summary>
		/// Setup the handler after message box spawn.
		/// </summary>
		void Start() {
			var btn = transform.FindChild("OKButton").GetComponent<Button>();
			btn.onClick.AddListener(() => {
				Close();

				if (onClose != null) {
					onClose();
					onClose = null;
				}
			});
		}

		/// <summary>
		/// Close currently active message box.
		/// </summary>
		public void Close() {
			gameObject.SetActive(false);
			MPGUI.Instance.ShowCursor(false);
		}

		/// <summary>
		/// Show message box with given text.
		/// </summary>
		/// <param name="text">The text to show.</param>
		/// <param name="onClose">The callback that will be closed when OK button is
		/// pressed.</param> <returns>true if message box was showed false
		/// otherwise</returns>
		public bool Show(string text, OnClose onClose = null) {
			// Allow only one message box.
			if (gameObject.activeSelf) { return false; }
			this.onClose = onClose;
			transform.FindChild("Text").gameObject.GetComponent<Text>().text = text;
			gameObject.SetActive(true);
			MPGUI.Instance.ShowCursor(true);
			return true;
		}
	}
}
