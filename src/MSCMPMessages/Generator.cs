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

		private string GetEnumValueAsString(Type enumType, string valueName) {
			Type underlyingType = enumType.GetEnumUnderlyingType();
			return Convert.ChangeType(Enum.Parse(enumType, valueName), underlyingType).ToString();
		}

		public void GenerateEnum(Type enumType) {

			if (!enumType.IsEnum) {
				throw new Exception("The type must be enum.");
			}
			Type enumUnderlyingType = enumType.GetEnumUnderlyingType();

			Array values = Enum.GetValues(enumType);
			BeginBlock("public enum " + enumType.Name + " : " + enumUnderlyingType.FullName);
			{
				for (int i = 0; i < values.Length; ++i) {
					string valueName = values.GetValue(i).ToString();
					WriteLine(valueName + " = " + GetEnumValueAsString(enumType, valueName) + ",");
				}
			}
			EndBlock();

			BeginBlock("public class " + enumType.Name + "Helpers");
			{
				BeginBlock("public static bool IsValueValid(" + enumUnderlyingType.FullName + " value)");
				{
					BeginBlock("switch (value)");
					{
						for (int i = 0; i < values.Length; ++i) {
							string valueName = values.GetValue(i).ToString();
							WriteLine("case " + GetEnumValueAsString(enumType, valueName) + ":");
						}

						WriteLine("\treturn true;");
					}
					EndBlock();

					WriteLine("return false;");
				}
				EndBlock();
			}
			EndBlock();
		}

		public void GenerateMessage(Type messageType) {
			var descriptor = messageType.GetCustomAttribute<NetMessageDesc>();
			string interfaceName = "";
			if (descriptor != null) {
				interfaceName = ": INetMessage";
			}

			BeginBlock("public class " + messageType.Name + interfaceName);
			{
				// Message info.


				if (descriptor != null) {
					BeginBlock("public byte MessageId");
					BeginBlock("get");
					WriteLine("return " + (byte)descriptor.messageId + ";");
					EndBlock();
					EndBlock();
				}

				FieldInfo[] fields = messageType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

				// Write constructor.

				BeginBlock("public " + messageType.Name + "()");
				{
					foreach (FieldInfo field in fields) {
						Type fieldType = field.FieldType;
						if (fieldType.Namespace == "MSCMPMessages.Messages" && fieldType.IsClass) {
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

				BeginBlock("public bool Write(BinaryWriter writer)");
				{
					BeginBlock("try");
					{
						foreach (FieldInfo field in fields) {
							Type fieldType = field.FieldType;
							if (fieldType.IsEnum) {
								WriteLine("writer.Write((" + fieldType.GetEnumUnderlyingType().FullName + ")" + field.Name + ");");
							}
							else if (fieldType.Namespace == "MSCMPMessages.Messages") {
								BeginBlock("if (!" + field.Name + ".Write(writer))");
								{
									WriteLine("return false;");
								}
								EndBlock();
							}
							else {
								WriteLine("writer.Write((" + fieldType.FullName + ")" + field.Name + ");");
							}
						}

						WriteLine("return true;");
					}
					EndBlock();
					BeginBlock("catch (System.Exception)");
					{
						WriteLine("return false;");
					}
					EndBlock();
				}
				EndBlock();

				// Read method.

				BeginBlock("public bool Read(BinaryReader reader)");
				{
					BeginBlock("try");
					{
						foreach (FieldInfo field in fields) {
							Type fieldType = field.FieldType;
							if (fieldType.IsEnum) {
								string valueVarName = "_" + field.Name + "Value";
								Type enumUnderlayingType = fieldType.GetEnumUnderlyingType();
								WriteLine(enumUnderlayingType.FullName + " " + valueVarName + " = reader.Read" + enumUnderlayingType.Name + "();");
								BeginBlock("if (!" + fieldType.Name + "Helpers.IsValueValid(" + valueVarName + "))");
								{
									WriteLine("return false;");
								}
								EndBlock();

								WriteLine(field.Name + " = (" + fieldType.Name + ")" + valueVarName + ";");
							}
							else if (fieldType.Namespace == "MSCMPMessages.Messages") {
								BeginBlock("if (!" + field.Name + ".Read(reader))");
								{
									WriteLine("return false;");
								}
								EndBlock();
							}
							else {
								WriteLine(field.Name + " = reader.Read" + fieldType.Name + "();");
							}
						}

						WriteLine("return true;");
					}
					EndBlock();
					BeginBlock("catch (System.Exception)");
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

			BeginBlock("namespace MSCMP.Network.Messages");
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
			writer.WriteLine(identation + text + " {");
			identation += "\t";
		}

		private void EndBlock() {
			identation = identation.Remove(identation.Length - 1);
			WriteLine("}");
		}
	}
}
