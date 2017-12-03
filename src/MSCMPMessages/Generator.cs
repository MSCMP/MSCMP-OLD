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

		private string GetTypeName(Type type) {
			if (type.Namespace == "MSCMPMessages.Messages") {
				return type.Name;
			}
			return type.FullName;
		}

		private bool IsNetworkMessage(Type type) {
			return type.Namespace == "MSCMPMessages.Messages" && type.IsClass;
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
						if (field.FieldType.IsArray) {
							WriteLine(field.Name + " = null;");
						}
						else if (IsNetworkMessage(field.FieldType)) {
							WriteLine(field.Name + " = new " + GetTypeName(field.FieldType) + "();");
						}
					}
				}
				EndBlock();

				// Write fields.


				// All fields in network messages are made public.

				foreach (FieldInfo field in fields) {
					WriteLine("public " + GetTypeName(field.FieldType) + "\t" + field.Name + ";");
				}

				WriteNewLine();

				// Write method.

				BeginBlock("public bool Write(BinaryWriter writer)");
				{
					BeginBlock("try");
					{
						foreach (FieldInfo field in fields) {
							WriteTypeWrite(field.FieldType, field.Name);
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
							WriteTypeRead(field.FieldType, field.Name);
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

		private void WriteTypeWrite(Type type, string name) {
			if (type.IsArray) {
				WriteLine("writer.Write((System.Int32)" + name + ".Length);");

				Type elementType = type.GetElementType();
				BeginBlock("foreach (" + GetTypeName(elementType) + " value in " + name + ")");
				{
					WriteTypeWrite(elementType, "value");
				}
				EndBlock();
			}
			else if (type.IsEnum) {
				WriteLine("writer.Write((" + type.GetEnumUnderlyingType().FullName + ")" + name + ");");
			}
			else if (IsNetworkMessage(type)) {
				BeginBlock("if (!" + name + ".Write(writer))");
				{
					WriteLine("return false;");
				}
				EndBlock();
			}
			else {
				WriteLine("writer.Write((" + type.FullName + ")" + name + ");");
			}
		}

		private void WriteTypeRead(Type type, string name) {
			if (type.IsArray) {
				string lenVarName = name + "Length";
				WriteLine("System.Int32 " + lenVarName +" = reader.ReadInt32();");
				Type elementType = type.GetElementType();
				WriteLine(name + " = new " + GetTypeName(elementType) + "[" + lenVarName + "];");

				BeginBlock("for (int i = 0 ; i < " + lenVarName + "; ++i)");
				{
					if (elementType.IsClass) {
						WriteLine(name + "[i] = new " + GetTypeName(elementType) + "();");
					}
					WriteTypeRead(elementType, name + "[i]");
				}
				EndBlock();
			}
			else if (type.IsEnum) {
				string valueVarName = "_" + name + "Value";
				Type enumUnderlayingType = type.GetEnumUnderlyingType();
				WriteLine(enumUnderlayingType.FullName + " " + valueVarName + " = reader.Read" + enumUnderlayingType.Name + "();");
				BeginBlock("if (!" + name + "Helpers.IsValueValid(" + valueVarName + "))");
				{
					WriteLine("return false;");
				}
				EndBlock();

				WriteLine(name + " = (" + GetTypeName(type) + ")" + valueVarName + ";");
			}
			else if (IsNetworkMessage(type)) {
				BeginBlock("if (!" + name + ".Read(reader))");
				{
					WriteLine("return false;");
				}
				EndBlock();
			}
			else {
				WriteLine(name + " = reader.Read" + type.Name + "();");
			}
		}

		private void WriteHeader() {
			WriteLine("// Generated at " + DateTime.Now.ToString());
			WriteLine("using System.IO;");
			WriteLine("using System.Collections.Generic;");

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
