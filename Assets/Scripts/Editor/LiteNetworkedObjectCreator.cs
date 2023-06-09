using UnityEngine;
using UnityEditor;
using Pnak;
using System;
using System.IO;
using Fusion;
using System.Linq;

namespace PnakEditor
{
	[InitializeOnLoad]
	public static class LiteNetworkedObjectCreator
	{
		static LiteNetworkedObjectCreator()
		{
			UnityEditor.AssemblyReloadEvents.afterAssemblyReload += WriteLiteNetworkedSO;
		}

		public static void WriteLiteNetworkedSO()
		{
			// Using reflection, get all defined structs that implement INetworkStruct and are directly in the Pnak.LiteNetworkedData namespace (no nested defined structs)
			// Filter out all structs that are not serializable
			// For each struct, create a new public field in LiteNetworkedSO with the same name as field value in LiteNetworkedData which is of the same type as the struct. This field should be serialized and only one of these fields should be of the same type.

			Type liteNetworkedDataType = typeof(LiteNetworkedData);
			
			Type[] types = System.Reflection.Assembly.GetAssembly(liteNetworkedDataType).GetTypes();
			string[] structTypes = new string[types.Length];
			int structCount = 0;

			foreach (Type type in types)
			{
				if (type.IsValueType && type.IsSerializable)
				{
					if (!type.FullName.StartsWith("Pnak.LiteNetworkedData+")) continue;

					structTypes[structCount] = type.Name;
					structCount++;
				}
			}

			structTypes = structTypes.Where(x => !string.IsNullOrEmpty(x)).ToArray();
			string[] fieldNames = new string[structCount];
			// string[] humanReadableNames = new string[structCount];

			System.Reflection.FieldInfo[] fields = liteNetworkedDataType.GetFields();
			for (int i = 0; i < structCount; i++)
			{
				fieldNames[i] = structTypes[i] + "Field";

				// Apply PascalCase separation
				// string human = fieldNames[i].Replace("_", " ");
				// humanReadableNames[i] = string.Concat(human.Select((x, j) => j > 0 && char.IsUpper(x) ? " " + x.ToString() : x.ToString())).TrimStart(' ');
			}

			WriteToFile(structTypes, fieldNames);
		}

		public static void WriteToFile(string[] structTypes, string[] fieldNames)
		{
			string file = "Assets/Scripts/Editor/LiteNetworkedSO.cs";
			using (StreamWriter writer = new StreamWriter(file))
			{
				writer.WriteLine("// Path: Assets\\Scripts\\Editor\\LiteNetworkedObjectCreator.cs");
				writer.WriteLine("// This file is automatically generated by LiteNetworkedObjectCreator.cs and SHOULD NOT BE EDITED MANUALLY.");
				writer.WriteLine("using UnityEngine;");
				writer.WriteLine("using UnityEditor;");
				writer.WriteLine("using Pnak;");
				writer.WriteLine("");
				writer.WriteLine("namespace PnakEditor");
				writer.WriteLine("{");
				writer.WriteLine("\tpublic class LiteNetworkedSO : ScriptableObject");
				writer.WriteLine("\t{");

				writer.WriteLine("\t\tprivate static LiteNetworkedSO instance;");
				writer.WriteLine("\t\tpublic static LiteNetworkedSO Instance");
				writer.WriteLine("\t\t{");
				writer.WriteLine("\t\t\tget");
				writer.WriteLine("\t\t\t{");
				writer.WriteLine("\t\t\t\tif (instance == null)");
				writer.WriteLine("\t\t\t\t{");
				writer.WriteLine("\t\t\t\t\tinstance = CreateInstance<LiteNetworkedSO>();");
				writer.WriteLine("\t\t\t\t\tinstance.hideFlags = HideFlags.DontSave;");
				writer.WriteLine("\t\t\t\t}");
				writer.WriteLine("\t\t\t\treturn instance;");
				writer.WriteLine("\t\t\t}");
				writer.WriteLine("\t\t}");
				writer.WriteLine("");

				writer.WriteLine("\t\tprivate static SerializedObject serializedInstance;");
				writer.WriteLine("\t\tpublic static SerializedObject SerializedInstance");
				writer.WriteLine("\t\t{");
				writer.WriteLine("\t\t\tget");
				writer.WriteLine("\t\t\t{");
				writer.WriteLine("\t\t\t\tif (serializedInstance == null)");
				writer.WriteLine("\t\t\t\t{");
				writer.WriteLine("\t\t\t\t\tserializedInstance = new SerializedObject(Instance);");
				writer.WriteLine("\t\t\t\t}");
				writer.WriteLine("\t\t\t\treturn serializedInstance;");
				writer.WriteLine("\t\t\t}");
				writer.WriteLine("\t\t}");
				writer.WriteLine("");

				for (int i = 0; i < structTypes.Length; i++)
				{
					writer.WriteLine("\t\tpublic LiteNetworkedData." + structTypes[i] + " " + fieldNames[i] + ";");
				}

				writer.WriteLine("\t}");
				writer.WriteLine("}");
			}

			EditorApplication.delayCall += AssetDatabase.Refresh;
		}
	}
}