using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Pnak
{
	[CustomPropertyDrawer(typeof(AttachedAttribute))]
	public class AttachedAttributePropertyDrawer : PropertyDrawer
	{
		private Component[] TryToSetProperty(SerializedProperty property)
		{
			Component component = property.serializedObject.targetObject as Component;
			if (component == null) return null;

			Component[] options = null;


			AttachedAttribute attachedAttribute = attribute as AttachedAttribute;
			if (attachedAttribute.IncludeChildren)
				options = component.GetComponentsInChildren(fieldInfo.FieldType);
			else
				options = component.GetComponents(fieldInfo.FieldType);
			
			if (options.Length > 0 && property.objectReferenceValue == null)
			{
				property.objectReferenceValue = options[0];
			}

			return options;
		}

		public override float GetPropertyHeight(SerializedProperty property,
												GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, true);
		}

		public override void OnGUI(Rect position,
								SerializedProperty property,
								GUIContent label)
		{
			Component[] options = TryToSetProperty(property);

			if (options == null)
			{
				EditorGUI.HelpBox(position, "'Attached' attribute is not on valid field", MessageType.Error);
				return;
			}

			if (options.Length == 0)
			{
				AttachedAttribute attachedAttribute = attribute as AttachedAttribute;
				EditorGUI.HelpBox(position, "No '" + fieldInfo.FieldType.Name + "' attached to this object", attachedAttribute.Required ? MessageType.Error : MessageType.Info);
				return;
			}

			if (options.Length > 1)
			{
				float thisWidth = position.width / 2;
				float width = position.width;
				position.x += position.width - thisWidth;
				position.width = thisWidth;
				EditorGUI.HelpBox(position, "Required", MessageType.Error);
				

				// Draw dropdown that sets the property reference value
				int index = System.Array.IndexOf(options, property.objectReferenceValue);

				string[] display = options.Select(o => o.gameObject.name + $" ({o.GetType().Name})").ToArray();
				index = EditorGUI.Popup(position, index, display);

				position.x -= width - thisWidth;
				position.width = width - thisWidth;
			}

			GUI.enabled = false;
			EditorGUI.PropertyField(position, property, label, true);
			GUI.enabled = true;
		}
	}
}