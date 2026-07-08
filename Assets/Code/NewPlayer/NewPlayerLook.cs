using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(NewPlayerViewBodyController))]
public class NewPlayerLook : MonoBehaviour
{
    private NewPlayerViewBodyController viewBodyController;

    private void Awake()
    {
        viewBodyController = GetComponent<NewPlayerViewBodyController>();
    }

    public void ProcessLook(Vector2 lookInput)
    {
        viewBodyController.ProcessLook(lookInput);
    }
}
