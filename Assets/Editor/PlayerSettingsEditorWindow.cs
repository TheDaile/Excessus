#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class PlayerSettingsEditorWindow : EditorWindow
{
    [MenuItem("Tools/Player Settings")]
    public static void Open()
    {
        GetWindow<PlayerSettingsEditorWindow>("Player Settings");
    }

    private void OnGUI()
    {
        GUILayout.Label("Controls", EditorStyles.boldLabel);

        ButtonMode sprintMode = PlayerSettings.SprintMode;
        ButtonMode crouchMode = PlayerSettings.CrouchMode;

        EditorGUI.BeginChangeCheck();

        sprintMode = (ButtonMode)EditorGUILayout.EnumPopup("Sprint Mode", sprintMode);
        crouchMode = (ButtonMode)EditorGUILayout.EnumPopup("Crouch Mode", crouchMode);

        if (EditorGUI.EndChangeCheck())
        {
            PlayerSettings.SprintMode = sprintMode;
            PlayerSettings.CrouchMode = crouchMode;
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Reset To Defaults"))
        {
            PlayerSettings.ResetToDefaults();
        }

        if (GUILayout.Button("Delete All PlayerPrefs"))
        {
            if (EditorUtility.DisplayDialog(
                    "Delete All PlayerPrefs?",
                    "This removes all saved PlayerPrefs for this project.",
                    "Delete",
                    "Cancel"))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
            }
        }
    }
}
#endif