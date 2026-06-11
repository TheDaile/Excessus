using System.Collections.Generic;
using UnityEngine;

public static class PlayModeTestScene
{
    private static readonly List<Behaviour> disabledBehaviours = new List<Behaviour>();
    private static readonly List<Collider> disabledColliders = new List<Collider>();

    private static bool hasCapturedGlobalState;
    private static float previousTimeScale;
    private static CursorLockMode previousCursorLockState;
    private static bool previousCursorVisibility;

    public static void PrepareIsolatedTestScene()
    {
        RestoreSceneState();
        CaptureGlobalState();
        DisableExistingSceneComponents();

        Time.timeScale = 1f;
    }

    public static void RestoreSceneState()
    {
        foreach (Behaviour behaviour in disabledBehaviours)
        {
            if (behaviour != null)
            {
                behaviour.enabled = true;
            }
        }

        foreach (Collider collider in disabledColliders)
        {
            if (collider != null)
            {
                collider.enabled = true;
            }
        }

        disabledBehaviours.Clear();
        disabledColliders.Clear();

        if (!hasCapturedGlobalState)
        {
            return;
        }

        Time.timeScale = previousTimeScale;
        Cursor.lockState = previousCursorLockState;
        Cursor.visible = previousCursorVisibility;
        hasCapturedGlobalState = false;
    }

    private static void CaptureGlobalState()
    {
        previousTimeScale = Time.timeScale;
        previousCursorLockState = Cursor.lockState;
        previousCursorVisibility = Cursor.visible;
        hasCapturedGlobalState = true;
    }

    private static void DisableExistingSceneComponents()
    {
        foreach (Behaviour behaviour in Object.FindObjectsByType<Behaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            bool belongsToRuntimeAssembly = behaviour.GetType().Assembly == typeof(PlayerController).Assembly;
            bool isCamera = behaviour is Camera;

            if (!behaviour.enabled || (!belongsToRuntimeAssembly && !isCamera))
            {
                continue;
            }

            disabledBehaviours.Add(behaviour);
            behaviour.enabled = false;
        }

        foreach (Collider collider in Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!collider.enabled)
            {
                continue;
            }

            disabledColliders.Add(collider);
            collider.enabled = false;
        }
    }
}
