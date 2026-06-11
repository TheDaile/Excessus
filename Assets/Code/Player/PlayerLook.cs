using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [SerializeField] public Camera playerCamera;
    private float xRotation = 0f;
    public float xSensitivity = 30f;
    public float ySensitivity = 30f;

    public void Awake()
    {
        if (playerCamera == null)
        {
            throw new MissingReferenceException("PlayerLook requires a Player Camera reference. Assign it in the Inspector.");
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ProcessLook(Vector2 input)
    {
        float mouseX = input.x;
        float mouseY = input.y;

        xRotation -= mouseY  * Time.deltaTime * ySensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX * Time.deltaTime * xSensitivity);
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (playerCamera == null)
        {
            Debug.LogWarning("PlayerLook: Player Camera reference is not assigned.", this);
        }
    }
#endif
}