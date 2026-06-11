using System;
using UnityEngine;

public static class PlayerSettings
{
    private const string SprintModeKey = "Controls.SprintMode";
    private const string CrouchModeKey = "Controls.CrouchMode";

    public static event Action Changed;

    public static ButtonMode SprintMode
    {
        get => (ButtonMode)PlayerPrefs.GetInt(SprintModeKey, (int)ButtonMode.Hold);
        set
        {
            PlayerPrefs.SetInt(SprintModeKey, (int)value);
            Changed?.Invoke();
        }
    }
    public static ButtonMode CrouchMode
    {
        get => (ButtonMode)PlayerPrefs.GetInt(CrouchModeKey, (int)ButtonMode.Hold);
        set
        {
            PlayerPrefs.SetInt(CrouchModeKey, (int)value);
            Changed?.Invoke();
        }
    }

    public static void ResetToDefaults()
    {
        PlayerPrefs.SetInt(SprintModeKey, (int)ButtonMode.Hold);
        PlayerPrefs.SetInt(CrouchModeKey, (int)ButtonMode.Hold);
        PlayerPrefs.Save();

        Changed?.Invoke();
    }
}