using UnityEngine;

public class ShieldInteractable : Interactable
{
    protected override void Interact()
    {
        Debug.Log("Interacted with " + gameObject.name);
    }
}
