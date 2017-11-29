using System;
using System.IO;
using System.Reflection;

namespace MSCMPMessages {
	class Generator {

		StreamWriter writer = null;
		string identation = "";

		public Generator(string fileName) {
			writer = new StreamWriter(fileName);

			WriteHeader();
		}

		public void EndGeneration() {
			WriteFooter();

			writer.Flush();
		}

		public void GenerateMessage(Type messageType) {
			var descriptor = messageType.GetCustomAttribute<NetMessageDesc>();
			string interfaceName = "";
			if (descriptor != null) {
				interfaceName = ": INetMessage";
			}

			BeginBlock("public class " + messageType.Name + interfaceName + " {");
			{
				// Message info.


				if (descriptor != null) {
					BeginBlock("public byte MessageId {");
					BeginBlock("get {");
					WriteLine("return " + (byte)descriptor.messageId + ";");
					EndBlock();
					EndBlock();
				}

				FieldInfo[] fields = messageType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

				// Write constructor.

				BeginBlock("public " + messageType.Name + "() {");
				{
					foreach (FieldInfo field in fields) {
						Type fieldType = field.FieldType;
						if (fieldType.Namespace == "MSCMPMessages.Messages") {
							WriteLine(field.Name + " = new " + fieldType.Name + "();");
						}
					}
				}
				EndBlock();

				// Write fields.


				// All fields in network messages are made public.

				foreach (FieldInfo field in fields) {
					Type fieldType = field.FieldType;
					string fieldTypeName = fieldType.FullName;
					if (fieldType.Namespace == "MSCMPMessages.Messages") {
						fieldTypeName = fieldType.Name;
					}

					WriteLine("public " + fieldTypeName + "\t" + field.Name + ";");
				}

				WriteNewLine();

				// Write method.

				BeginBlock("public bool Write(BinaryWriter writer) {");
				{
					BeginBlock("try {");
					{
						foreach (FieldInfo field in fields) {
							if (field.FieldType.Namespace == "MSCMPMessages.Messages") {
								BeginBlock("if (!" + field.Name + ".Write(writer)) {");
								{
									WriteLine("return false;");
								}
								EndBlock();
							}
							else {
								WriteLine("writer.Write((" + field.FieldType.FullName + ")" + field.Name + ");");
							}
						}

						WriteLine("return true;");
					}
					EndBlock();
					BeginBlock("catch (System.Exception) {");
					{
						WriteLine("return false;");
					}
					EndBlock();
				}
				EndBlock();

				// Read method.

				BeginBlock("public bool Read(BinaryReader reader) {");
				{
					BeginBlock("try {");
					{
						foreach (FieldInfo field in fields) {
							if (field.FieldType.Namespace == "MSCMPMessages.Messages") {
								BeginBlock("if (!" + field.Name + ".Read(reader)) {");
								{
									WriteLine("return false;");
								}
								EndBlock();
							}
							else {
								WriteLine(field.Name + " = reader.Read" + field.FieldType.Name + "();");
							}
						}

						WriteLine("return true;");
					}
					EndBlock();
					BeginBlock("catch (System.Exception) {");
					{
						WriteLine("return false;");
					}
					EndBlock();
				}
				EndBlock();
			}
			EndBlock();
		}

		private void WriteHeader() {
			WriteLine("// Generated at " + DateTime.Now.ToString());
			WriteLine("using System.IO;");

			BeginBlock("namespace MSCMP.Network.Messages {");
		}

		private void WriteFooter() {

			EndBlock();

			WriteNewLine();
			WriteLine("//eof");
			WriteNewLine();
		}

		private void WriteLine(string text) {
			writer.WriteLine(identation + text);
		}

		private void WriteNewLine() {
			writer.Write("\n");
		}

		private void BeginBlock(string text) {
			writer.WriteLine(identation + text);
			identation += "\t";
		}

		private void EndBlock() {
			identation = identation.Remove(identation.Length - 1);
			WriteLine("}");
		}
	}
}
