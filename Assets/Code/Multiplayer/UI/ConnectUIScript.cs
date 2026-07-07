using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ConnectUIScript : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    void Start()
    {
        hostButton.onClick.AddListener(HostButtonOnClick);
        clientButton.onClick.AddListener(ClientButtonOnClick);
    }

    void HostButtonOnClick()
    {
        NetworkManager.Singleton.StartHost();
    }
    void ClientButtonOnClick()
    {
        NetworkManager.Singleton.StartClient();
    }
}
