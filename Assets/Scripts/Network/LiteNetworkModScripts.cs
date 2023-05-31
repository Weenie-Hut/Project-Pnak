using System.Linq;
using UnityEngine;

namespace Pnak
{
	public class LiteNetworkModScripts : ScriptableObject
	{
#if UNITY_EDITOR
		private static LiteNetworkModScripts _instance;
		public static LiteNetworkModScripts Instance
		{
			get
			{
				if (_instance == null)
				{
					string guid = UnityEditor.AssetDatabase.FindAssets("t:LiteNetworkModScripts")[0];
					string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
					_instance = UnityEditor.AssetDatabase.LoadAssetAtPath<LiteNetworkModScripts>(path);
				}
				return _instance;
			}
		}

		public static string[] FieldNames;
		public static GUIContent[] ModOptions;

		private void OnValidate()
		{
			_instance = this;

			int modsLength = Mods == null ? 0 : Mods.Length;

			FieldNames = new string[modsLength + 1];
			ModOptions = new GUIContent[modsLength + 1];

			FieldNames[0] = null;
			ModOptions[0] = new GUIContent("< Select Mod >");

			System.Reflection.FieldInfo[] fields = typeof(LiteNetworkedData).GetFields();
			for (int i = 0; i < modsLength; i++)
			{
				if (Mods[i] == null)
				{
					FieldNames[i + 1] = null;
					ModOptions[i + 1] = new GUIContent("< Missing Mod >");
					continue;
				}

				FieldNames[i + 1] = Mods[i].DataType.Name + "Field";
				ModOptions[i + 1] = new GUIContent(Mods[i].name);
				Mods[i].EditorSetScriptIndex(i);
			}
		}
#endif

		[NonReorderable]
		public LiteNetworkMod[] Mods;
	}
}