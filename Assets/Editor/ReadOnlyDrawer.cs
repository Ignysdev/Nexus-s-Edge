using UnityEditor;
using UnityEngine;
using static UnityEditor.PlayerSettings;

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    /* - - - - -
     * Ajusta corretamente a altura dos elementos.
     * - - - - - */
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    /* - - - - -
     * Impede a maioria modificação dos elementos no Inspector.
     * - - - - - */
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;

        EditorGUI.PropertyField(position, property, label, true);

        GUI.enabled = true;
    }
}