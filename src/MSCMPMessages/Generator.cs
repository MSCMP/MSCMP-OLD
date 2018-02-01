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

		private int CountOptionals(Type messageType) {
			int optionals = 0;
			foreach (FieldInfo field in fields) {

				if (field.GetCustomAttribute<Optional>() != null) {
					++optionals;
				}
			}
			return optionals;
		}

		private bool hasOptionals = false;
		private int optionalCounter = 0;
		private FieldInfo[] fields = null;

		public void GenerateMessage(Type messageType) {
			var descriptor = messageType.GetCustomAttribute<NetMessageDesc>();
			string interfaceName = "";
			if (descriptor != null) {
				interfaceName = ": INetMessage";
			}

			fields = messageType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

			int optionals = CountOptionals(messageType);
			if (optionals > sizeof(Byte) * 8) {
				throw new Exception("Too many optionals in single message. Currently we support up to 8 optionals per message.");
			}
			hasOptionals = optionals > 0;

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

				// Write constructor.

				BeginBlock("public " + messageType.Name + "()");
				{
					foreach (FieldInfo field in fields) {
						if (field.FieldType.IsArray) {
							WriteLine(field.Name + " = new " + GetTypeName(field.FieldType.GetElementType()) + "[0];");
						}
						else if (IsNetworkMessage(field.FieldType)) {
							WriteLine(field.Name + " = new " + GetTypeName(field.FieldType) + "();");
						}
					}
				}
				EndBlock();

				// Write fields.

				if (hasOptionals) {
					WriteLine("private byte optionalsMask = 0;");
				}

				// All fields in network messages are made public.

				foreach (FieldInfo field in fields) {
					string accessiblity = "public";
					// Make optional attributes private.
					if (field.GetCustomAttribute<Optional>() != null) {
						accessiblity = "private";
					}
					WriteLine(accessiblity + " " + GetTypeName(field.FieldType) + "\t" + field.Name + ";");
				}

				WriteNewLine();


				// Write method.
				optionalCounter = 0;

				BeginBlock("public bool Write(BinaryWriter writer)");
				{
					BeginBlock("try");
					{
						if (hasOptionals) {
							WriteTypeWrite(typeof(Byte), "optionalsMask", false);
						}

						foreach (FieldInfo field in fields) {
							WriteTypeWrite(field.FieldType, field.Name, field.GetCustomAttribute<Optional>() != null);
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
				optionalCounter = 0;

				BeginBlock("public bool Read(BinaryReader reader)");
				{
					BeginBlock("try");
					{
						if (hasOptionals) {
							WriteTypeRead(typeof(Byte), "optionalsMask", false);
						}

						foreach (FieldInfo field in fields) {
							WriteTypeRead(field.FieldType, field.Name, field.GetCustomAttribute<Optional>() != null);
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

				if (hasOptionals) {
					optionalCounter = 0;
					WriteAccessors();
				}
			}
			EndBlock();

			// Cleanup the state of generator.

			fields = null;
			hasOptionals = false;
		}

		private void WriteTypeWrite(Type type, string name, bool isOptional) {
			if (isOptional) {
				int mask = 1 << optionalCounter;
				BeginBlock($"if ((optionalsMask & {mask}) != 0)");
				++optionalCounter;
			}

			if (type.IsArray) {
				WriteLine("writer.Write((System.Int32)" + name + ".Length);");

				Type elementType = type.GetElementType();
				BeginBlock("foreach (" + GetTypeName(elementType) + " value in " + name + ")");
				{
					WriteTypeWrite(elementType, "value", false);
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

			if (isOptional) {
				EndBlock();
			}
		}

		private void WriteTypeRead(Type type, string name, bool isOptional) {
			if (isOptional) {
				int mask = 1 << optionalCounter;
				BeginBlock($"if ((optionalsMask & {mask}) != 0)");
				++optionalCounter;
			}

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
					WriteTypeRead(elementType, name + "[i]", false);
				}
				EndBlock();
			}
			else if (type.IsEnum) {
				string valueVarName = "_" + name + "Value";
				Type enumUnderlayingType = type.GetEnumUnderlyingType();
				WriteLine(enumUnderlayingType.FullName + " " + valueVarName + " = reader.Read" + enumUnderlayingType.Name + "();");
				BeginBlock("if (!" + type.Name + "Helpers.IsValueValid(" + valueVarName + "))");
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

			if (isOptional) {
				EndBlock();
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

		/// <summary>
		/// Writes optionals accessors.
		/// </summary>
		private void WriteAccessors() {
			foreach (var field in fields) {
				if (field.GetCustomAttribute<Optional>() == null) {
					continue;
				}

				WriteAccessorsForField(field);
			}
		}

		/// <summary>
		/// Write accessors for the following optional field.
		/// </summary>
		/// <param name="field">The field to write accessors for.</param>
		private void WriteAccessorsForField(FieldInfo field) {
			Type type = field.FieldType;
			string rawName = field.Name;
			string capitalizedName = rawName.Substring(0, 1).ToUpper() + rawName.Substring(1);

			int fieldMask = (1 << optionalCounter);

			BeginBlock($"public {GetTypeName(type)} {capitalizedName}");
			{
				BeginBlock("get");
				{
					WriteLine($"return {rawName};");
				}
				EndBlock();

				BeginBlock("set");
				{
					WriteLine($"{rawName} = value;");
					WriteLine($"optionalsMask |= {fieldMask};");
				}
				EndBlock();
			}
			EndBlock();

			BeginBlock($"public bool Has{capitalizedName}");
			{
				BeginBlock("get");
				{
					WriteLine($"return (optionalsMask & {fieldMask}) != 0;");
				}
				EndBlock();
			}
			EndBlock();

			optionalCounter++;
		}
	}
}
