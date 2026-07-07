using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PlayerLook : NetworkBehaviour
{
    [SerializeField] public Camera playerCamera;
    private AudioListener audioListener;
    private float xRotation = 0f;
    private bool localPlayer;
    public float xSensitivity = 30f;
    public float ySensitivity = 30f;

    public void Awake()
    {
        if (playerCamera == null)
        {
            throw new MissingReferenceException("PlayerLook requires a Player Camera reference. Assign it in the Inspector.");
        }

        audioListener = playerCamera.GetComponent<AudioListener>();
        SetLocalPlayer(CanUseLook());
    }

    public override void OnNetworkSpawn()
    {
        SetLocalPlayer(IsOwner);
    }

    public override void OnNetworkDespawn()
    {
        SetLocalPlayer(false);
    }

    public void SetLocalPlayer(bool value)
    {
        bool wasLocalPlayer = localPlayer;
        localPlayer = value;

        if (playerCamera != null)
        {
            playerCamera.enabled = value;
        }

        if (audioListener != null)
        {
            audioListener.enabled = value;
        }

        if (value)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if (wasLocalPlayer)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void ProcessLook(Vector2 input)
    {
        if (!CanUseLook())
        {
            return;
        }

        float mouseX = input.x;
        float mouseY = input.y;

        xRotation -= mouseY  * Time.deltaTime * ySensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX * Time.deltaTime * xSensitivity);
    }

    private void OnDisable()
    {
        if (!localPlayer)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private bool CanUseLook()
    {
        if (IsSpawned)
        {
            return IsOwner;
        }

        return Unity.Netcode.NetworkManager.Singleton == null || !HasNetworkObject;
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
