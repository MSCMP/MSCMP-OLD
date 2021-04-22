using System;
using System.Collections;
using System.IO;

namespace MSCMP.Utilities {
	class HTMLWriter : IDisposable {

		StreamWriter streamWriter = null;

		string fileName = "";
		public string FileName {
			get { return fileName; }
		}

		public HTMLWriter(string fileName) {
			this.fileName = fileName;
			streamWriter = new StreamWriter(fileName, false, System.Text.Encoding.UTF8);
			if (streamWriter == null) {
				Logger.Log($"Failed to write document - {fileName}!");
			} else {
				streamWriter.AutoFlush = false;
			}
		}

		public void Dispose() { streamWriter.Close(); }

		public void WriteString(string str) { streamWriter.Write(str); }

		Stack tagStack = new Stack();

		public void ShortTag(string name, string attributes = "") {
			WriteString($"<{name}");
			if (attributes.Length > 0) { WriteString($" {attributes}"); }
			WriteString("/>");
		}

		public void StartTag(string name, string attributes = "") {
			WriteString($"<{name}");
			if (attributes.Length > 0) { WriteString($" {attributes}"); }
			WriteString(">");
			tagStack.Push(name);
		}

		public void EndTag() {
			string tagName = (string)tagStack.Pop();
			WriteString($"</{tagName}>");
		}

		public void NewLine() { ShortTag("br"); }

		public void WriteValue(string value) { WriteString(value); }

		public void OneLiner(string tag, string value, string attributes = "") {
			StartTag(tag, attributes);
			WriteValue(value);
			EndTag();
		}

		public void Link(string url, string value, string attributes = "") {
			StartTag("a", $"href=\"{url}\" " + attributes);
			WriteValue(value);
			EndTag();
		}
		public delegate void WriteContents(HTMLWriter writer);

		public static bool WriteDocument(string fileName, string title,
				string cssStyleFile, WriteContents writeDelegate) {
			using (HTMLWriter writer = new HTMLWriter(fileName)) {
				writer.StartTag("head");
				{
					writer.StartTag("title");
					{ writer.WriteValue(title); }
					writer.EndTag();

					if (cssStyleFile.Length > 0) {
						writer.ShortTag("link",
								"rel=\"stylesheet\" type=\"text/css\" href=\"" + cssStyleFile +
										"\"");
					}
				}
				writer.EndTag();

				writer.StartTag("body");
				{ writeDelegate(writer); }
				writer.EndTag();
			}
			return true;
		}
	}
}
