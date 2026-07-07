using UnityEngine;

[DisallowMultipleComponent]
public class NewPlayerLook : MonoBehaviour
{
    [Header("Kamera")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraFollowTarget;

    [Header("Fizyczne cialo")]
    [SerializeField] private Rigidbody physicalHips;
    [SerializeField] private bool rotateBodyWithLook;

    [Header("Czulosc")]
    [SerializeField] private float xSensitivity = 30f;
    [SerializeField] private float ySensitivity = 30f;
    [SerializeField] private float bodyTurnSpeed = 12f;

    private AudioListener audioListener;
    private float pitch;
    private float yaw;

    private void Awake()
    {
        if (physicalHips == null)
        {
            physicalHips = FindBody("Hips", "Pelvis");
        }

        if (playerCamera != null)
        {
            audioListener = playerCamera.GetComponent<AudioListener>();
            yaw = playerCamera.transform.eulerAngles.y;
            playerCamera.enabled = true;
        }
        else
        {
            yaw = transform.eulerAngles.y;
        }

        if (audioListener != null)
        {
            audioListener.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (playerCamera == null || cameraFollowTarget == null)
        {
            return;
        }

        playerCamera.transform.position = cameraFollowTarget.position;
    }

    private void FixedUpdate()
    {
        if (!rotateBodyWithLook || physicalHips == null)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.Euler(0f, yaw, 0f);
        Quaternion nextRotation = Quaternion.Slerp(
            physicalHips.rotation,
            targetRotation,
            Time.fixedDeltaTime * bodyTurnSpeed
        );
        physicalHips.MoveRotation(nextRotation);
    }

    public void ProcessLook(Vector2 input)
    {
        if (playerCamera == null)
        {
            return;
        }

        yaw += input.x * Time.deltaTime * xSensitivity;
        pitch -= input.y * Time.deltaTime * ySensitivity;
        pitch = Mathf.Clamp(pitch, -80f, 80f);

        playerCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private Rigidbody FindBody(params string[] aliases)
    {
        Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>(true);

        foreach (Rigidbody body in bodies)
        {
            string bodyName = NormalizeBoneName(body.transform.name);

            foreach (string alias in aliases)
            {
                string normalizedAlias = NormalizeBoneName(alias);

                if (bodyName == normalizedAlias || bodyName.EndsWith(normalizedAlias))
                {
                    return body;
                }
            }
        }

        return null;
    }

    private static string NormalizeBoneName(string value)
    {
        return value
            .Replace(" ", "")
            .Replace("_", "")
            .Replace("-", "")
            .ToLowerInvariant();
    }
}
